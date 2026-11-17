"""Generate fixed-grid Soviet bases and joint masks without overwriting supplied PNGs."""
from pathlib import Path
from PIL import Image
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'mods/ca/bits/soviet/buildings'
OUTPUT = SOURCE / 'remap'
GRID = SOURCE / 'grid'
# Preserve the approved six shades; add black edges and the remaining joint shades.
# Concrete (including near-identical variants), brown dirt and transparency stay untouched.
JOINTS = [(175, 182, 185), (85, 88, 99), (75, 78, 89),
          (104, 109, 118), (135, 142, 149), (62, 63, 74), (0, 0, 0)]
EXTRA_SHADES = [(106, 91, 100), (167, 174, 177), (98, 89, 99),
                (82, 85, 96), (154, 158, 163), (128, 143, 170),
                (118, 108, 116), (92, 95, 106), (162, 168, 172),
                (152, 156, 161), (195, 202, 207)]


def joint_indices(image):
    pixels = list(image.getdata())
    width, height = image.size
    mapping = {rgb: index for index, rgb in enumerate(JOINTS + EXTRA_SHADES, 1)}
    anchors = {i for i, rgba in enumerate(pixels) if rgba[3] == 255 and rgba[:3] in JOINTS}
    result = []
    for i, rgba in enumerate(pixels):
        x, y = i % width, i // width
        # Extra antialias/shadow shades must actually touch an authored line, not a remote chip.
        near_line = any(ny * width + nx in anchors
                        for ny in range(max(0, y - 1), min(height, y + 2))
                        for nx in range(max(0, x - 1), min(width, x + 2)))
        result.append(mapping.get(rgba[:3], 0) if rgba[3] == 255 and (i in anchors or near_line) else 0)
    return result


def grid_bib(original, tile, nx, ny):
    # Integer translations of the approved native 1x1: 64x32 half-cell panels.
    # Clip to the original silhouette, so registrations/footprints never change.
    result = Image.new('RGBA', original.size, (147, 151, 156, 255))
    for x, y in sorted(((x, y) for x in range(nx) for y in range(ny)), key=lambda p: sum(p)):
        result.alpha_composite(tile, ((ny - 1 + x - y) * 64, (x + y) * 32))
    result.putalpha(original.getchannel('A'))
    return result


def generate():
    OUTPUT.mkdir(exist_ok=True)
    GRID.mkdir(exist_ok=True)
    tile = Image.open(SOURCE / 'bib1x1.png').convert('RGBA')
    manifest = []
    for source in sorted(SOURCE.glob('bib*.png')):
        original = Image.open(source).convert('RGBA')
        nx, ny = map(int, re.fullmatch(r'bib(\d+)x(\d+)\.png', source.name).groups())
        image = grid_bib(original, tile, nx, ny)
        image.save(GRID / source.name)
        mask = Image.new('P', image.size)
        palette = [0] * 768
        for index, rgb in enumerate(JOINTS + EXTRA_SHADES, 1):
            # Pure black remains black under multiplicative remapping. Lift only its
            # mask luminance so it becomes a dark player-color edge (base stays black).
            # PaletteFromPng/PlayerColorRemap expects premultiplied RGB. Preserve
            # the earlier 65% appearance of non-black joints, not a brighter ramp.
            palette[index * 3:index * 3 + 3] = ((128, 128, 128) if rgb == (0, 0, 0)
                                               else tuple(round(c * 166 / 255) for c in rgb))
        mask.putpalette(palette)
        pixels = joint_indices(image)
        mask.putdata(pixels)
        # Replace black edges completely: the old 65% dark overlay still looked
        # black. Keep the approved subtle blend for the other joint shades.
        alpha = [0] + [166] * (len(JOINTS) + len(EXTRA_SHADES))
        alpha[7] = 255
        mask.save(OUTPUT / source.name, transparency=bytes(alpha))
        manifest.append({'source': source.name, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                         'mask_pixels': sum(p != 0 for p in pixels), 'size': image.size,
                         'grid_cells': [nx, ny], 'panel_pixels': [64, 32],
                         'grid_sha256': hashlib.sha256((GRID / source.name).read_bytes()).hexdigest()})
    (OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    generate()
