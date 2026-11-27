"""Reproducible native-density cut-and-paste, never replacement artwork.

Only source pixels and binary masks are allowed. Optional repairs copy bounded
source patches at 1:1 density without blending, recoloring, rotation or mirroring.
Outputs are development assets, not registered actors or a finished urban kit.
"""
from collections import deque
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'mods/ca/bits/misc/buildings'
ART = ROOT / 'art-source/urban-warfare'
OUTPUT = ART / 'cutouts'
SOURCE_HASHES = json.loads((ART / 'source-hashes.json').read_text(encoding='utf-8'))
CUTS = [spec for path in sorted(ART.glob('source-cuts*.json'))
        for spec in json.loads(path.read_text(encoding='utf-8'))]


def source_image(name):
    path = SOURCE / name
    if path.resolve().parent != SOURCE.resolve():
        raise ValueError('Source must be a supplied building-directory file')
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    if SOURCE_HASHES.get(name) != digest:
        raise ValueError(f'Source is new or replaced: inventory/review it before recutting {name}')
    return Image.open(path).convert('RGBA'), digest


def source_reference(spec):
    source, digest = source_image(spec['source'])
    if 'source_sha256' in spec and digest != spec['source_sha256']:
        raise ValueError(f"Source replaced: re-review cut {spec['id']}")
    sources = {spec['source']: digest}
    for repair in spec.get('repairs', []):
        if not repair.get('reason'):
            raise ValueError('Every cut-and-paste repair needs an explanation')
        donor, donor_hash = source_image(repair['source'])
        sources[repair['source']] = donor_hash
        box = repair['crop']
        assert 0 <= box[0] < box[2] <= donor.width
        assert 0 <= box[1] < box[3] <= donor.height
        patch = donor.crop(box)
        x, y = repair['at']
        assert 0 <= x <= source.width-patch.width and 0 <= y <= source.height-patch.height
        # Binary coverage copies exact RGBA, including any source alpha, without blending.
        coverage = patch.getchannel('A').point(lambda a: 255 if a else 0)
        if 'polygon' in repair:
            shape = Image.new('L', patch.size)
            ImageDraw.Draw(shape).polygon([tuple(p) for p in repair['polygon']], fill=255)
            coverage = ImageChops.multiply(coverage, shape)
        source.paste(patch, (x, y), coverage)
    return source, sources


def extract(spec):
    reference, sources = source_reference(spec)
    mask = Image.new('L', reference.size)
    ImageDraw.Draw(mask).polygon([tuple(p) for p in spec['polygon']], fill=255)
    for hole in spec.get('exclude', []):
        ImageDraw.Draw(mask).polygon([tuple(p) for p in hole], fill=0)
    masked = reference.copy()
    masked.putalpha(ImageChops.multiply(reference.getchannel('A'), mask))
    if 'exterior_background' in spec:
        background = spec['exterior_background']
        colors = {tuple(background)} if isinstance(background[0], int) else {tuple(c) for c in background}
        pixels = masked.load()
        candidates = {(x, y) for y in range(masked.height) for x in range(masked.width)
                      if pixels[x, y][3] and pixels[x, y][:3] in colors}
        queue = deque((x, y) for x, y in candidates if any(
            not (0 <= nx < masked.width and 0 <= ny < masked.height)
            or not pixels[nx, ny][3]
            for nx, ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]))
        while queue:
            point = queue.popleft()
            if point not in candidates:
                continue
            candidates.remove(point)
            x, y = point
            pixels[x, y] = (*pixels[x, y][:3], 0)
            queue.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
    if 'retain_rgb' in spec:
        # Optional exact source-palette selection within the hand-bounded mask,
        # e.g. paint without its surrounding asphalt. This only removes alpha.
        allowed = {tuple(rgb) for rgb in spec['retain_rgb']}
        masked.putdata([p if p[:3] in allowed else (*p[:3],0) for p in masked.getdata()])
    bounds = masked.getbbox()
    assert bounds is not None
    cut = masked.crop(bounds)
    for got, expected in zip(cut.getdata(), reference.crop(bounds).getdata()):
        assert got[3] == 0 or got == expected
    return cut, bounds, sources


def prepare():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    records = []
    ids = set()
    for spec in CUTS:
        assert spec['id'] not in ids, 'Duplicate cutout ID'
        ids.add(spec['id'])
        cut, bounds, sources = extract(spec)
        output = OUTPUT / (spec['id'] + '.png')
        cut.save(output)
        for name, digest in sources.items():
            assert hashlib.sha256((SOURCE / name).read_bytes()).hexdigest() == digest
        records.append(dict(spec, source_sha256=sources[spec['source']], sources=sources,
                            crop=list(bounds), ground_in_cutout=[spec['ground'][0]-bounds[0],
                                                               spec['ground'][1]-bounds[1]],
                            size=list(cut.size), output=output.relative_to(ROOT).as_posix(),
                            status='source-cut-only; runtime placement and footprint pending'))
    (ART / 'cutout-catalog.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
    # Review native pixels at integer enlargement on light AND dark backgrounds.
    review = ART / 'reviews/current'
    review.mkdir(parents=True, exist_ok=True)
    for page in range((len(CUTS)+3)//4):
        for name, color, text in [('dark', (45,55,60), 'white'), ('light', (210,220,230), 'black')]:
            sheet = Image.new('RGB', (1000,640), color)
            draw = ImageDraw.Draw(sheet)
            for i, spec in enumerate(CUTS[page*4:page*4+4]):
                cut = Image.open(OUTPUT / (spec['id']+'.png'))
                factor = min(2, 460//cut.width, 275//cut.height)
                cut = cut.resize((cut.width*factor, cut.height*factor), Image.Resampling.NEAREST)
                x, y = i%2*500+20, i//2*320+10
                sheet.paste(cut, (x,y), cut)
                draw.text((x,y+285), spec['id'], fill=text)
            sheet.save(review / f'{name}-{page}.png')
    print(f'Extracted {len(records)} source-only cutouts; originals preserved. No runtime integration.')


if __name__ == '__main__':
    prepare()
