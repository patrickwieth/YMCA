import json
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

    def test_shipped_snapshot_and_native_fixtures_are_current(self):
        self.assertEqual(json.loads((REPO / 'mods/ca/modular/designer-catalog.json').read_text(encoding='utf-8')), data())
        self.assertEqual(json.loads(Path(__file__).with_name('designer-calculation-cases.json').read_text()), cases())
        self.assertEqual(len(cases()), 32)


if __name__ == '__main__':
    unittest.main()
