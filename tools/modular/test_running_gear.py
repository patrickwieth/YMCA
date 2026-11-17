import copy
import json
import re
import unittest
from pathlib import Path

from calibrate import calculate
from survey_vehicles import local_fields, survey

HERE = Path(__file__).resolve().parent


class RunningGearTests(unittest.TestCase):
    def setUp(self):
        self.catalog = json.loads((HERE / 'catalog.json').read_text())
        self.by_name = {d['name']: d for d in self.catalog['designs']}

    def test_all_previous_designs_preserve_scalar_results(self):
        previous = json.loads((HERE / 'running-gear-migration-baseline.json').read_text())
        self.assertEqual(previous['source_commit'], '06f6fa36')
        for name, expected in previous['designs'].items():
            with self.subTest(design=name):
                if 'error' in expected:
                    with self.assertRaisesRegex(ValueError, expected['error']):
                        calculate(self.catalog, self.by_name[name])
                    continue
                actual = calculate(self.catalog, self.by_name[name])
                for key, value in expected.items():
                    self.assertAlmostEqual(actual[key], value, msg=name + ': ' + key)

    def test_new_baselines_and_shared_components(self):
        previous = json.loads((HERE / 'running-gear-migration-baseline.json').read_text())['designs']
        new = [d for d in self.catalog['designs'] if d['name'] not in previous]
        self.assertEqual(len(new), 12)
        for d in new:
            r = calculate(self.catalog, d)
            for key, value in d['target'].items():
                self.assertEqual(r[key], value, (d['name'], key))
        for name in ('Nod Light Tank baseline', 'T-34 baseline', 'Devil Tank baseline'):
            self.assertIn('light-cannon', self.by_name[name]['components'])
            self.assertEqual(calculate(self.catalog, self.by_name[name])['locomotor'], 'tracked')
        for name in ('Hum-Vee baseline', 'Buggy baseline'):
            self.assertIn('scout-sensors', self.by_name[name]['components'])
            self.assertEqual(calculate(self.catalog, self.by_name[name])['locomotor'], 'wheeled')

    def test_new_reference_targets_match_source_fields(self):
        rows = {(r['faction'], r['actor']): r for r in survey(HERE.parents[1], self.catalog)}
        previous = json.loads((HERE / 'running-gear-migration-baseline.json').read_text())['designs']
        for d in self.catalog['designs']:
            if d['name'] in previous:
                continue
            actor = d['reference_actor']
            # Explicit, inspected aliases only; do not claim general MiniYaml resolution.
            if actor in ('Challenger_Tank', 'Leclerc_Tank', 'Leopard_Tank'):
                actor = '2TNK'
            source = rows[d['faction'], actor]
            for field in ('cost', 'hp', 'speed'):
                self.assertEqual(d['target'][field], int(source[field]), (d['name'], field))

    def test_gear_is_counted_once_and_defaults_are_overridable(self):
        d = copy.deepcopy(self.by_name['Battlemaster baseline'])
        implicit = calculate(self.catalog, d)
        d['running_gear'] = 'tracks-standard'
        self.assertEqual(calculate(self.catalog, d), implicit)
        self.catalog['components']['tracks-standard']['cost'] += 10
        self.catalog['components']['tracks-standard']['mass'] += 100
        r = calculate(self.catalog, d)
        self.assertEqual(r['cost'] - implicit['cost'], 10)
        self.assertEqual(r['mass'] - implicit['mass'], 100)

    def test_incompatible_missing_and_duplicate_gear_fail(self):
        d = copy.deepcopy(self.by_name['Battlemaster baseline'])
        for gear in ('wheels-light', 'walker-heavy', 'diesel', None, 'missing'):
            d['running_gear'] = gear
            with self.assertRaises(ValueError):
                calculate(self.catalog, d)
        d.pop('running_gear')
        d['components'].append('tracks-standard')
        with self.assertRaises(ValueError):
            calculate(self.catalog, d)

    def test_gear_limits_are_real_constraints(self):
        d = self.by_name['Battlemaster baseline']
        g = self.catalog['components']['tracks-standard']
        g['max_speed'] = 60
        g['turn_speed'] = 20
        r = calculate(self.catalog, d)
        self.assertEqual((r['speed'], r['turn_speed']), (60, 20))
        g['max_mass'] = 9999
        with self.assertRaisesRegex(ValueError, 'load exceeded'):
            calculate(self.catalog, d)
        for field in ('mass', 'cost', 'electric_kw', 'max_mass', 'max_speed', 'turn_speed'):
            c = copy.deepcopy(self.catalog)
            c['components']['tracks-standard'][field] = float('nan')
            with self.assertRaisesRegex(ValueError, 'running gear'):
                calculate(c, d)

    def test_walker_and_light_artillery_keep_actual_engine_profiles(self):
        jug = calculate(self.catalog, self.by_name['Juggernaut baseline'])
        self.assertEqual(jug['running_gear'], 'walker-heavy')
        self.assertEqual(jug['locomotor'], 'sheavytracked')
        arty = calculate(self.catalog, self.by_name['Allied Artillery baseline'])
        self.assertEqual((arty['locomotor'], arty['armor']), ('lighttracked', 'Light'))
        self.assertNotIn('infantry', self.catalog['locomotors']['lighttracked']['crushes'])

    def test_movement_profile_snapshots_match_source(self):
        text = (HERE.parents[1] / 'mods/ca/rules/world.yaml').read_text()
        for name, profile in self.catalog['locomotors'].items():
            block = re.search(r'\tLocomotor@' + name.upper() + r':\n(.*?)(?=\n\t[^\t ]|\Z)', text, re.S).group(1)
            terrain = {k: int(v) for k, v in re.findall(r'^\t{3}(\w+): (\d+)$', block, re.M)}
            crushes = re.search(r'^\t\tCrushes: (.+)$', block, re.M).group(1).split(', ')
            self.assertEqual(profile['terrain_speeds'], terrain)
            self.assertEqual(profile['crushes'], crushes)


class SurveyTests(unittest.TestCase):
    def test_local_fields_do_not_pretend_to_resolve_inheritance(self):
        fields, parents, traits = local_fields('A:\n\tInherits: B\n\tInherits: C\n\t-Buildable:\n\tMobile:\n\t\tSpeed: 90')
        self.assertEqual(parents, ['B', 'C'])
        self.assertEqual(fields, {'Mobile.Speed': '90'})
        self.assertIn('-Buildable', traits)

    def test_six_factions_and_honest_unresolved_values(self):
        catalog = json.loads((HERE / 'catalog.json').read_text())
        rows = survey(HERE.parents[1], catalog)
        self.assertEqual({r['faction'] for r in rows}, {'china', 'allies', 'soviet', 'gdi', 'nod', 'scrin'})
        self.assertGreater(len(rows), 100)
        challenger = next(r for r in rows if r['actor'] == 'Challenger_Tank')
        self.assertEqual(challenger['cost'], '')
        self.assertEqual(challenger['designs'], ['Challenger baseline'])
        parent = next(r for r in rows if r['actor'] == '2TNK')
        self.assertEqual(parent['buildable'], 'removed locally')
        hover = next(r for r in rows if r['actor'] == 'Mammoth.Hover')
        self.assertFalse(hover['designs'])
        self.assertIn('inspection required', hover['status'])


if __name__ == '__main__':
    unittest.main()
