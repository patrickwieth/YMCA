import json
from pathlib import Path
import unittest

from export_designer_catalog import data, cases, REPO


class DesignerCatalogTests(unittest.TestCase):
    def test_shipped_snapshot_and_native_fixtures_are_current(self):
        self.assertEqual(json.loads((REPO / 'mods/ca/modular/designer-catalog.json').read_text(encoding='utf-8')), data())
        self.assertEqual(json.loads(Path(__file__).with_name('designer-calculation-cases.json').read_text()), cases())
        self.assertEqual(len(cases()), 24)


if __name__ == '__main__':
    unittest.main()
