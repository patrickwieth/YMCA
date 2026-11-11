"""Resolved-rule regression tests for the independent Soviet buildings."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[1]
BIBS = {'FACT.Soviet': 4, 'POWR.Soviet': 3, 'APWR.Soviet': 4, 'TPWR': 4,
        'DOME.Soviet': 3, 'WEAP.Soviet': 4, 'FIX.Soviet': 4, 'Soviet_Refinery': 3,
        'Cloning_Vats': 3, 'NKOREA_BUNKER': 3, 'FTUR': 2, 'TTUR': 2, 'TSLA': 1,
        'MSLO': 3, 'SPEN': 4, 'IRON': 3, 'SAM': 2, 'AFLD': 3, 'STEK': 3,
        'BARR': 3, 'INDP': 3, 'HPAD.Soviet': 3, 'FACT.Nukular.Soviet': 4}


def traits(text):
    return dict(re.findall(r'^([^\s:]+):([^\n]*(?:\n[\t ][^\n]*|\n(?=\n))*)', text, re.M))


def gameplay_hash(text):
    fields = traits(text)
    if 'IsometricSelectable' in fields:
        fields['IsometricSelectable'] = re.sub(r'(?m)^[\t ]+(?:DecorationBounds|DecorationHeight):[^\n]*\n?', '', fields['IsometricSelectable'])
    values = {key: re.sub(r'\s+', '', value) for key, value in fields.items()
              if not key.startswith(('RenderSprites', 'WithIdleOverlay', 'WithBuildingBib'))}
    return hashlib.sha256(json.dumps(values, sort_keys=True).encode()).hexdigest()


def resolved(command, name):
    env = dict(os.environ, ENGINE_DIR=str(ROOT / 'engine'),
               MOD_SEARCH_PATHS=f'{ROOT / "mods"},{ROOT / "engine/mods"}')
    return subprocess.check_output(['dotnet', str(ROOT / 'engine/bin/OpenRA.Utility.dll'),
                                    str(ROOT / 'mods/ca'), command, name], env=env,
                                   cwd=ROOT, text=True)


class SovietBuildingArtTests(unittest.TestCase):
    def test_resolved_bibs_and_unchanged_gameplay(self):
        baseline = json.loads((ROOT / 'tools/soviet-building-alignment-gameplay-baseline.json').read_text())
        calibration = {e['Actor']: e for e in json.loads((ROOT / 'tools/soviet-building-alignment.json').read_text())['entries']}
        calibration['fact.nukular.soviet'] = calibration['fact.soviet']
        for actor, size in BIBS.items():
            with self.subTest(actor=actor):
                text = resolved('--resolved-rules', actor)
                fields = traits(text)
                self.assertIn(f'Image: sovietbib{size}x{size}', fields['WithIdleOverlay@SOVIETBIB'])
                self.assertIn('IsDecoration: true', fields['WithIdleOverlay@SOVIETBIB'])
                self.assertNotIn('WithIdleOverlay@ALLIEDBIB', fields)
                # Disabled legacy bibs still load sequences in AddedToWorld.
                self.assertNotIn('WithBuildingBib', fields)
                self.assertEqual(gameplay_hash(text), baseline[actor])
                if actor.lower() in calibration:
                    entry = calibration[actor.lower()]
                    self.assertIn('DecorationBounds: ' + ', '.join(map(str, entry['DecorationBounds'])), fields['IsometricSelectable'])
                    self.assertIn('DecorationHeight: ' + str(entry['DecorationHeight']), fields['IsometricSelectable'])
                if actor == 'AFLD':
                    self.assertIn('Image: afld.soviet', fields['RenderSprites'])
                if actor == 'HPAD.Soviet':
                    self.assertIn('Image: hpad.soviet', fields['RenderSprites'])

    def test_seven_bases_do_not_inherit_allied_actors(self):
        source = (ROOT / 'mods/ca/rules/soviet/building-bases.yaml').read_text()
        self.assertEqual(len(re.findall(r'^\^Soviet\w+Base:', source, re.M)), 7)
        for parent in re.findall(r'^\tInherits[^:]*: (.+)$', source, re.M):
            self.assertTrue(parent.startswith('^'), parent)
            self.assertNotIn('Allied', parent)
        actors = (ROOT / 'mods/ca/rules/soviet/structures.yaml').read_text()
        for actor in ('FACT', 'POWR', 'APWR', 'WEAP', 'FIX', 'DOME', 'HPAD'):
            block = actors.split('\n' + actor + '.Soviet:\n')[1].split('\n\n')[0]
            self.assertRegex(block, r'Inherits: \^Soviet\w+Base')
            self.assertNotRegex(block, r'Inherits: ' + actor + r'\s')

    def test_sentry_rotation_uses_existing_sheet_metadata(self):
        from PIL import Image
        path = ROOT / 'mods/ca/bits/soviet/buildings/rotational/sentry turret.png'
        metadata = path.with_suffix('.yaml').read_text()
        self.assertIn('FrameSize: 106,106', metadata)
        self.assertIn('FrameAmount: 9', metadata)
        with Image.open(path) as image:
            self.assertGreaterEqual(image.width, 3 * 106)
            self.assertGreaterEqual(image.height, 3 * 106)
        for name in ('ftur.soviet', 'ttur.soviet'):
            fields = traits(resolved('--resolved-sequences', name))
            for state in ('turret', 'damaged-turret'):
                self.assertIn('Filename: bits/soviet/buildings/rotational/sentry turret.png', fields[state])
                self.assertIn('Frames: 7, 8, 5, 2, 1, 0, 3, 6', fields[state])
                self.assertIn('Facings: 8', fields[state])

    def test_missile_animation_preserved(self):
        sequence = resolved('--resolved-sequences', 'mslo.soviet')
        fields = traits(sequence)
        for state in ('active', 'damaged-active'):
            self.assertIn('Count: 7', fields[state])
            self.assertIn('Length: 7', fields[state])
        calibration = json.loads((ROOT / 'tools/soviet-building-alignment.json').read_text())['entries']
        offset = ','.join(map(str, next(e for e in calibration if e['Actor'] == 'mslo')['Offset']))
        for state in ('idle', 'damaged-idle', 'active', 'damaged-active', 'open', 'damaged-open'):
            self.assertIn('Anchor: Bottom', fields[state])
            self.assertIn('Offset: ' + offset, fields[state])


if __name__ == '__main__':
    unittest.main()
