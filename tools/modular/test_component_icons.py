import unittest
from PIL import Image
from generate_component_icons import ROOT, OUT, SOURCES, atlas


class ComponentIconTests(unittest.TestCase):
    def test_user_sources_remain_full_resolution_rgba(self):
        for name in SOURCES.values():
            with Image.open(OUT / name) as image:
                self.assertEqual(image.size, (256, 256))
                self.assertEqual(image.mode, 'RGBA')
                self.assertEqual(image.getchannel('A').getextrema(), (0, 255))

    def test_downsampled_atlas_is_reproducible_and_power_of_two(self):
        with Image.open(OUT / 'component-icons.png') as image:
            self.assertEqual(image.mode, 'RGBA')
            self.assertEqual(image.size, (256, 64))
            self.assertEqual(image.tobytes(), atlas().tobytes())
            for size in image.size:
                self.assertEqual(size & (size - 1), 0)
            self.assertIsNone(image.crop((192, 0, 256, 64)).getchannel('A').getbbox())
            chrome = (ROOT / 'mods/ca/chrome.yaml').read_text(encoding='utf-8')
            self.assertIn('Image: ca|bits/modular/component-icons.png', chrome)
            for i, key in enumerate(SOURCES):
                self.assertIn(f'\t\t{key}: {i * 64}, 0, 64, 64', chrome)
                self.assertIsNotNone(image.crop((i * 64, 0, i * 64 + 64, 64)).getchannel('A').getbbox())

    def test_exact_part_mapping_and_pending_pickup_icons(self):
        icon = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleModuleIconWidget.cs').read_text(encoding='utf-8')
        for role, part, key in [('engine', 'diesel', 'engine-basic'),
                                ('engine', 'diesel-large', 'engine-improved'),
                                ('generator', 'baseline-generator', 'generator-basic')]:
            self.assertIn(f'("{role}", "{part}") => "{key}"', icon)
        self.assertIn('_ => module', icon)
        self.assertIn('"modular-component-icons" : "modular-module-icons"', icon)
        sidebar = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomVehicleSpaceLogic.cs').read_text(encoding='utf-8')
        self.assertIn('canvas.GetModuleIcon = module =>', sidebar)
        self.assertIn('pendingRole == role ? pendingPart : profile().Parts[role]', sidebar)
        self.assertIn('ButtonIcon(button, () => CustomVehicleModuleIconWidget.ForPart(module, profile().Parts[role]))', sidebar)
        self.assertIn('CustomVehicleModuleIconWidget.ForPart(module, id)', sidebar)
        canvas = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        self.assertIn('DrawIcon(GetModuleIcon(id)', canvas)
        self.assertIn('const int Cell = 28;', canvas)


if __name__ == '__main__':
    unittest.main()
