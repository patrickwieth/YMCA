"""Pack Blender PNGs without resizing or per-frame trimming; preserve fixed pivots."""
from pathlib import Path
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art-source/soviet-sentry'
frames = [Image.open(OUT / 'renders' / f'turret-{i:02d}.png').convert('RGBA') for i in range(32)]
assert all(im.size == (128, 128) for im in frames)
sheet = Image.new('RGBA', (1024, 512))
review = Image.new('RGB', (1024, 576), '#454950')
draw = ImageDraw.Draw(review)
for i, im in enumerate(frames):
    x, y = i % 8 * 128, i // 8 * 128
    sheet.paste(im, (x, y))
    review.paste(im, (x, i // 8 * 144), im)
    draw.text((x + 4, i // 8 * 144 + 129), f'{i:02d} / {i * 11.25:g} deg', fill='white')
sheet.save(OUT / 'sentry-turret-32.png')
review.save(OUT / 'review-32.png')
frames[0].resize((512, 512), Image.Resampling.NEAREST).save(OUT / 'review-facing-00.png')
(OUT / 'render-layout.json').write_text(json.dumps({
    'frame_size': [128, 128], 'facings': 32, 'columns': 8, 'rows': 4,
    'order': 'row-major, counterclockwise model Z, 11.25 degrees per frame',
    'note': 'Prototype only; validate OpenRA facing-zero, handedness and attachment offset before binding.'
}, indent=2) + '\n')
print('Packed 32 frames into 1024x512 RGBA sheet. Game bindings unchanged.')
