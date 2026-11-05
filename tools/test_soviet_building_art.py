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
        'Cloning_Vats': 3, 'NKOREA_BUNKER': 3, 'FTUR': 2, 'TTUR': 2, 'TSLA': 2,
        'MSLO': 3, 'SPEN': 4, 'IRON': 3, 'SAM': 2, 'AFLD': 3, 'STEK': 3,
        'BARR': 3, 'INDP': 3, 'HPAD.Soviet': 3, 'FACT.Nukular.Soviet': 4}


def traits(text):
    return dict(re.findall(r'^([^\s:]+):([^\n]*(?:\n[\t ][^\n]*|\n(?=\n))*)', text, re.M))


def gameplay_hash(text):
    values = {key: re.sub(r'\s+', '', value) for key, value in traits(text).items()
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
        baseline = json.loads((ROOT / 'tools/soviet-building-gameplay-baseline.json').read_text())
        for actor, size in BIBS.items():
            with self.subTest(actor=actor):
                text = resolved('--resolved-rules', actor)
                fields = traits(text)
                self.assertIn(f'Image: sovietbib{size}x{size}', fields['WithIdleOverlay@SOVIETBIB'])
                self.assertIn('IsDecoration: true', fields['WithIdleOverlay@SOVIETBIB'])
                self.assertNotIn('WithIdleOverlay@ALLIEDBIB', fields)
                # Disabled legacy bibs still load sequences in AddedToWorld.
                self.assertNotIn('WithBuildingBib', fields)
                if actor in baseline:
                    self.assertEqual(gameplay_hash(text), baseline[actor])
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

    def test_missile_animation_preserved(self):
        sequence = resolved('--resolved-sequences', 'mslo.soviet')
        fields = traits(sequence)
        for state in ('active', 'damaged-active'):
            self.assertIn('Count: 7', fields[state])
            self.assertIn('Length: 7', fields[state])
        for state in ('idle', 'damaged-idle', 'active', 'damaged-active', 'open', 'damaged-open'):
            self.assertIn('Anchor: Bottom', fields[state])
            self.assertIn('Offset: 0,-43', fields[state])


if __name__ == '__main__':
    unittest.main()
