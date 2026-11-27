"""Regression assertions over reproducible native missile-height captures.
Live rule guard: OpenRA.Utility ca --audit-missile-height check.
"""
import json
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art-source/missile-height-policy'

def load(name):
    return json.loads((ART / name).read_text())

def shots(name):
    return {(s['Weapon'], s['LaunchAltitude'], s['TargetAltitude']): s for s in load(name)['Shots']}

class MissileHeightPolicyTests(unittest.TestCase):
    def test_resolved_protection_without_changing_existing_fields(self):
        before, after = load('resolved-before.json'), load('resolved-after.json')
        self.assertEqual(len(before), len(after))
        for old, new in zip(before, after):
            self.assertEqual(old, {k: new[k] for k in old}, old['Weapon'])
            if new['BoundTo'] != 'Water':
                self.assertTrue(new['Aware'] or new['Relative'], new['Weapon'])
            else:
                self.assertFalse(new['Aware'])
                self.assertFalse(new['Relative'])

    def test_flat_ground_is_identical(self):
        self.assertEqual(shots('before-0.json'), shots('final-0.json'))

    def test_reproduced_ground_launcher_examples_are_fixed(self):
        before, after = shots('before-4.json'), shots('final-4.json')
        for key in [('hq7rockets', 200, 0), ('ifvrocketsaae', 200, 2560),
                    ('atomizerboltsaa', 200, 2560), ('bikerockethail', 200, 0)]:
            self.assertGreater(before[key]['MissDistance'], 2048)
            self.assertLess(before[key]['Altitude'], -362)
            self.assertLess(after[key]['MissDistance'], 1024)

    def test_no_early_self_plateau_collision_at_any_tested_height(self):
        flat = shots('final-0.json')
        for height in (4, 8, 12):
            elevated = shots(f'final-{height}.json')
            self.assertEqual(set(flat), set(elevated))
            for key, shot in elevated.items():
                self.assertTrue(shot['Finished'], key)
                self.assertFalse(shot['MissDistance'] > 2048 and shot['Altitude'] < -362, key)
                if flat[key]['MissDistance'] < 1024:
                    self.assertLess(shot['MissDistance'], 2048, key)

    def test_water_and_explicit_engine_lookahead(self):
        water = load('water.json')['Shots']
        self.assertEqual(len(water), 4)
        for shot in water:
            self.assertTrue(shot['Finished'])
            self.assertEqual(shot['Altitude'], 0)
            self.assertLess(shot['MissDistance'], 512)
        for shot in load('engine-lookahead.json')['Shots']:
            if shot['Weapon'].startswith('atomizerbolts'):
                self.assertTrue(shot['Finished'])
                self.assertLess(shot['MissDistance'], 1024)
        self.assertTrue(load('mlrs-aa-regression.json')['Passed'])

    def test_engine_patch_registered(self):
        self.assertIn('tools/patches/missile-physical-height.patch', (ROOT / 'tools/engine-patches.txt').read_text())
        self.assertIn('CruiseRelativeToTerrain', (ROOT / 'tools/patches/missile-physical-height.patch').read_text())

if __name__ == '__main__':
    unittest.main()
