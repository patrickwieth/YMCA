import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class FactionNavigationTests(unittest.TestCase):
    def test_play_is_first_and_management_routes_to_library(self):
        menu = (ROOT / 'mods/ca/chrome/mainmenu.yaml').read_text(encoding='utf-8')
        self.assertLess(menu.index('Button@CUSTOM_PLAY_BUTTON:'), menu.index('Button@SINGLEPLAYER_BUTTON:'))
        source = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs').read_text(encoding='utf-8')
        self.assertIn('Game.OpenWindow("CUSTOM_FACTION_LIBRARY_PANEL"', source)
        self.assertIn('("CUSTOM_FACTION_BUTTON").OnClick = () => Open(false)', source)
        self.assertIn('("CUSTOM_PLAY_BUTTON").OnClick = () => Open(true)', source)
        self.assertIn('if (editorSession != null) editorSession.Save(roster)', source)
        self.assertIn('factionName.IsDisabled = () => true', source)

    def test_creation_is_play_only_deletion_is_confirmed_and_editor_is_nested(self):
        source = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLibraryLogic.cs').read_text(encoding='utf-8')
        self.assertIn('if (playMode) Button(body, "New faction"', source)
        self.assertIn('if (!playMode) return;', source)
        self.assertIn('confirmText: "Delete faction"', source)
        self.assertIn('onConfirm: () => Run(() => { library.Delete(entry.Path); List(); })', source)
        self.assertIn('"editorSession", session', source)
        self.assertIn('Save = r => library.Update(path, r)', source)
        self.assertIn('"Vehicles", "Infantry", "Ships", "Aircraft", "Buildings"', source)

    def test_light_turret_hit_testing_drawing_and_items_share_display_scale(self):
        source = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        self.assertIn('int GridCell(bool turret) => turret && LightBody ? 20 : Cell;', source)
        self.assertIn('w * cell, h * cell).Contains(point)', source)
        self.assertIn('x = (point.X - o.X) / cell', source)
        self.assertIn('o.X + p.X * cell, o.Y + p.Y * cell', source)
        self.assertIn('Color.Lime : Color.Red, true, cell)', source)
        for kind in ('Walker', 'Tripod', 'LightVehicle', 'Wheeled', 'Bike', 'Hover'):
            self.assertIn('case CustomVehicleSilhouetteKind.' + kind, source)


if __name__ == '__main__':
    unittest.main()
