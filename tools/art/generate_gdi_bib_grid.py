"""GDI foundation grids from the supplied native 1x1; never overwrite originals."""
from collections import Counter
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'mods/ca/bits/gdi/buildings'
MANIFEST = ROOT / 'art-source/gdi-bib-grid/manifest.json'
SIZES = [(1, 1), (2, 2), (3, 3), (4, 4), (4, 5)]


def generate():
    tile = Image.open(SOURCE / 'bib1x1.png').convert('RGBA')
    assert tile.size == (128, 64)
    concrete = Counter(p for p in tile.getdata() if p[3] == 255).most_common(1)[0][0]
    entries = []
    for nx, ny in SIZES:
        source = SOURCE / f'bib{nx}x{ny}.png'
        original = Image.open(source).convert('RGBA')
        assert original.size == ((nx + ny) * 64, (nx + ny) * 32)
        target = source if (nx, ny) == (1, 1) else SOURCE / f'bib{nx}x{ny}-grid.png'
        if target != source:
            grid = Image.new('RGBA', original.size, concrete)
            for x, y in sorted(((x, y) for x in range(nx) for y in range(ny)), key=sum):
                grid.alpha_composite(tile, ((ny - 1 + x - y) * 64, (x + y) * 32))
            grid.putalpha(original.getchannel('A'))
            grid.save(target)
        entries.append(dict(source=source.name, runtime=target.name, cells=[nx, ny],
                            source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                            runtime_sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(entries, indent=2) + '\n')


if __name__ == '__main__':
    generate()
