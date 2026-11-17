"""Source-only cut/paste textures for the Blender sentry prototype (Pillow)."""
from pathlib import Path
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
OUT = ROOT / 'art-source/soviet-sentry/textures'
OUT.mkdir(parents=True, exist_ok=True)
im = Image.open(SOURCE).convert('RGBA')
# QUAD coordinates: upper-left, lower-left, lower-right, upper-right.
# Perspective rectification uses nearest-neighbour sampling only. No painted pixels.
patches = {
    'side_panel': ((32, 40), (140, 102, 139, 134, 165, 122, 166, 93)),
    'roof': ((32, 24), (115, 73, 139, 87, 162, 78, 137, 63)),
    'red_band': ((32, 12), (105, 136, 106, 148, 142, 155, 142, 145)),
    'rear_panel': ((16, 24), (172, 93, 172, 112, 179, 109, 179, 89)),
}
for name, (size, quad) in patches.items():
    im.transform(size, Image.Transform.QUAD, quad, Image.Resampling.NEAREST).save(OUT / (name + '.png'))
# Uniform materials are single source pixels expanded with nearest-neighbour.
swatches = {'armor': (124, 73), 'edge': (138, 100), 'dark': (129, 128),
            'steel': (80, 115), 'red': (118, 144), 'highlight': (133, 75)}
for name, (x, y) in swatches.items():
    im.crop((x, y, x + 1, y + 1)).resize((8, 8), Image.Resampling.NEAREST).save(OUT / (name + '.png'))
(OUT.parent / 'texture-provenance.json').write_text(json.dumps({
    'source': SOURCE.relative_to(ROOT).as_posix(),
    'sampling': 'nearest; all texture RGBA values copied from source',
    'patches': patches, 'swatches': swatches}, indent=2) + '\n')
source_colors = set(im.getdata())
for p in OUT.glob('*.png'):
    assert set(Image.open(p).convert('RGBA').getdata()) <= source_colors, p
print('Verified: every texture pixel occurs verbatim in vorlage.png.')
