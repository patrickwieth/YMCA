"""Validate the isolated native selection-zoom screenshots (1700x1000 fixture)."""
from pathlib import Path
import sys
from PIL import Image

root = Path(sys.argv[1])
files = sorted(root.glob('Screenshots/**/*.png'))
assert len(files) >= 4, 'Missing far-hidden, far-shown, close-shown or settings capture'
images = [Image.open(p).convert('RGB') for p in files[:3]]
assert all(im.size == (1700, 1000) for im in images)
white, health = [], []
for im in images:
    crop = im.crop((450, 250, 1250, 700))
    pixels = list(crop.getdata())
    white.append(sum(min(p) > 230 for p in pixels))
    health.append({i for i, p in enumerate(pixels) if p[1] > 180 and p[0] < 80 and p[2] < 120})
assert white[0] == 0 and white[1] > 300 and white[2] > 300, white
assert len(health[0]) > 100 and health[0] == health[1], 'Health bar pixels changed with frame setting'
results = (root / 'results.txt').read_text()
assert 'SELECTED 8' in results and 'FINAL_SELECTED 8' in results
assert 'SLIDER Below 75% zoom' in results and 'RESET_PASS' in results and 'PASS:' in results
settings = (root / 'settings.yaml').read_text(encoding='utf-8-sig')
assert 'SelectionBoxMinimumZoom: 0.75' in settings, 'Live slider value not saved'
print(f'PASS: white frame pixels hidden/shown/close={white}; identical {len(health[0])} health pixels; selection, UI slider, persistence and reset intact.')
