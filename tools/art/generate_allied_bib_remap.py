"""Allied black-rim masks only. Supplied foundations and their inner lines stay intact."""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'mods/ca/bits/allies/buildings'
OUTPUT = SOURCE / 'remap'


def generate():
    OUTPUT.mkdir(exist_ok=True)
    manifest = []
    for path in sorted(SOURCE.glob('bib*.png')):
        source = Image.open(path).convert('RGBA')
        mask = Image.new('P', source.size)
        palette = [0] * 768
        # Black itself cannot be color-remapped. Opaque mid-grey becomes a
        # readable dark player color, with no residual black showing through.
        palette[3:6] = [128, 128, 128]
        mask.putpalette(palette)
        indices = [int(rgba == (0, 0, 0, 255)) for rgba in source.getdata()]
        mask.putdata(indices)
        mask.save(OUTPUT / path.name, transparency=0)
        manifest.append({'source': path.name, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                         'size': source.size, 'rim_pixels': sum(indices)})
    (OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    generate()
