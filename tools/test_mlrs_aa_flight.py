"""Checks recorded native regression evidence and the narrow AA-only rule change.
Regenerate evidence with prepare_mlrs_aa_probe.py and YMCA_MLRS_AA_PROBE=1.
"""
import json
import unittest
from pathlib import Path
from convert_handcrafted_layouts import miniyaml
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art-source/mlrs-aa-flight'

class MlrsAaFlightTests(unittest.TestCase):
    def test_reproduction_and_fixed_native_flights(self):
        before = json.loads((ART / 'before.json').read_text())
        after = json.loads((ART / 'after.json').read_text())
        self.assertFalse(before['Passed'])
        self.assertTrue(after['Passed'])
        self.assertEqual(len(after['Flights']), 8)
        broken = next(s for s in before['Flights'] if s['Weapon'] == '227mmaa.dedicated' and s['Ground'] > 0)
        self.assertGreater(broken['MissDistance'], 8000)
        self.assertLess(broken['Trace'][-1]['AboveTerrain'], 0)
        for shot in after['Flights']:
            self.assertTrue(shot['Finished'])
            self.assertLess(shot['MissDistance'], 1024)
            self.assertTrue(all(p['AboveTerrain'] >= 0 for p in shot['Trace']))
        for old, new in zip(before['Flights'], after['Flights']):
            if old['Ground'] == 0:
                self.assertEqual(old, new, 'Flat-ground trajectory must stay unchanged')

    def test_real_mlrs_hits_stationary_and_moving_aircraft(self):
        before = json.loads((ART / 'before.json').read_text())['Combat']
        after = json.loads((ART / 'after.json').read_text())['Combat']
        self.assertEqual([False, True], [s['Moving'] for s in after])
        self.assertTrue(all(s['Damage'] > 0 for s in after))
        self.assertGreater(after[1]['Damage'], before[1]['Damage'])
        self.assertEqual(after[0]['Damage'], before[0]['Damage'])

    def test_rule_scope_and_no_range_damage_buff(self):
        rules = miniyaml((ROOT / 'mods/ca/rules/gdi/weapons.yaml').read_text(encoding='utf-8-sig'))
        aa = rules.child('227mmAA')
        dedicated = rules.child('227mmAA.dedicated')
        self.assertEqual(aa.child('Projectile').child('TerrainHeightAware').value, 'true')
        self.assertNotIn('TerrainHeightAware', [n.key for n in rules.child('227mm').child('Projectile').children])
        self.assertEqual(aa.child('Range').value, '10c512')
        self.assertEqual(aa.child('Warhead@1Dam').child('Damage').value, '3000')
        self.assertEqual(dedicated.child('Range').value, '12c512')
        self.assertEqual(dedicated.child('Projectile').child('RangeLimit').value, '16c0')

if __name__ == '__main__':
    unittest.main()
