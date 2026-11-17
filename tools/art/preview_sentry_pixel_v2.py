"""Prepare source-only decal, or finish the single-view pixel comparison."""
from pathlib import Path
import sys
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'art-source/soviet-sentry'
OUT = BASE / 'attempt-02'
OUT.mkdir(parents=True, exist_ok=True)
source = Image.open(ROOT / 'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
if '--prepare' in sys.argv:
    panel = Image.open(BASE / 'textures/side_panel.png').convert('RGBA')
    # Cut out only the source emblem; replace baked highlights/shadows with one source swatch.
    arr = np.array(panel)
    r, g, b = arr[:, :, 0].astype(float), arr[:, :, 1].astype(float), arr[:, :, 2].astype(float)
    emblem = (r > 1.35 * g) & (r > 1.6 * b) & (r > 70)
    result = np.empty_like(arr)
    result[:] = (85, 81, 82, 255)
    result[emblem] = arr[emblem]
    im = Image.fromarray(result)
    assert set(im.getdata()) <= set(source.getdata())
    im.save(OUT / 'side-panel-clean.png')
    print('Prepared source-only flat-panel/emblem cutout.')
    raise SystemExit()

raw = Image.open(OUT / 'render-raw.png').convert('RGBA')
a = np.array(raw)
opaque = a[:, :, 3] >= 128
# Lock to selected reference colors plus copied emblem colors. No dithering.
colors = [(42,38,39), (53,50,52), (65,63,65), (85,81,82), (121,113,105),
          (133,126,119), (58,33,30), (76,49,45), (107,53,49)]
colors += [c[:3] for c in Image.open(OUT / 'side-panel-clean.png').getdata()]
palette = np.array(sorted(set(colors)), dtype=np.int32)
rgb = a[:, :, :3].astype(np.int32)
nearest = ((rgb[:, :, None, :] - palette[None, None, :, :]) ** 2).sum(axis=3).argmin(axis=2)
result = np.zeros_like(a)
result[:, :, :3] = palette[nearest]
result[:, :, 3] = opaque * 255
mask = Image.fromarray((opaque * 255).astype('uint8'))
outline = (np.array(mask.filter(ImageFilter.MaxFilter(3))) > 0) & ~opaque
result[outline] = (42,38,39,255)
result[result[:,:,3] == 0] = 0
sprite = Image.fromarray(result)
assert set(sprite.getdata()) <= set(source.getdata())
sprite.save(OUT / 'turret-pixel-v2.png')
sprite.resize((640,640), Image.Resampling.NEAREST).save(OUT / 'turret-pixel-v2-4x.png')
# Comparison shows original pixel sizes without bilinear scaling.
comparison = Image.new('RGB', (480,200), '#77736e')
d = ImageDraw.Draw(comparison)
ref = source.crop((43,58,187,161))
old = Image.open(BASE / 'renders/turret-00.png').convert('RGBA')
for x, im, label in [(0,ref,'Reference (crop)'),(160,old,'V1'),(320,sprite,'V2 - flat palette')]:
    comparison.paste(im, (x+(160-im.width)//2, 24), im)
    d.text((x+6,180), label, fill='white')
comparison.save(OUT / 'comparison-native.png')
comparison.resize((1440,600),Image.Resampling.NEAREST).save(OUT / 'comparison-3x.png')
print(f'V2: {len(set(sprite.getdata()))-1} visible source colors, binary alpha, no dithering.')
