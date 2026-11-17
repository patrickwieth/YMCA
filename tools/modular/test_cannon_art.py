import json
import unittest
from PIL import Image
from generate_cannon_art import ROOT, ART, bindings, assets, cs_source, key


class CannonArtTests(unittest.TestCase):
    def test_registry_and_chrome_are_reproducible(self):
        sheet, chrome = assets()
        with Image.open(ROOT / 'mods/ca/bits/modular/cannon-icons.png') as image:
            self.assertEqual(image.tobytes(), sheet.tobytes())
            self.assertEqual(image.mode, 'RGBA')
            for n in image.size:
                self.assertEqual(n & (n - 1), 0)
        self.assertIn(chrome, (ROOT / 'mods/ca/chrome.yaml').read_text(encoding='utf-8'))
        self.assertEqual(cs_source(), (ROOT / 'OpenRA.Mods.CA/Modular/CustomVehicleCannonArt.cs').read_text(encoding='utf-8'))

    def test_pixels_and_rotations_come_from_original_sources(self):
        names = sorted({b['image'] for b in bindings()['bindings'] if b['image']})
        sheet, chrome = assets()
        self.assertEqual(len(names), 9)
        for i, name in enumerate(names):
            with Image.open(ART / name) as source:
                for rotation in range(2):
                    sprite = source if not rotation else source.transpose(Image.Transpose.ROTATE_270)
                    slot = i * 2 + rotation
                    x, y = slot % 4 * 256, slot // 4 * 256
                    self.assertEqual(sheet.crop((x, y, x + sprite.width, y + sprite.height)).tobytes(), sprite.tobytes())
                    self.assertIn(key(name) + ('-rotated' if rotation else '') + ':', chrome)
        self.assertIsNone(sheet.crop((0, 1280, 1024, sheet.height)).getchannel('A').getbbox())

    def test_variants_stay_reserved_and_bindings_are_native_choices(self):
        rows = bindings()['bindings']
        self.assertEqual(len({(b['hull'], b['weapon']) for b in rows}), len(rows))
        used = {b['image'] for b in rows if b['image']}
        for path in ART.glob('*.png'):
            if path.name != '152mm short.png' and any(word in path.stem for word in ('burst', 'long', 'recoil', 'short')):
                self.assertNotIn(path.name, used)
        catalog = json.loads((ROOT / 'mods/ca/modular/designer-catalog.json').read_text(encoding='utf-8'))
        by_hull = {a['options']['chassis'][0]: a['options'] for a in catalog['assemblies']}
        self.assertEqual(len({b['hull'] for b in rows if b['image']}), 21)
        for b in rows:
            self.assertIn(b['weapon'], by_hull[b['hull']]['weapon'])
            self.assertNotIn('damage', b)
            self.assertTrue(b['native_weapon'])
        self.assertEqual(next(b for b in rows if b['hull'] == 'allied-medium-hull')['image'], '88mm.png')
        for b in rows:
            if b['native_weapon'] == '30mm':
                self.assertEqual(b['label'], '75mm Cannon')
                self.assertEqual(b['image'], '75mm.png')

    def test_grid_dropdown_and_pending_pickup_use_the_same_binding(self):
        logic = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomVehicleSpaceLogic.cs').read_text(encoding='utf-8')
        self.assertIn('CustomVehicleCannonArt.For(profile().Parts["chassis"], weapon)', logic)
        self.assertIn('pendingRole == "weapon" ? pendingPart : profile().Parts["weapon"]', logic)
        self.assertIn('PartIcon(module, id)', logic)
        self.assertIn('PartLabel(role, id)', logic)
        canvas = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        self.assertIn('DrawCannon(GetCannonArt()', canvas)
        self.assertIn('void DrawWeapon', canvas)  # explicit fallback for missing/specialized art
        utility = (ROOT / 'OpenRA.Mods.CA/UtilityCommands/CheckCustomFactionPreviewsCommand.cs').read_text(encoding='utf-8')
        self.assertIn('a.LocalOffset.Length >= art.Barrels', utility)
        self.assertIn('art.SourceWeapon', utility)


if __name__ == '__main__':
    unittest.main()
