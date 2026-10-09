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

    def test_lobby_identity_is_map_local_and_does_not_change_gameplay_ids(self):
        source = (ROOT / 'OpenRA.Mods.Cameo/Widgets/Logic/LobbyLogic.cs').read_text(encoding='utf-8')
        self.assertIn('map.Package.GetStream("custom-faction.json")', source)
        self.assertIn('customIdentityMap = map.Uid; customIdentity = null;', source)
        self.assertIn('!customIdentity.AppliesTo(client.Faction)', source)
        self.assertEqual(source.count('SetupCustomFactionIdentity(template, client);'), 2)
        presentation = source.split('void SetupCustomFactionIdentity(')[1].split('void UpdatePlayerList()')[0]
        self.assertIn('identity.Side', presentation)
        self.assertIn('WidgetUtils.TruncateText(identity.Name', presentation)
        self.assertIn('flag.GetImageCollection = () => "flags"', presentation)
        self.assertIn('flag.GetImageName = () => identity.BaseFaction', presentation)
        self.assertNotIn('IssueOrder', presentation)
        self.assertNotIn('client.Faction =', presentation)
        self.assertNotIn('GetMessage(identity.Name)', presentation)
        self.assertIn('row.GetOrNull("FACTION") as DropDownButtonWidget', presentation)

    def test_all_containers_hit_testing_drawing_and_items_share_display_scale(self):
        source = (ROOT / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        self.assertIn('const int Cell = CustomVehicleCanvasGeometry.Cell;', source)
        geometry = (ROOT / 'OpenRA.Mods.CA/Modular/CustomVehicleCanvasGeometry.cs').read_text(encoding='utf-8')
        self.assertIn('const int Cell = 96;', geometry)
        self.assertIn('input.Location - RenderOrigin + pan', source)
        self.assertIn('Game.Renderer.EnableScissor(RenderBounds)', source)
        self.assertIn('MouseButton.Middle', source)
        self.assertIn('int GridCell(bool turret) => Cell;', source)
        chrome = (ROOT / 'mods/ca/chrome/custom-faction.yaml').read_text(encoding='utf-8')
        self.assertIn('Width: WINDOW_WIDTH - 32\n\tHeight: WINDOW_HEIGHT - 32', chrome)
        self.assertNotIn('Button@ROTATE:', chrome)
        self.assertNotIn('Button@CANCEL:', chrome)
        self.assertIn('Keycode.R', source)
        self.assertIn('Keycode.ESCAPE', source)
        self.assertIn('Layout.ContainsCell(t, cx, cy)', source)
        self.assertIn('var cx = (point.X - o.X) / cell', source)
        self.assertIn('SnapWeaponSocket(turret, ref x, ref y)', source)
        self.assertIn('if (id == "weapon") DrawWeapon', source)
        self.assertIn('o.X + p.X * cell, o.Y + p.Y * cell', source)
        self.assertIn('Color.Lime : Color.Red, true, cell)', source)
        for kind in ('Walker', 'Tripod', 'LightVehicle', 'Wheeled', 'Bike', 'Hover', 'HeavyWalker', 'MiniDrone'):
            self.assertIn('case CustomVehicleSilhouetteKind.' + kind, source)


if __name__ == '__main__':
    unittest.main()
