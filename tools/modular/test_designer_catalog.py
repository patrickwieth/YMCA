import json
import re
from pathlib import Path
import unittest

from export_designer_catalog import data, cases, REPO
from check_prototype import actor_sources, differences


class DesignerCatalogTests(unittest.TestCase):
    def test_designer_skirmish_guard_replaces_stock_restoration(self):
        manifest = (REPO / 'mods/ca/mod.yaml').read_text(encoding='utf-8')
        server_traits = manifest.split('ServerTraits:\n', 1)[1].split('\nChromeMetrics:', 1)[0]
        self.assertIn('\tCustomFactionSkirmishLogic\n', server_traits)
        self.assertNotIn('\tSkirmishLogic\n', server_traits)

    def test_roster_lint_matches_each_parent_not_everything_to_tank(self):
        sources = actor_sources('modular.custom.a:\n\tInherits: HMMV\nmodular.custom.b:\n\tInherits: MLRS\n')
        baseline = 'Error: palette for hmmv\nError: condition for mlrs'
        extra = baseline + '\nError: palette for modular.custom.a\nError: condition for modular.custom.b'
        self.assertEqual(len(differences(baseline, extra, sources)[2]), 2)
        self.assertFalse(differences(baseline, extra, sources)[3])
        self.assertEqual(differences(baseline, extra + '\nError: palette for modular.custom.a.gun', sources)[3],
                         ['Error: palette for modular.custom.a.gun'])
        with self.assertRaises(ValueError):
            actor_sources('modular.custom.a:\n\tInherits: UnsupportedActor\n')

    def test_expanded_gdi_defaults_match_current_stock_scalar_fields(self):
        text = (REPO / 'mods/ca/rules/gdi/vehicles.yaml').read_text(encoding='utf-8')
        targets = {'juggernaut': 'Juggernaut', 'designer-mammoth-hull': 'Mammoth',
                   'designer-hmlrs-hull': 'hmlrs', 'designer-disruptor-hull': 'DISR', 'designer-mk2-hull': 'MAMMOTHMK2'}
        for hull, actor in targets.items():
            row = next(r for r in cases() if r['parts']['chassis'] == hull and r['parts']['generator'] == 'baseline-generator')
            block = re.search(r'(?ms)^' + re.escape(actor) + r':\n(.*?)(?=^\S|\Z)', text).group(1)
            for field, key in [('Cost', 'cost'), ('HP', 'hp'), ('Speed', 'speed')]:
                value = int(re.search(r'(?m)^\t\t' + field + r': (\d+)$', block).group(1))
                self.assertEqual(row['values'][key], value, (actor, field))

    def test_nod_defaults_match_stock_including_explicit_artillery_parent(self):
        nod = (REPO / 'mods/ca/rules/nod/vehicles.yaml').read_text(encoding='utf-8')
        allied = (REPO / 'mods/ca/rules/allies/vehicles.yaml').read_text(encoding='utf-8')
        targets = {'nod-light-hull': ('LTNK', nod), 'buggy-hull': ('BGGY', nod),
                   'designer-nod-artillery-hull': ('ARTY', allied), 'designer-ssm-hull': ('SSM', nod)}
        for hull, (actor, source) in targets.items():
            row = next(r for r in cases() if r['parts']['chassis'] == hull and r['parts']['generator'] == 'baseline-generator')
            self.assertEqual(row['base_faction'], 'blackh')
            block = re.search(r'(?ms)^' + re.escape(actor) + r':\n(.*?)(?=^\S|\Z)', source).group(1)
            for field, key in [('Cost', 'cost'), ('HP', 'hp'), ('Speed', 'speed')]:
                self.assertEqual(row['values'][key], int(re.search(r'(?m)^\t\t' + field + r': (\d+)$', block).group(1)))

    def test_native_weapon_parent_names_use_exact_yaml_declaration_case(self):
        native = (REPO / 'OpenRA.Mods.CA/Modular/CustomVehicleAssembly.cs').read_text(encoding='utf-8')
        parents = re.findall(r'\{ "Armament(?:@[A-Za-z0-9_-]+)?", "([^"]+)" \}', native)
        paths = list((REPO / 'mods/ca/weapons').glob('*.yaml')) + list((REPO / 'mods/ca/rules').glob('*/weapons.yaml'))
        declarations = set()
        for path in paths:
            declarations.update(re.findall(r'(?m)^([^\s:]+):', path.read_text(encoding='utf-8-sig')))
        self.assertGreater(len(parents), 20)
        for parent in parents:
            self.assertIn(parent, declarations, 'MiniYaml Inherits is case-sensitive, unlike an Armament weapon reference')

    def test_shipped_snapshot_and_native_fixtures_are_current(self):
        self.assertEqual(json.loads((REPO / 'mods/ca/modular/designer-catalog.json').read_text(encoding='utf-8')), data())
        self.assertEqual(json.loads(Path(__file__).with_name('designer-calculation-cases.json').read_text()), cases())
        self.assertEqual(len(cases()), 50)


if __name__ == '__main__':
    unittest.main()
