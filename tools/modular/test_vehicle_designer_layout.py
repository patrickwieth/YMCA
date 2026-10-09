"""Check the designer's three columns at supported logical UI sizes (before UI scaling)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def widgets():
    text = (ROOT / 'mods/ca/chrome/custom-faction.yaml').read_text(encoding='utf-8')
    panel = text.split('Background@CUSTOM_FACTION_PANEL:', 1)[1].split('\nScrollPanel@MODULAR_PART_DROPDOWN_TEMPLATE:', 1)[0]
    starts = list(re.finditer(r'^\t\t\w+@(\w+):\n', panel, re.M))
    return {m.group(1): dict(re.findall(r'^\t\t\t(\w+): ([^\n]*)$',
            panel[m.end():starts[i + 1].start() if i + 1 < len(starts) else len(panel)], re.M))
            for i, m in enumerate(starts)}


def bounds(node, width, height):
    def value(field):
        terms = node.get(field, '0').replace('PARENT_WIDTH', str(width)).replace('PARENT_HEIGHT', str(height)).split('-')
        return int(terms[0]) - sum(int(t) for t in terms[1:])
    return tuple(value(k) for k in ('X', 'Y', 'Width', 'Height'))


class VehicleDesignerLayoutTests(unittest.TestCase):
    def test_grid_uses_full_height_and_sidebars_do_not_cover_it(self):
        nodes = widgets()
        for screen_width, screen_height in [(1280, 720), (1920, 1080), (2560, 1440), (3840, 2160)]:
            width, height = screen_width - 32, screen_height - 32
            gx, gy, gw, gh = bounds(nodes['LAYOUT'], width, height)
            self.assertEqual((gy, gh), (16, height - 32))
            for id in ('TITLE', 'TANK_NAME_LABEL', 'TANK_NAME', 'SIDEBAR', 'SELECTION', 'WARNING', 'STATUS', 'SAVE', 'BACK'):
                x, y, w, h = bounds(nodes[id], width, height)
                self.assertLessEqual(x + w, gx, id)
                self.assertGreaterEqual(y, 0, id)
                self.assertLessEqual(y + h, height, id)
            for id in ('PREVIEW_TITLE', 'PREVIEW_STATUS', 'PREVIEW_BACKGROUND', 'PREVIEW_ROTATION', 'PROPERTIES'):
                x, y, w, h = bounds(nodes[id], width, height)
                self.assertGreaterEqual(x, gx + gw, id)
                self.assertLessEqual(x + w, width, id)
                self.assertLessEqual(y + h, height, id)
            left = ['TITLE', 'TANK_NAME_LABEL', 'TANK_NAME', 'SIDEBAR', 'SELECTION', 'WARNING', 'STATUS', 'SAVE']
            for a, b in zip(left, left[1:]):
                _, ay, _, ah = bounds(nodes[a], width, height)
                _, by, _, _ = bounds(nodes[b], width, height)
                self.assertLessEqual(ay + ah, by, (a, b))
            sx, sy, sw, sh = bounds(nodes['SAVE'], width, height)
            bx, by, bw, bh = bounds(nodes['BACK'], width, height)
            self.assertEqual((sy, sh), (by, bh))
            self.assertLessEqual(sx + sw, bx)

    def test_faction_fields_and_legacy_controls_are_hidden_even_on_load_error(self):
        nodes = widgets()
        for id in ('FACTION_NAME_LABEL', 'FACTION_NAME', 'BASE', 'LOAD', 'DESIGN_LABEL', 'DESIGN',
                   'ADD', 'COPY', 'DELETE', 'TEMPLATES', 'PLAY'):
            self.assertEqual(nodes[id]['Visible'], 'false', id)
        self.assertEqual(nodes['TANK_NAME']['Width'], '220')
        self.assertEqual(nodes['PREVIEW_TITLE']['Y'], '16')
        self.assertEqual(nodes['PREVIEW_BACKGROUND']['Y'], '60')
        self.assertEqual(nodes['STATUS']['TooltipContainer'], 'CUSTOM_VEHICLE_TOOLTIP')
        source = (ROOT / 'OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs').read_text(encoding='utf-8')
        self.assertIn('status.Bounds.Width', source)
        self.assertIn('status.GetTooltipText', source)
        self.assertIn('factionName.Text = roster.Name', source, 'hide the name without losing faction identity')


if __name__ == '__main__':
    unittest.main()
