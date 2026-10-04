import unittest
from export_designer_catalog import data, cases
from turret_modules import MODULES, recipes, canonical
from report_turret_modules import report, REPORT


class TurretModuleTests(unittest.TestCase):
    def test_four_source_qualified_modules_are_native_but_not_arbitrary(self):
        catalog = data()
        for id, m in MODULES.items():
            part = catalog['components'][id]; recipe = catalog['turret_modules'][id]
            self.assertEqual(part['slots_required'], 1)
            self.assertEqual(part['display_name'], m['label'])
            self.assertEqual(recipe['source_actor'], m['actor'])
            self.assertFalse(recipe['arbitrary_mounting'])
            assembly = next(a for a in catalog['assemblies'] if a['options']['chassis'] == [m['hull']])
            self.assertEqual(assembly['options']['carrier'], [id])
        fortress = next(a for a in catalog['assemblies'] if a['options']['chassis'] == ['stock-batf-hull'])
        self.assertEqual(fortress['options']['carrier'], ['superheavy-bunker'])

    def test_prices_and_numeric_baselines_are_unchanged(self):
        expected = {'prism-turret': (150, 500, 1350, 22000, 82), 'artillery-turret': (50, 500, 550, 15000, 68),
                    'missile-turret': (50, 500, 1200, 20000, 135), 'sonic-turret': (200, 1000, 1500, 75000, 56)}
        catalog = data(); fixtures = cases()
        for id, (cost, mass, price, hp, speed) in expected.items():
            self.assertEqual(catalog['components'][id]['cost'], cost)
            self.assertEqual(catalog['components'][id]['mass'], mass)
            self.assertEqual(catalog['components'][id]['electric_kw'], 0)
            row = next(c for c in fixtures if c['parts']['carrier'] == id and c['parts']['generator'] == 'baseline-generator')
            self.assertEqual((row['values']['cost'], row['values']['hp'], row['values']['speed']), (price, hp, speed))
        self.assertEqual(len(fixtures), 168)

    def test_weapon_profiles_and_render_models_remain_distinct(self):
        r = recipes()
        weapons = lambda id: {t['fields']['Weapon'] for t in r[id]['armaments']}
        self.assertEqual(weapons('prism-turret'), {'PrisTLaser'})
        self.assertEqual(weapons('artillery-turret'), {'155mmTDM'})
        self.assertEqual(weapons('missile-turret'), {'StnkMissile', 'StnkMissile.AA'})
        self.assertEqual(weapons('sonic-turret'), {'SonicZap', 'SonicZapVisual', 'SonicZap.UPG', 'SonicZapVisual.UPG'})
        self.assertTrue(any(t['type'] == 'WithVoxelTurretInfo' for t in r['prism-turret']['graphics']))
        self.assertTrue(any(t['type'] == 'WithSpriteTurretInfo' for t in r['sonic-turret']['graphics']))
        self.assertEqual({t['fields']['RequiresCondition'] for t in r['sonic-turret']['armaments']}, {'gdiupg2','!gdiupg2'})

    def test_carrier_migration_is_specific_not_a_global_integrated_mount_replacement(self):
        self.assertEqual(canonical('nod-combat-howitzer-hull', 'integrated-mount'), 'artillery-turret')
        self.assertEqual(canonical('nod-combat-stealth-hull', 'integrated-mount'), 'missile-turret')
        self.assertEqual(canonical('nod-combat-apc-hull', 'integrated-mount'), 'integrated-mount')
        a = data(); a['turret_modules']['sonic-turret']['source_actor'] = 'bad'
        self.assertEqual(data()['turret_modules']['sonic-turret']['source_actor'], 'DISR')

    def test_report_is_current(self):
        self.assertEqual(REPORT.read_text(encoding='utf-8'), report())


if __name__ == '__main__':
    unittest.main()
