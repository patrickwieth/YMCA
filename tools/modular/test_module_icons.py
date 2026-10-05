import re
import unittest
from PIL import Image
from generate_module_icons import ROOT, OUT, IDS, render, atlas, contact_sheet


class ModuleIconTests(unittest.TestCase):
    def test_all_spatial_modules_have_original_rgba_icons(self):
        source = (ROOT / 'OpenRA.Mods.CA/Modular/CustomVehicleSpaceDemo.cs').read_text(encoding='utf-8')
        self.assertEqual(set(IDS), set(re.findall(r'new Module\("([^"]+)"', source)))
        for id, expected in render().items():
            with Image.open(OUT / f'{id}.png') as image:
                self.assertEqual(image.mode, 'RGBA')
                self.assertEqual(image.size, (64, 48))
                self.assertEqual(image.tobytes(), expected.tobytes(), id)
                alpha = image.getchannel('A')
                self.assertEqual(alpha.getextrema(), (0, 255))
                self.assertGreater(len(set(image.getdata())), 8, id)

    def test_atlas_regions_and_review_sheet_are_reproducible(self):
        images = render()
        with Image.open(OUT / 'module-icons.png') as sheet:
            self.assertEqual(sheet.tobytes(), atlas(images).tobytes())
            chrome = (ROOT / 'mods/ca/chrome.yaml').read_text(encoding='utf-8')
            for i, id in enumerate(IDS):
                x, y = i % 3 * 64, i // 3 * 48
                self.assertIn(f'\t\t{id}: {x}, {y}, 64, 48', chrome)
                self.assertEqual(sheet.crop((x, y, x+64, y+48)).tobytes(), images[id].tobytes())
        with Image.open(ROOT / 'docs/modular/module-icons-preview.png') as preview:
            self.assertEqual(preview.tobytes(), contact_sheet(images).tobytes())

    def test_ui_uses_icons_in_sidebar_options_grid_and_hand_without_front_label(self):
        canvas = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        sidebar = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomVehicleSpaceLogic.cs').read_text(encoding='utf-8')
        icon = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleModuleIconWidget.cs').read_text(encoding='utf-8')
        self.assertIn('CustomVehicleModuleIconWidget.DrawIcon(id', canvas)
        self.assertIn('ButtonIcon(button', sidebar)
        self.assertIn('OptionIcon(item', sidebar)
        self.assertIn('"MODULAR_PART_DROPDOWN_TEMPLATE"', sidebar)
        self.assertIn('EventBoundsContains(int2 location) => false', icon)
        self.assertNotIn('FRONT  -->', canvas)
        self.assertNotIn('CreateActor(', icon)


if __name__ == '__main__':
    unittest.main()
