"""Native fixture screenshot regression (1x zoom, before movement).
Usage: python tools/test_building_occlusion_pixels.py BASELINE.png FIXED.png [china]
Use test-rock-occlusion.ps1 -Buildings -Gdi [-Baseline], second screenshot.
"""
import sys
from PIL import Image

before, after = [Image.open(p).convert('RGB') for p in sys.argv[1:3]]
assert before.size == after.size
cx, cy = before.width // 2, before.height // 2

def changes(box):
    return [tuple(b - a for a, b in zip(p, q))
            for p, q in zip(before.crop(box).getdata(), after.crop(box).getdata())]

# An actually hidden tank must brighten a substantial part of the roof;
# merely emitting silhouette renderables for empty model bounds is insufficient.
china = len(sys.argv) > 3 and sys.argv[3] == 'china'
white = changes((cx - 20, cy - 110, cx + 30, cy - 20) if china else
                (cx - 20, cy - 55, cx + 40, cy + 20))
assert sum(all(channel > 8 for channel in pixel) for pixel in white) > 200, 'No substantive white silhouette'
# Normal front-facing unit and grass outside the building remain untouched.
for box in [(cx - 32, cy + 280, cx + 32, cy + 350),
            (cx - 320, cy - 80, cx - 280, cy + 40)]:
    assert sum(any(abs(channel) > 8 for channel in pixel) for pixel in changes(box)) == 0, 'Unoccluded pixels changed'
print('PASS: actual occluded unit pixels brighten; front unit and outside pixels unchanged')
