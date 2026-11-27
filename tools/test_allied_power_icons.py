from pathlib import Path
import hashlib
import json
import unittest
from PIL import Image
from convert_handcrafted_layouts import miniyaml

ROOT = Path(__file__).resolve().parents[1]

class AlliedPowerIconTests(unittest.TestCase):
    def test_source_preservation_and_sizes(self):
        archive = ROOT / 'art-source/allied-power-icons'
        for record in json.loads((archive / 'provenance.json').read_text()):
            self.assertEqual(hashlib.sha256((ROOT / record['source']).read_bytes()).hexdigest(), record['sha256'])
            path = ROOT / record['runtime']
            with Image.open(path) as image:
                self.assertEqual(image.size, (192, 144))
                self.assertEqual(image.getchannel('A').getextrema(), (255, 255))
            with Image.open(archive / (path.stem + '-256x192.png')) as image:
                self.assertEqual(image.size, (256, 192))

    def test_allied_bindings_only(self):
        sequences = miniyaml((ROOT / 'mods/ca/sequences/allies.yaml').read_text())
        for actor, filename in [('powr.allies', 'power-plant'), ('apwr.allies', 'fusion-power-plant')]:
            icon = sequences.child(actor).child('icon')
            self.assertEqual(icon.child('Filename').value, f'bits/allies/icons/{filename}.png')
            self.assertEqual(icon.child('ButtonLabelOffset').value, '0,-2')
            self.assertEqual(icon.child('ButtonLabelFont').value, 'OsShp')
        common = miniyaml((ROOT / 'mods/ca/sequences/structures.yaml').read_text())
        self.assertEqual(common.child('powr').child('icon').child('Filename').value, 'powricon.shp')
        self.assertEqual(common.child('apwr').child('icon').child('Filename').value, 'apwricon.shp')

    def test_retired_water_is_not_bound(self):
        self.assertNotIn('WithIdleOverlay@WATERCONTACT', (ROOT / 'mods/ca/rules/misc/structures.yaml').read_text())
        self.assertNotIn('oil-platform-water.png', (ROOT / 'mods/ca/sequences/structures.yaml').read_text())
        self.assertFalse((ROOT / 'mods/ca/bits/terrain/environment/oil-platform-water.png').exists())

if __name__ == '__main__':
    unittest.main()
