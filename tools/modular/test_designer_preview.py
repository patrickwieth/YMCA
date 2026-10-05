import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]


class DesignerPreviewTests(unittest.TestCase):
    def test_unified_panel_preserves_actor_preview_and_roster_controls(self):
        chrome = (REPO / 'mods/ca/chrome/custom-faction.yaml').read_text(encoding='utf-8')
        logic = (REPO / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs').read_text(encoding='utf-8')
        self.assertIn('Width: 1120\n\tHeight: 740', chrome)
        for name in ('SIDEBAR', 'LAYOUT', 'PROPERTIES', 'BASE', 'LOAD', 'DESIGN', 'SAVE', 'PLAY'):
            self.assertIn('@' + name + ':', chrome)
        self.assertNotIn('SPACE_DEMO', chrome)
        self.assertFalse((REPO / 'mods/ca/chrome/custom-vehicle-space.yaml').exists())
        for name in ('VEHICLE_PREVIEW', 'PREVIEW_ROTATION', 'PREVIEW_STATUS'):
            self.assertIn('@' + name + ':', chrome)
            self.assertIn('"' + name + '"', logic)
        self.assertIn('preview.SetVehicle(compiler.PreviewActor(p), p.BaseFaction)', logic)

    def test_progressive_fields_are_hidden_and_native_choices_commit_on_drop(self):
        source = (REPO / 'OpenRA.Mods.CA/Widgets/Logic/CustomVehicleSpaceLogic.cs').read_text(encoding='utf-8')
        canvas = (REPO / 'OpenRA.Mods.CA/Widgets/CustomVehicleSpaceWidget.cs').read_text(encoding='utf-8')
        self.assertIn('IsVisible = visible', source)
        self.assertIn('Field("Turret / mount", 1, Color.White, () => canvas.Layout.HasChassis)', source)
        self.assertIn('Field(title, row, color, () => Ready)', source)
        self.assertIn('compiler.CompatibleOptions(profile(), "chassis")', source)
        self.assertIn('canvas.OnInstall', source)
        self.assertIn('else if (OnInstall(Selected))', canvas)
        self.assertIn('Dictionary<CustomVehicleDesign, CustomVehicleSpaceDemo>', source)
        self.assertIn('OnSelectionCancelled', source)

    def test_properties_follow_configured_parts_and_planning_export_requires_confirmation(self):
        source = (REPO / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs').read_text(encoding='utf-8')
        self.assertIn('propertyPanel.AddChild(row)', source)
        self.assertNotIn('propertyPanel.ContentHeight =', source)
        self.assertIn('compiler.Calculate(p)', source)
        self.assertIn('design.Parts = before', source)
        self.assertIn('save.OnClick = () => ConfirmPlanningExport(', source)
        self.assertIn('play.OnClick = () => ConfirmPlanningExport(', source)
        self.assertIn('new CustomVehicleSpaceLogic(widget, modData, compiler, Profile', source)

    def test_preview_is_render_only_and_supports_voxel_components(self):
        source = (REPO / 'OpenRA.Mods.CA/Widgets/CustomVehiclePreviewWidget.cs').read_text(encoding='utf-8')
        self.assertIn('IRenderActorPreviewVoxelsInfo', source)
        self.assertIn('DynamicFacingInit', source)
        self.assertIn('EnableScissor(RenderBounds)', source)
        self.assertIn('DisableScissor()', source)
        self.assertNotIn('CreateActor(', source)
        self.assertNotIn('IssueOrder(', source)
        self.assertIn('field.SetValue(clone,', source)
        self.assertNotIn('field.SetValue(trait,', source)

    def test_titan_uses_actual_sprite_frames_not_a_voxel_claim(self):
        source = (REPO / 'mods/ca/sequences/gdi.yaml').read_text(encoding='utf-8').split('\ntitn:', 1)[1].split('\ntitn.railgun:', 1)[0]
        self.assertIn('Filename: titan.shp', source)
        self.assertIn('Facings: 32', source)
        self.assertIn('Start: 416', source)


if __name__ == '__main__':
    unittest.main()
