import copy
import json
from pathlib import Path
import unittest

from calibrate import calculate, validate_roster, report


class CalibrationTests(unittest.TestCase):
    def setUp(self):
        self.catalog = json.loads(Path(__file__).with_name('catalog.json').read_text())
        self.design = copy.deepcopy(self.named('Battlemaster baseline'))

    def named(self, name):
        return next(d for d in self.catalog['designs'] if d['name'] == name)

    def replace(self, old, new):
        self.design['components'][self.design['components'].index(old)] = new

    def test_reference_baselines(self):
        for name in ('Battlemaster baseline', 'Battlemaster nuclear drive',
                     'Juggernaut baseline', 'Heavy Tesla Tank baseline', 'Battlemaster Autoloader'):
            design = self.named(name)
            result = calculate(self.catalog, design)
            for key, target in design['target'].items():
                self.assertEqual(result[key], target, (name, key))

    def test_nuclear_shell_family(self):
        for name in ('Battlemaster Nuclear Shells', 'Battlemaster Nuclear Shells PDL',
                     'Battlemaster Nuclear Shells Reflector'):
            d = self.named(name)
            r = calculate(self.catalog, d)
            for key, target in d['target'].items():
                self.assertEqual(r[key], target, (name, key))
            self.assertIn('nuclear-shell', d['components'])
            self.assertNotIn('nuclear', d['components'])  # Ammunition is not propulsion.

    def test_mass_production_residual(self):
        for name in ('Battlemaster Mass Production', 'Battlemaster Mass Production Reflector'):
            d = self.named(name)
            r = calculate(self.catalog, d)
            for key, target in d['target'].items():
                self.assertEqual(r[key], target, (name, key))
        d = self.named('Battlemaster Mass Production PDL')
        r = calculate(self.catalog, d)
        self.assertEqual((r['gross_cost'], r['discount'], r['cost']), (1850, 600, 1250))
        self.assertEqual(d['target']['cost'], 1000)
        for key, target in d['target'].items():
            if key != 'cost':
                self.assertEqual(r[key], target, key)
        self.assertIn('1000 -> 1250 (+25.0%)', report(self.catalog))

    def test_same_pdl_increment_cannot_fit_different_legacy_increments(self):
        for cp_value in (0, 200, 300):
            self.catalog['credits_per_cp'] = cp_value
            deltas = []
            for family in ('Battlemaster Autoloader', 'Battlemaster Nuclear Shells',
                           'Battlemaster Mass Production'):
                base = calculate(self.catalog, self.named(family))
                pdl = calculate(self.catalog, self.named(family + ' PDL'))
                deltas.append(pdl['cost'] - base['cost'])
            self.assertEqual(deltas, [950 - cp_value] * 3)

    def test_autoloader_pdl(self):
        d = self.named('Battlemaster Autoloader PDL')
        r = calculate(self.catalog, d)
        for key, target in d['target'].items():
            if key != 'speed':
                self.assertEqual(r[key], target, key)
        self.assertLessEqual(abs(r['speed'] / d['target']['speed'] - 1), 0.010001)
        self.assertEqual((r['catalog_points'], r['cp'], r['tech']), (5, 2, 3))

    def test_reflector_matches_with_shared_hardware(self):
        d = self.named('Battlemaster Autoloader Reflector')
        r = calculate(self.catalog, d)
        self.assertEqual((r['cost'], r['hp'], r['speed']), (950, 40000, 100))
        for key in ('armor', 'cp', 'tech', 'burst', 'burst_delay_ticks', 'reload_ticks', 'range_cells', 'damage'):
            self.assertEqual(r[key], d['target'][key])
        self.assertEqual((r['gross_cost'], r['discount']), (1550, 600))
        self.assertIn('1550 - 600 = 950', report(self.catalog))

    def test_emp_discount_matches_legacy_target(self):
        design = self.named('Juggernaut EMP / efficient')
        result = calculate(self.catalog, design)
        self.assertEqual((result['gross_cost'], result['discount'], result['cost']), (2300, 300, 2000))
        self.assertEqual((result['speed'], result['hp'], result['cp']), (50, 60000, 1))
        baseline = calculate(self.catalog, self.named('Juggernaut EMP / baseline'))
        self.assertEqual((baseline['cost'], baseline['speed']), (1800, 45))

    def test_global_cp_value_applies_to_every_design(self):
        for value in (0, 200, 300):
            self.catalog['credits_per_cp'] = value
            for design in self.catalog['designs']:
                if design['name'] == 'Battlemaster PDL + reflector / baseline':
                    with self.assertRaisesRegex(ValueError, 'Generator output exceeded'):
                        calculate(self.catalog, design)
                    continue
                result = calculate(self.catalog, design)
                self.assertEqual(result['discount'], result['cp'] * value)
                self.assertEqual(result['cost'], result['gross_cost'] - result['cp'] * value)

    def test_individual_discounts_rejected(self):
        self.catalog['components']['emp-shell']['design_credit_discount'] = 200
        with self.assertRaises(ValueError):
            calculate(self.catalog, self.named('Juggernaut EMP / efficient'))

    def test_invalid_discounts(self):
        for amount in (-1, float('nan'), float('inf'), 2300, 3000):
            with self.subTest(amount=amount):
                self.catalog['credits_per_cp'] = amount
                with self.assertRaises(ValueError):
                    calculate(self.catalog, self.named('Juggernaut EMP / efficient'))

    def test_report_separates_references_and_experiments(self):
        text = report(self.catalog)
        self.assertIn('2000 -> 2000 (+0.0%)', text)
        self.assertIn('2300 - 300 = 2000', text)
        references, experiments = text.split('## Experimental combinations')
        self.assertNotIn('Battlemaster PDL /', references)
        self.assertIn('Battlemaster PDL /', experiments)
        self.assertIn('Battlemaster Autoloader PDL', references)

    def test_power_conversion(self):
        self.assertEqual(calculate(self.catalog, self.design)['reserve_kw'], 460)
        self.design['components'].append('pdl')
        self.assertEqual(calculate(self.catalog, self.design)['reserve_kw'], 272)
        self.replace('baseline-generator', 'efficient-generator')
        self.replace('diesel', 'diesel-large')
        self.assertEqual(calculate(self.catalog, self.design)['reserve_kw'], 474)

    def test_armor_modifies_only_chassis(self):
        self.replace('heavy', 'reflector')
        result = calculate(self.catalog, self.design)
        # Exercise non-neutral percentages independently of the fitted defaults.
        self.catalog['components']['reflector'].update(mass_percent=120, hp_percent=90, cost_percent=120)
        result = calculate(self.catalog, self.design)
        self.assertEqual(result['mass'], 11600)
        self.assertEqual(result['hp'], 36000)
        self.assertEqual((result['gross_cost'], result['discount'], result['cost']), (1050, 300, 750))
        self.assertEqual(result['electric_kw'], 67.5)
        self.assertEqual(result['armor'], 'Reflector')

    def test_design_point_axes(self):
        self.design['components'].append('pdl')
        self.replace('heavy', 'reflector')
        self.replace('baseline-generator', 'efficient-generator')
        r = calculate(self.catalog, self.design)
        self.assertEqual((r['catalog_points'], r['cp'], r['tech']), (4, 2, 3))
        self.assertEqual(validate_roster(self.catalog, [self.design, self.design]), 8)
        with self.assertRaises(ValueError):
            validate_roster(self.catalog, [self.design, self.design], level=7)

    def test_invalid_selections(self):
        for old, new in [('cannon-turret', 'fixed-triple'), ('shell', 'emp-shell')]:
            with self.subTest(new=new):
                design = copy.deepcopy(self.design)
                design['components'][design['components'].index(old)] = new
                with self.assertRaises(ValueError):
                    calculate(self.catalog, design)
        self.design['components'].append('pdl')
        self.catalog['components']['battlemaster']['equipment_slots'] = 0
        with self.assertRaises(ValueError):
            calculate(self.catalog, self.design)

    def test_limits(self):
        for component, field, value in [('baseline-generator', 'efficiency', 0),
                                         ('baseline-generator', 'max_electric_kw', 5),
                                         ('diesel', 'mechanical_kw', 40),
                                         ('battlemaster', 'max_mass', 9000),
                                         ('battlemaster', 'tier', 0)]:
            with self.subTest(field=field):
                catalog = copy.deepcopy(self.catalog)
                catalog['components'][component][field] = value
                with self.assertRaises(ValueError):
                    calculate(catalog, self.design)

    def test_duplicate_and_missing_roles(self):
        for ids in [self.design['components'] + ['shell'], self.design['components'][1:]]:
            with self.assertRaises(ValueError):
                calculate(self.catalog, dict(self.design, components=ids))


if __name__ == '__main__':
    unittest.main()
