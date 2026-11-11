import unittest
import re
import json
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

    def test_gdi_calibrated_placement_preserves_all_png_body_states(self):
        source = (ROOT / 'mods/ca/sequences/gdi-building-art.yaml').read_text()
        placement = (ROOT / 'mods/ca/sequences/gdi-building-placement.yaml').read_text()
        manifest = (ROOT / 'mods/ca/mod.yaml').read_text()
        self.assertLess(manifest.index('ca|sequences/gdi-building-art.yaml'),
                        manifest.index('ca|sequences/gdi-building-placement.yaml'))
        images = re.findall(r'^([^\s#]+\.gdi\.png):$', placement, re.M)
        calibration = json.loads((ROOT / 'tools/gdi-building-alignment.json').read_text())
        offsets = {entry['Image']: entry['Offset'] for entry in calibration['entries']}
        self.assertEqual(len(images), 13)
        self.assertEqual(set(images), set(offsets))
        for image in images:
            original = block(source, image)
            override = block(placement, image)
            expected_states = []
            for state, fields in re.findall(r'^\t([^\s:]+):\n((?:\t\t[^\n]*\n?)*)', original, re.M):
                if 'Filename: bits/gdi/buildings/' not in fields:
                    continue
                expected_states.append(state)
                x, y = offsets[image]
                self.assertIn(f'\t{state}:\n\t\tOffset: {x},{y}', override)
            self.assertEqual(len(re.findall(r'^\t[^\s:]+:', override, re.M)), len(expected_states))

    def test_new_gdi_factory_and_communications_bindings(self):
        rules = (ROOT / 'mods/ca/rules/gdi/building-art.yaml').read_text()
        sequences = (ROOT / 'mods/ca/sequences/gdi-building-art.yaml').read_text()
        for actor, image, filename, bib in (
                ('WEAP.TD', 'awep.gdi.png', 'weapons factory.png', 4),
                ('EYE', 'eye.gdi.png', 'communication center.png', 3),
                ('UPGC', 'upgc.gdi.png', 'upgrade center.png', 3)):
            actor_rules = block(rules, actor)
            self.assertIn('Image: ' + image, actor_rules)
            self.assertIn('-WithBuildingBib:', actor_rules)
            self.assertIn(f'Image: gdibib{bib}x{bib}', actor_rules)
            self.assertIn('IsDecoration: true', actor_rules)
            for state in ('idle', 'damaged-idle', 'dead'):
                fields = re.search(r'^\t' + state + r':\n((?:\t\t[^\n]*\n?)*)',
                                   block(sequences, image), re.M)[1]
                self.assertIn('Filename: bits/gdi/buildings/' + filename, fields)
                self.assertIn('Scale: 1', fields)
                self.assertIn('Length: 1', fields)
        self.assertIn('-WithProductionDoorOverlayCA:', block(rules, 'WEAP.TD'))
        self.assertIn('\tplace:\n\t\tFilename: bits/gdi/buildings/weapons factory.png',
                      block(sequences, 'awep.gdi.png'))
        # Existing radar remains separate from the advanced communications center.
        self.assertIn('Filename: bits/gdi/buildings/radar.png', block(sequences, 'hq.gdi.png'))
        eye_rules = block((ROOT / 'mods/ca/rules/structures.yaml').read_text(), 'EYE')
        self.assertEqual(len(re.findall(r'^\tInherits@', eye_rules, re.M)), 6)

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
