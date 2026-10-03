import unittest
from collections import Counter
from calibrate import calculate
from compile_prototype import load_inputs
from export_designer_catalog import data, cases
from stock_combat_expansion import ROWS, key, prerequisites
from generate_stock_combat_bindings import render, TARGET
from list_missing_vehicles import inventory, native_actors


class StockCombatExpansionTests(unittest.TestCase):
    def test_explicit_generated_native_bindings_are_current(self):
        self.assertEqual(TARGET.read_text(encoding='utf-8'), render())
        self.assertEqual(len(ROWS), 44)
        self.assertEqual(len(native_actors()), 89)

    def test_all_production_ground_combat_base_families_have_native_bindings(self):
        _, families = inventory()
        self.assertFalse([f for f in families if f['role'] == 'combat' and f['status'] == 'production candidate' and not f['native']])
        # Base BATF is disabled: do not silently remove that restriction to inflate coverage.
        self.assertNotIn('batf', native_actors())
        self.assertIn('batf.bunker', native_actors())

    def test_every_new_baseline_matches_engine_exported_values(self):
        fixtures = cases()
        for r in ROWS:
            fixture = next(c for c in fixtures if c['parts']['chassis'] == key(r)+'-hull')
            for source, result in [('cost','cost'), ('hp','hp'), ('speed','speed'), ('turn','turn_speed')]:
                self.assertEqual(fixture['values'][result], r[source], (r['actor'], source))
            self.assertNotIn('~disabled', prerequisites(r))
            for p in r['prerequisites']:
                if not p.startswith('~') or p.startswith(('~promotion.', '~!promotion.', '~!upg.')):
                    self.assertIn(p, prerequisites(r))

    def test_shared_hardware_prices_are_added_once_and_stock_arsenal_is_chassis_bound(self):
        d = data(); parts = d['components']
        self.assertEqual(sum(p['role'] == 'running_gear' for p in parts.values()), 16)
        for r in ROWS:
            a = next(a for a in d['assemblies'] if a['options']['chassis'] == [key(r)+'-hull'])
            self.assertEqual(a['options']['weapon'], ['original-armament'])
            self.assertEqual(sum(parts[v[0]].get('cost', 0) for v in a['options'].values()), r['cost'])
            self.assertTrue(all(len(v) == 1 for v in a['options'].values()), 'Fixed stock assemblies are not arbitrary mix-and-match weapons.')
            self.assertEqual(parts[key(r)+'-hull']['factions'], [r['faction']])

    def test_bunker_is_a_separate_superheavy_module_not_a_chassis_name(self):
        parts = data()['components']
        self.assertEqual(parts['stock-batf-hull']['display_name'], 'Battle Fortress')
        self.assertEqual(parts['stock-batf-hull']['chassis_class'], 'superheavy')
        self.assertEqual(parts['superheavy-bunker']['compatible_chassis_classes'], ['superheavy'])
        self.assertEqual(parts['superheavy-bunker']['display_name'], 'Bunker Module')
        self.assertEqual(parts['superheavy-bunker']['cost'], 1000)
        self.assertEqual(parts['superheavy-bunker']['slots_required'], 3)
        self.assertEqual(parts['stock-batf-hull']['carrier_slots'], 3)
        self.assertEqual(parts['stock-batf-hull']['allowed']['carrier'], ['superheavy-bunker'])

    def test_bunker_uses_all_three_slots_and_blocks_even_an_approved_auxiliary_mount(self):
        catalog, _ = load_inputs()
        snapshot = data(); catalog['components'].update(snapshot['components'])
        choices = next(a['options'] for a in snapshot['assemblies'] if a['options']['chassis'] == ['stock-batf-hull'])
        design = dict(faction='allies', running_gear=choices['running_gear'][0],
            components=[v[0] for role, v in choices.items() if role != 'running_gear'])
        self.assertEqual(calculate(catalog, design)['cost'], 3000)
        hull = catalog['components']['stock-batf-hull']
        hull['carrier_slots'] = 2
        with self.assertRaisesRegex(ValueError, 'slot'): calculate(catalog, design)
        hull['carrier_slots'] = 3
        hull['auxiliary_mount'] = dict(carriers=['integrated-mount'], slot='roof')
        design['auxiliary_mount'] = dict(carrier='integrated-mount', weapon='original-armament', ammunition='integral-stores')
        with self.assertRaisesRegex(ValueError, 'slot'): calculate(catalog, design)

    def test_catalog_is_larger_than_any_one_saved_roster(self):
        counts = Counter(a.get('faction', 'gdi') for a in data()['assemblies'])
        self.assertEqual(counts, dict(gdi=15, nod=14, china=8, allies=13, soviet=25, scrin=14))
        self.assertEqual(len(cases()), 168)


if __name__ == '__main__':
    unittest.main()
