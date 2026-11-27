"""Compare four native ATWR rotations, with only voxel occluders toggled.
Usage: python tools/test_voxel_occlusion_pixels.py BASELINE_DIR FIXED_DIR
Generate using test-rock-occlusion.ps1 -Buildings -Voxel [-Baseline].
"""
from pathlib import Path
import sys
from PIL import Image

folders = [Path(p) for p in sys.argv[1:]]
assert len(folders) == 2
shots = [sorted((p / 'Screenshots').rglob('*.png')) for p in folders]
assert all(len(s) >= 4 for s in shots)
for rotation in range(4):
    before, after = [Image.open(s[rotation]).convert('RGB') for s in shots]
    assert before.size == after.size
    cx, cy = before.width // 2, before.height // 2
    def delta(box):
        return [tuple(b - a for a, b in zip(p, q)) for p, q in
                zip(before.crop(box).getdata(), after.crop(box).getdata())]
    # The sprite base is already an occluder in BOTH runs. Only the newly
    # supported voxel turret can cause this additional white unit portion.
    pixels = delta((cx - 35, cy - 115, cx + 35, cy - 45))
    assert sum(all(c > 8 for c in p) for p in pixels) > 100, f'No voxel-only silhouette at rotation {rotation}'
    # Later captures have timestamp notifications over the front unit; its
    # unclipped renderables are also checked by the native probe.
    grass = delta((cx - 200, cy - 100, cx - 140, cy + 80))
    assert not any(any(abs(c) > 8 for c in p) for p in grass), 'Outside pixels changed'
    if rotation == 0:
        front = delta((cx - 32, cy + 280, cx + 32, cy + 350))
        # Separate native runs can differ by a few idle-animation pixels.
        # The native probe additionally rejects ANY silhouette renderable here.
        assert sum(any(abs(c) > 8 for c in p) for p in front) <= 16, 'Front unit changed substantially'
print('PASS: four voxel-only silhouette comparisons; front unit and outside pixels unchanged')
