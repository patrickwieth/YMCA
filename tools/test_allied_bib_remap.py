"""Only original opaque black rim pixels may be recolored on Allied foundations."""
from pathlib import Path
import hashlib
import json
import unittest
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'mods/ca/bits/allies/buildings'


class AlliedBibRemapTests(unittest.TestCase):
    def test_exact_black_rim_only(self):
        entries = json.loads((SOURCE / 'remap/manifest.json').read_text())
        self.assertEqual(len(entries), 5)
        for entry in entries:
            with self.subTest(bib=entry['source']):
                path = SOURCE / entry['source']
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), entry['sha256'])
                original = Image.open(path).convert('RGBA')
                mask = Image.open(SOURCE / 'remap' / entry['source'])
                self.assertEqual(mask.mode, 'P')
                self.assertEqual(mask.size, original.size)
                self.assertEqual(mask.info['transparency'], 0)
                self.assertEqual(mask.getpalette()[3:6], [128, 128, 128])
                indices = list(mask.getdata())
                self.assertEqual(sum(indices), entry['rim_pixels'])
                self.assertGreater(sum(indices), 0)
                self.assertEqual(indices, [int(c == (0, 0, 0, 255)) for c in original.getdata()])
                for base, overlay in zip(original.getdata(), mask.convert('RGBA').getdata()):
                    self.assertEqual(overlay[3], 255 if base == (0, 0, 0, 255) else 0)


if __name__ == '__main__':
    unittest.main()
