import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]


class DesignerPreviewTests(unittest.TestCase):
    def test_panel_and_logic_wire_actor_preview_without_changing_panel_size(self):
        chrome = (REPO / 'mods/ca/chrome/custom-faction.yaml').read_text(encoding='utf-8')
        logic = (REPO / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs').read_text(encoding='utf-8')
        self.assertIn('Width: 800\n\tHeight: 640', chrome)
        for name in ('VEHICLE_PREVIEW', 'PREVIEW_ROTATION', 'PREVIEW_STATUS'):
            self.assertIn('@' + name + ':', chrome)
            self.assertIn('"' + name + '"', logic)
        self.assertIn('preview.SetVehicle(compiler.PreviewActor(p), p.BaseFaction)', logic)

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
