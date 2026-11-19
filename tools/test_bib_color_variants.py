"""GDI joints only, neutral concrete; unchanged Nod lower vertical faces."""
import hashlib
import json
import unittest
from PIL import Image
from art.generate_bib_color_variants import ROOT, OUT, CONCRETE, NOD_SIDES, GDI_JOINTS, mask_for


class BibColorVariantsTests(unittest.TestCase):
    def test_masks(self):
        entries = json.loads((OUT/'manifest.json').read_text())
        self.assertEqual(len(entries), 9)
        for e in entries:
            with self.subTest(mask=e['mask']):
                folder = ROOT/f"mods/ca/bits/{e['family']}/buildings"
                source = Image.open(folder/e['source']).convert('RGBA')
                indexed = Image.open(folder/e['mask'])
                mask = indexed.convert('RGBA')
                self.assertEqual(indexed.mode, 'P')
                self.assertEqual(mask.size, source.size)
                self.assertGreater(e['pixels'], 0)
                for key in ['source', 'mask']:
                    self.assertEqual(hashlib.sha256((folder/e[key]).read_bytes()).hexdigest(), e[key+'_sha256'])
                for x in range(mask.width):
                    occupied = [y for y in range(source.height) if source.getpixel((x,y))[3]]
                    for y in range(mask.height):
                        s = source.getpixel((x,y)); m = mask.getpixel((x,y))
                        if e['family'] == 'gdi':
                            expected = 255 if s[:3] == (0,0,0) else 166
                            self.assertEqual(m[3], expected if s[3] == 255 and s[:3] in GDI_JOINTS else 0)
                            if s[:3] == CONCRETE: self.assertEqual(m[3], 0)
                        elif m[3]:
                            self.assertEqual(m[3], 255)
                            self.assertEqual(s[3], 255)
                            self.assertIn(s[:3], NOD_SIDES)
                            self.assertLessEqual(max(occupied)-y, 6)
                generated, alpha = mask_for(source, e['family'], *e['cells'])
                self.assertEqual(generated.tobytes(), indexed.tobytes())
                self.assertEqual(generated.getpalette(), indexed.getpalette())
                stored = indexed.info['transparency']
                if isinstance(stored, int): stored = bytes(0 if i == stored else 255 for i in range(len(alpha)))
                self.assertEqual(alpha, stored)

    def test_runtime_no_longer_binds_corner_masks(self):
        sequences = (ROOT/'mods/ca/sequences/bib-player-color-variants.yaml').read_text()
        self.assertNotIn('-corner-remap.png', sequences)
        self.assertEqual(sequences.count('-joint-remap.png'), 5)
        palettes = (ROOT/'mods/ca/rules/palettes.yaml').read_text()
        self.assertIn('Name: gdi-bib-joints', palettes)
        self.assertNotIn('Filename: bits/gdi/buildings/bib1x1-corner-remap.png', palettes)

if __name__ == '__main__':
    unittest.main()
