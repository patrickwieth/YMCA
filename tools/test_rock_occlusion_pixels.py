"""Compare native slope fixtures, including actual unit pixels (not mask counts).
Usage: python tools/test_rock_occlusion_pixels.py BASELINE_DIR FIXED_DIR
Both captures must use test-rock-occlusion.ps1 -Slopes, baseline also -Baseline.
"""
import re
import sys
from pathlib import Path
from PIL import Image


def main():
    baseline, fixed = map(Path, sys.argv[1:])
    images = [Image.open(sorted(root.glob('Screenshots/**/*.png'))[0]).convert('RGB')
              for root in (baseline, fixed)]
    assert images[0].size == images[1].size
    text = (fixed / 'results.txt').read_text()
    scale = float(re.search(r'UI_SCALE ([\d.]+)', text)[1])
    checked = set()
    for cell, sx, sy, ramp in re.findall(r'PIXEL (\S+) (\d+) (\d+) ramp=(\d+)', text):
        # The front reference can lie outside this camera; it is covered by
        # the separate movement/privacy/depletion fixture instead.
        if ramp == '0' and cell not in ('102,-33', '101,-34', '99,-36', '110,-25'):
            continue
        x, y = round(int(sx) * scale), round(int(sy) * scale)
        body = []
        differences = []
        for py in range(max(0, y-32), min(images[0].height, y+8)):
            for px in range(max(0, x-20), min(images[0].width, x+20)):
                before, after = [im.getpixel((px, py)) for im in images]
                if before[2] > max(before[0], before[1]) + 30:
                    body.append(after)
                    differences.append(max(abs(a-b) for a, b in zip(before, after)))
        assert len(body) >= 30, f'{cell}: missing baseline unit pixels'
        blue = sum(p[2] > max(p[0], p[1]) + 30 for p in body)
        if cell == '102,-33':
            assert blue == 0, 'Fully hidden body must not retain normal player color'
            # A vanished silhouette would be dark terrain, not translucent white.
            assert sum(min(p) for p in body)/len(body) > 85, 'Hidden silhouette is not visible'
        elif cell == '101,-34':
            assert 0 < blue < len(body), 'Partial occlusion must retain both normal and white body pixels'
            hidden = [p for p in body if p[2] <= max(p[0], p[1]) + 30]
            assert sum(min(p) for p in hidden)/len(hidden) > 85, 'Partial silhouette was painted over'
        else:
            assert blue == len(body), f'{cell}: visible ramp/roof/approach body was whitened'
            # Small palette-animation/edge differences are tolerated, not a white overlay.
            assert max(differences) <= 32, f'{cell}: visible body received an unwanted tint'
        print(f'{cell} ramp={ramp}: normal={blue}/{len(body)} PASS')
        checked.add(cell)
    assert len(checked) == 12, 'Expected eight ramp and four visibility reference positions'
    print('PASS: actual unit pixels on four ramp directions, full and partial occlusion')


if __name__ == '__main__':
    main()
