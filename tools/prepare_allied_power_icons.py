"""Trial cameos from the authoritative Allied building PNGs; originals untouched."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import hashlib, json
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art-source/allied-power-icons'
OUT = ROOT / 'mods/ca/bits/allies/icons'

def make_icon(source, crop_box):
    # Source-space close-up: emphasize the reactor, not the entire ground footprint.
    image = Image.new('RGBA', (256, 192))
    draw = ImageDraw.Draw(image)
    for y in range(192):
        t = y / 191
        draw.line((0, y, 255, y), fill=(round(43-23*t), round(67-38*t), round(85-45*t), 255))
    crop = source.crop(crop_box).resize((256, 192), Image.Resampling.LANCZOS)
    x, y = 0, 0
    shadow = Image.new('RGBA', image.size)
    shadow.paste((0, 0, 0, 130), (x+3, y+5), crop.getchannel('A'))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(3)))
    image.alpha_composite(crop, (x,y))
    return image

def main():
    ART.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    review = Image.new('RGB', (600, 320), (28, 32, 40))
    records = []
    for i, (name, filename) in enumerate((('power-plant', 'small fusion power.png'), ('fusion-power-plant', 'fusion power 1.png'))):
        path = ROOT / 'mods/ca/bits/allies/buildings' / filename
        crop_box = (29, 31, 210, 167) if i == 0 else (30, 30, 231, 181)
        master = make_icon(Image.open(path).convert('RGBA'), crop_box)
        master.save(ART / f'{name}-256x192.png')
        # 2x source for the 96x72 logical cell: retain detail at 1.25x/1.5x UI scale.
        icon = master.resize((192,144), Image.Resampling.LANCZOS)
        icon.save(OUT / f'{name}.png')
        preview = icon.resize((96,72), Image.Resampling.LANCZOS)
        review.paste(preview.resize((288,216), Image.Resampling.NEAREST), (6+300*i,8))
        review.paste(preview, (102+300*i,240))
        records.append({'source': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'crop': crop_box, 'runtime': (OUT / f'{name}.png').relative_to(ROOT).as_posix()})
    review.save(ART / 'comparison.png')
    (ART / 'provenance.json').write_text(json.dumps(records, indent=2)+'\n')

if __name__ == '__main__':
    main()
