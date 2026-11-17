import unittest
from export_designer_catalog import data, cases
from nod_combat_expansion import FAMILIES
from list_missing_vehicles import inventory
from shared_running_gear import ALIASES
from turret_modules import canonical


class SharedRunningGearTests(unittest.TestCase):
    def test_classes_are_distinct_from_locomotors_and_old_duplicates_are_removed(self):
        parts = data()['components']
        self.assertEqual(sum(p['role'] == 'running_gear' for p in parts.values()), 16)
        for alias in ALIASES: self.assertNotIn(alias, parts)
        for key, locomotor in [('light-tracks', 'wheeled'), ('tracks-light-artillery', 'lighttracked')]:
            self.assertEqual(parts[key]['compatibility_class'], 'light-tracks')
            self.assertEqual(parts[key]['locomotor'], locomotor)
        for key in ('designer-mlrs-hull', 'designer-ssm-hull', 'designer-prism-hull'):
            self.assertEqual(parts[key]['allowed_running_gear'], ['light-tracks'])
            self.assertEqual(parts[key]['allowed_running_gear_classes'], ['light-tracks'])

    def test_all_hulls_have_explicit_class_admission_and_bound_options(self):
        snapshot = data(); parts = snapshot['components']
        for assembly in snapshot['assemblies']:
            hull = parts[assembly['options']['chassis'][0]]
            self.assertEqual(hull['allowed_running_gear_classes'], sorted({parts[g]['compatibility_class'] for g in assembly['options']['running_gear']}))

    def test_nod_baseline_values_and_shared_hardware(self):
        fixtures = cases(); parts = data()['components']
        for key, actor, label, gear, motor, armor, price, hp, speed, turn, tech, weapon_cost in FAMILIES:
            rows = [c for c in fixtures if c['parts']['chassis'] == f'nod-combat-{key}-hull']
            self.assertEqual(len(rows), 2)
            base = next(c for c in rows if c['parts']['generator'] == 'baseline-generator')
            for field, expected in [('cost', price), ('hp', hp), ('speed', speed), ('turn_speed', turn)]:
                self.assertEqual(base['values'][field], expected, (actor, field))
            self.assertEqual(base['parts']['carrier'], canonical(f'nod-combat-{key}-hull', 'integrated-mount'))
            self.assertEqual(base['parts']['ammunition'], 'integral-stores')
            self.assertGreaterEqual(parts[f'nod-combat-{key}-hull']['cost'], 0)

    def test_china_ground_combat_base_family_gap_is_closed_not_promoted_variants(self):
        _, families = inventory()
        self.assertFalse([f for f in families if f['faction'] == 'china' and f['role'] == 'combat' and f['status'] == 'production candidate' and not f['native']])

    def test_nod_ground_combat_base_family_gap_is_closed_not_promoted_variants(self):
        _, families = inventory()
        missing = [f for f in families if f['faction'] == 'nod' and f['role'] == 'combat' and f['status'] == 'production candidate' and not f['native']]
        self.assertEqual(missing, [])


if __name__ == '__main__':
    unittest.main()
