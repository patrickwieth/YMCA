"""Native GDI grid phase, silhouette, original preservation and explicit bindings."""
import hashlib
import json
import unittest
from PIL import Image
from art.generate_gdi_bib_grid import SOURCE, MANIFEST, ROOT, SIZES


class GdiBibGridTests(unittest.TestCase):
    def test_grid(self):
        entries = json.loads(MANIFEST.read_text())
        self.assertEqual([e['cells'] for e in entries], [list(s) for s in SIZES])
        tile = Image.open(SOURCE / 'bib1x1.png').convert('RGBA')
        yaml = (ROOT / 'mods/ca/sequences/gdi-building-art.yaml').read_text()
        for e in entries:
            with self.subTest(source=e['source']):
                original = Image.open(SOURCE / e['source']).convert('RGBA')
                grid = Image.open(SOURCE / e['runtime']).convert('RGBA')
                self.assertEqual(original.size, grid.size)
                self.assertEqual(original.getchannel('A').tobytes(), grid.getchannel('A').tobytes())
                for key in ['source', 'runtime']:
                    self.assertEqual(hashlib.sha256((SOURCE / e[key]).read_bytes()).hexdigest(), e[key + '_sha256'])
                self.assertIn('Filename: bits/gdi/buildings/' + e['runtime'], yaml)
                nx, ny = e['cells']
                for x in range(nx):
                    for y in range(ny):
                        ox, oy = (ny - 1 + x - y) * 64, (x + y) * 32
                        # Same panel centers and both crossing joints in every cell.
                        for px, py in [(64,16),(32,32),(64,32),(96,32),(64,48),(48,24),(80,40)]:
                            self.assertEqual(grid.getpixel((ox+px,oy+py)), tile.getpixel((px,py)))


if __name__ == '__main__':
    unittest.main()
