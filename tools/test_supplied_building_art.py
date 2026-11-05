import unittest
import re
from pathlib import Path
from PIL import Image
import audit_png_case

ROOT = Path(__file__).resolve().parents[1]


def block(text, name):
    tail = text.split(name + ':\n', 1)[1]
    return tail.split('\n\n', 1)[0]


class SuppliedBuildingArtTests(unittest.TestCase):
    def test_lowercase_pngs_and_exact_linux_paths(self):
        self.assertEqual(audit_png_case.audit(), [])

    def test_case_check_rejects_windows_case_alias(self):
        self.assertTrue(audit_png_case.exact_path('mods/ca/bits/gdi/buildings/barracks.png'))
        self.assertFalse(audit_png_case.exact_path('mods/ca/bits/gdi/buildings/BARRACKS.png'))

    def test_new_png_frames_exist(self):
        sequences = (ROOT / 'mods/ca/sequences/gdi-building-art.yaml').read_text()
        for line in sequences.splitlines():
            if line.strip().startswith('Filename:'):
                path = ROOT / 'mods/ca' / line.split(':', 1)[1].strip()
                with Image.open(path) as image:
                    image.verify()

    def test_gdi_rearward_placement_preserves_all_png_body_states(self):
        source = (ROOT / 'mods/ca/sequences/gdi-building-art.yaml').read_text()
        placement = (ROOT / 'mods/ca/sequences/gdi-building-placement.yaml').read_text()
        manifest = (ROOT / 'mods/ca/mod.yaml').read_text()
        self.assertLess(manifest.index('ca|sequences/gdi-building-art.yaml'),
                        manifest.index('ca|sequences/gdi-building-placement.yaml'))
        images = re.findall(r'^([^\s#]+\.gdi\.png):$', placement, re.M)
        self.assertEqual(len(images), 9)
        for image in images:
            original = block(source, image)
            override = block(placement, image)
            expected_states = []
            for state, fields in re.findall(r'^\t([^\s:]+):\n((?:\t\t[^\n]*\n?)*)', original, re.M):
                if 'Filename: bits/gdi/buildings/' not in fields:
                    continue
                expected_states.append(state)
                old = re.search(r'Offset: 0,(-?\d+)', fields)
                self.assertIn(f'\t{state}:\n\t\tOffset: 0,{int(old[1]) - 32}', override)
            self.assertEqual(len(re.findall(r'^\t[^\s:]+:', override, re.M)), len(expected_states))

    def test_checkpoint_restores_checkpoint_not_forward_command(self):
        rules = (ROOT / 'mods/ca/rules/husks.yaml').read_text()
        core = block(rules, 'FCOM.checkpoint.Husk')
        self.assertIn('Name: Ruin (Research Core)', core)
        self.assertEqual(core.count('IntoActor: fcom.checkpoint'), 2)
        ordinary = block(rules, 'FCOM.Husk')
        self.assertIn('Name: Ruin (Forward Command)', ordinary)
        self.assertEqual(ordinary.count('IntoActor: fcom\n'), 2)

    def test_research_source_unchanged_size_and_render_scale(self):
        with Image.open(ROOT / 'mods/ca/bits/misc/buildings/research core.png') as image:
            self.assertEqual(image.size, (1024, 1024))
        sequences = (ROOT / 'mods/ca/sequences/structures.yaml').read_text()
        core = block(sequences, 'fcom.checkpoint')
        self.assertEqual(core.count('Scale: 0.25'), 4)
        self.assertEqual(core.count('Offset: 0,68'), 4)


if __name__ == '__main__':
    unittest.main()
