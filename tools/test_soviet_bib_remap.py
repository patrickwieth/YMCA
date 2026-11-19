"""Fixed native grid, expanded joint colors, preserved source art and silhouettes."""
import hashlib
import json
from pathlib import Path
import unittest
from PIL import Image
from art.generate_soviet_bib_remap import JOINTS, EXTRA_SHADES, joint_indices

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'mods/ca/bits/soviet/buildings'


class SovietBibRemapTests(unittest.TestCase):
    def test_grid_and_masks(self):
        entries = json.loads((SOURCE / 'remap/manifest.json').read_text())
        self.assertEqual(len(entries), 5)
        tile = Image.open(SOURCE / 'bib1x1.png').convert('RGBA')
        for entry in entries:
            with self.subTest(bib=entry['source']):
                path = SOURCE / entry['source']
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), entry['sha256'])
                original = Image.open(path).convert('RGBA')
                grid_path = SOURCE / 'grid' / entry['source']
                grid = Image.open(grid_path).convert('RGBA')
                self.assertEqual(hashlib.sha256(grid_path.read_bytes()).hexdigest(), entry['grid_sha256'])
                self.assertEqual(grid.size, original.size)
                self.assertEqual(grid.getchannel('A').tobytes(), original.getchannel('A').tobytes())
                nx, ny = entry['grid_cells']
                # Same source pixels at every half-cell joint, independent of bib size.
                for x in range(nx):
                    for y in range(ny):
                        ox, oy = (ny - 1 + x - y) * 64, (x + y) * 32
                        for px, py in [(64, 16), (32, 32), (64, 32), (96, 32), (64, 48)]:
                            self.assertEqual(grid.getpixel((ox + px, oy + py)), tile.getpixel((px, py)))
                mask = Image.open(SOURCE / 'remap' / entry['source'])
                self.assertEqual(mask.mode, 'P')
                self.assertEqual(mask.size, grid.size)
                alpha = mask.info['transparency']
                self.assertEqual(alpha[0], 0)
                self.assertEqual(alpha[7], 255)
                self.assertTrue(all(a == 166 for i, a in enumerate(alpha) if i not in (0, 7)))
                indices = list(mask.getdata())
                self.assertEqual(indices, joint_indices(grid))
                self.assertEqual(sum(i != 0 for i in indices), entry['mask_pixels'])
                for rgba, index in zip(grid.getdata(), indices):
                    self.assertIn(index, range(1 + len(JOINTS) + len(EXTRA_SHADES)))
                    if rgba[3] != 255 or rgba[:3] in {(147, 151, 156), (133, 116, 96)}:
                        self.assertEqual(index, 0)
                    if rgba == (0, 0, 0, 255):
                        self.assertEqual(index, 7)
                palette = mask.getpalette()
                self.assertEqual(palette[21:24], [128, 128, 128])
                for index, rgb in enumerate(JOINTS + EXTRA_SHADES, 1):
                    if index != 7:
                        self.assertEqual(palette[index * 3:index * 3 + 3], [round(c * 166 / 255) for c in rgb])
                rgba_mask = list(mask.convert('RGBA').getdata())
                for base, overlay in zip(grid.getdata(), rgba_mask):
                    if base == (0, 0, 0, 255):
                        self.assertEqual(overlay, (128, 128, 128, 255))
                self.assertLess(entry['mask_pixels'], sum(p[3] > 0 for p in grid.getdata()) * 0.18)

    def test_extra_shades_only_along_lines(self):
        image = Image.new('RGBA', (7, 3), (147, 151, 156, 255))
        image.putpixel((0, 1), (*JOINTS[0], 255))
        image.putpixel((1, 1), (*EXTRA_SHADES[0], 255))
        image.putpixel((5, 1), (*EXTRA_SHADES[0], 255))
        indices = joint_indices(image)
        self.assertNotEqual(indices[8], 0)
        self.assertEqual(indices[12], 0)


if __name__ == '__main__':
    unittest.main()
