import unittest
from survey_loadouts import load, pack, unpack, source_hash, report, REPORT, summary, selected
from list_missing_vehicles import inventory


class LoadoutSurveyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = load()
        cls.rows = {r['actor']: r for r in cls.data['actors']}

    def test_snapshot_covers_every_ground_row_and_matches_current_sources(self):
        rows, _ = inventory()
        self.assertEqual(set(self.rows), {r['actor'] for r in rows})
        self.assertEqual(len(self.rows), 388)
        self.assertEqual(self.data['source_sha256'], source_hash())
        for row in self.rows.values():
            self.assertEqual(row['summary'], summary(row))
            for trait in row['traits']:
                self.assertNotIn('OpenRA.Support.BooleanExpression', trait['fields'].values())

    def test_shared_trait_deduplication_is_lossless_for_the_exported_fields(self):
        packed = pack(self.data)
        self.assertEqual(unpack(packed)['actors'], self.data['actors'])
        self.assertLess(len(packed['trait_definitions']), sum(len(r['traits']) for r in self.data['actors']))

    def test_battle_fortress_disability_and_bunker_protection_variants_are_not_hidden(self):
        self.assertEqual(self.rows['BATF']['production_status'], 'disabled')
        self.assertEqual(self.rows['BATF.AI']['production_status'], 'bot-gated')
        self.assertTrue(self.rows['BATF.Bunker.PDL']['summary']['cargo'])
        self.assertTrue(self.rows['BATF.Bunker.PDL']['summary']['pdl'])
        self.assertEqual(self.rows['BATF.Bunker.Reflector']['summary']['armor'], 'Reflector')
        self.assertEqual(self.rows['BATF.Prism']['summary']['turret_traits'], 0)
        self.assertIn('BattlePrisLaser', self.rows['BATF.Prism']['summary']['weapons'])

    def test_pdl_magazine_and_reload_and_reflector_target_class_are_resolved(self):
        row = self.rows['BATF.Bunker.PDL']
        ammo = next(t for t in selected(row, 'AmmoPool') if t['instance'] == 'pdl')
        self.assertEqual(ammo['fields']['Ammo'], '12')
        reload = next(t for t in selected(row, 'ReloadAmmoPool') if t['instance'] == 'pdl')
        self.assertEqual(reload['fields']['Delay'], '240')
        reflector = self.rows['BATF.Bunker.Reflector']
        self.assertTrue(any(t['fields'].get('TargetTypes') == 'reflector' for t in selected(reflector, 'Targetable')))

    def test_conditional_visual_weapons_are_not_counted_as_four_independent_disruptor_turrets(self):
        row = self.rows['DISR']
        self.assertEqual(row['summary']['turret_traits'], 1)
        self.assertEqual(row['summary']['armament_channels'], 4)
        self.assertEqual(set(row['summary']['weapons']), {'SonicZap','SonicZapVisual','SonicZap.UPG','SonicZapVisual.UPG'})
        artillery = self.rows['BATF.Artillery']
        self.assertFalse(artillery['summary']['cargo'])
        gun = next(t for t in selected(artillery, 'Armament') if t['fields']['Weapon'] == '155mm')
        self.assertEqual(gun['fields']['RequiresCondition'], 'cargo')

    def test_report_is_current_and_does_not_claim_new_native_bindings(self):
        self.assertEqual(REPORT.read_text(encoding='utf-8'), report(self.data))
        self.assertIn('keine neuen', report(self.data).lower())


if __name__ == '__main__':
    unittest.main()
