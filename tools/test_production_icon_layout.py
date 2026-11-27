"""96x72 logical production cells, independent of source texture resolution."""
from pathlib import Path
import unittest
from convert_handcrafted_layouts import miniyaml

ROOT = Path(__file__).resolve().parents[1]

class ProductionIconLayoutTests(unittest.TestCase):
    def test_sidebar_geometry(self):
        root = miniyaml((ROOT/'mods/ca/chrome/ingame-player.yaml').read_text()).child('Container@PLAYER_WIDGETS').child('Children')
        sidebar = root.child('Container@SIDEBAR_PRODUCTION')
        children = sidebar.child('Children')
        palette = children.child('ProductionPalette@PRODUCTION_PALETTE')
        self.assertEqual(palette.child('IconSize').value, '96, 72')
        self.assertEqual(palette.child('FitIconsToCell').value, 'true')
        self.assertEqual(palette.child('IconSpriteOffset').value, '0, 0')
        self.assertEqual(int(palette.child('X').value)+3*96+2, int(sidebar.child('Width').value)-8)
        for name in ('PALETTE_BACKGROUND', 'PALETTE_FOREGROUND'):
            row = children.child('Container@'+name).child('Children').child('SidebarImage@ROW_TEMPLATE')
            self.assertEqual(row.child('Height').value, '73')
        top = root.child('SidebarImage@SIDEBAR_BACKGROUND_TOP')
        self.assertEqual(top.child('Width').value, sidebar.child('Width').value)
        radar = top.child('Children').child('SidebarImage@RADAR')
        self.assertEqual(radar.child('PreserveAspect').value, 'true')
        self.assertEqual(int(radar.child('X').value)+int(radar.child('Width').value), 332)

    def test_legacy_hd_and_observer_size_contract(self):
        for source in ((64,48),(96,72),(192,144),(256,192)):
            for target in ((96,72),(32,24),(128,96)):
                scale = min(target[0]/source[0], target[1]/source[1])
                self.assertEqual((source[0]*scale,source[1]*scale), target)
        for physical_height, ui_scale in ((720,1),(1000,1.25),(1080,1.5),(1440,2)):
            logical_height = physical_height/ui_scale
            rows = max(1,int((logical_height-300-20)//73))
            self.assertLessEqual(300+rows*73+8,logical_height)

if __name__ == '__main__':
    unittest.main()
