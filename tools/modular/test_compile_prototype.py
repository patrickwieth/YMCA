import copy
import tempfile
import unittest
from pathlib import Path
import zipfile

from calibrate import calculate
from compile_prototype import build, compile_designs, load_inputs
from check_prototype import differences


class PrototypeTests(unittest.TestCase):
    def setUp(self):
        self.catalog, self.prototype = load_inputs()

    def test_emits_real_actors_and_derived_weapon_templates(self):
        rules, weapons, summary = compile_designs(self.catalog, self.prototype['designs'])
        self.assertEqual(len(summary), 3)
        self.assertEqual(rules.count('\n\tInherits: MTNK'), 3)
        self.assertEqual(weapons.count('\n\tInherits: 120mm'), 3)
        self.assertIn('Weapon: modular.stationary.gun', rules)
        self.assertIn('Range: 4c768', weapons)
        self.assertIn('Inherits@MODULARHOVER: ^HoverVehicle', rules)
        self.assertIn('ImmovableCondition: modular-stationary', rules)
        self.assertIn('PauseOnCondition: being-captured', rules)
        self.assertEqual(rules.count('\n\t-Buildable:'), 3)
        self.assertEqual(summary[0]['values']['cost'], 900)
        self.assertEqual(summary[1]['values']['cost'], 1100)
        self.assertEqual(summary[2]['values']['cost'], 850)

    def test_scalar_changes_reach_emitted_rules(self):
        self.catalog['components']['medium-tank-shell']['damage'] = 4321
        self.catalog['components']['medium-cannon-mount']['reload_ticks'] = 55
        self.catalog['components']['diesel']['mechanical_kw'] = 400
        rules, weapons, summary = compile_designs(self.catalog, self.prototype['designs'][:1])
        self.assertIn('Damage: 4321', weapons)
        self.assertIn('ReloadDelay: 55', weapons)
        self.assertIn('Speed: ' + str(summary[0]['values']['speed']), rules)
        self.assertLess(summary[0]['values']['speed'], 82)

    def test_stationary_zero_reserve_is_valid_but_negative_is_not(self):
        d = self.prototype['designs'][2]
        result = calculate(self.catalog, d)
        self.assertEqual((result['speed'], result['turn_speed']), (0, 0))
        self.assertEqual(result['locomotor'], 'wheeled')
        self.assertEqual(self.catalog['components']['gdi-stationary']['factions'], ['gdi'])
        self.catalog['components']['diesel']['mechanical_kw'] = 40
        self.assertEqual(calculate(self.catalog, d)['reserve_kw'], 0)
        with self.assertRaises(ValueError):
            calculate(self.catalog, self.prototype['designs'][0])
        self.catalog['components']['diesel']['mechanical_kw'] = 39
        with self.assertRaises(ValueError):
            calculate(self.catalog, d)

    def test_unsupported_capabilities_fail_closed(self):
        for field, value in (('crew', []), ('auxiliary_mount', {}), ('manufacturing', 'mass-production')):
            d = copy.deepcopy(self.prototype['designs'][0]); d[field] = value
            with self.assertRaisesRegex(ValueError, 'Unsupported design field'):
                compile_designs(self.catalog, [d])
        d = copy.deepcopy(self.prototype['designs'][0]); d['faction'] = 'nod'
        with self.assertRaises(ValueError):
            compile_designs(self.catalog, [d])
        d = copy.deepcopy(self.prototype['designs'][0]); d['components'].append('pdl')
        with self.assertRaises(ValueError):
            compile_designs(self.catalog, [d])
        self.catalog['components']['medium-cannon-mount']['cp'] = 1
        with self.assertRaisesRegex(ValueError, 'commander unlock'):
            compile_designs(self.catalog, self.prototype['designs'])

    def test_ids_and_behavior_bindings_are_guarded(self):
        d = copy.deepcopy(self.prototype['designs'][0])
        with self.assertRaises(ValueError):
            compile_designs(self.catalog, [d, d])
        d['actor'] = 'World:\nInjected'
        with self.assertRaises(ValueError):
            compile_designs(self.catalog, [d])
        self.catalog['components']['gdi-stationary']['locomotor'] = 'tracked'
        with self.assertRaisesRegex(ValueError, 'binding'):
            compile_designs(self.catalog, self.prototype['designs'])

    def test_lint_comparison_does_not_hide_new_errors(self):
        base = 'Error: missing palette for mtnk'
        self.assertEqual(differences(base, base + '\nError: missing palette for modular.tank')[2],
                         ['Error: missing palette for modular.tank'])
        self.assertEqual(differences(base, base + '\nError: new bad weapon')[3], ['Error: new bad weapon'])
        with self.assertRaises(ValueError):
            differences('', 'Failed with exception')

    def test_package_is_deterministic_and_preserves_existing_files(self):
        with tempfile.TemporaryDirectory() as temp:
            a, b = (Path(temp) / name for name in ('a.oramap', 'b.oramap'))
            manifest = build(a); build(b)
            self.assertEqual(a.read_bytes(), b.read_bytes())
            with self.assertRaises(FileExistsError):
                build(a)
            with zipfile.ZipFile(a) as z:
                self.assertIn('modular-rules.yaml', z.namelist())
                text = z.read('map.yaml').decode()
                self.assertIn('Rules: modular-rules.yaml', text)
                self.assertIn('Weapons: modular-weapons.yaml', text)
                self.assertIn('Transport: ocar', text)
                self.assertIn('Tileset: RUBBERDUCK-TEMPERATE', text)
            self.assertIn('mods/ca/rules/gdi/vehicles.yaml', manifest['source_sha256'])


if __name__ == '__main__':
    unittest.main()
