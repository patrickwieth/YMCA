import unittest
from pathlib import Path
from collections import Counter
from list_missing_vehicles import REPO, inventory, native_actors, report


class MissingVehicleTests(unittest.TestCase):
    def test_actual_native_bindings_not_spreadsheet_coverage(self):
        self.assertEqual(len(native_actors()), 31)
        _, families = inventory()
        missing = [f for f in families if not f['native'] and f['status'] == 'production candidate']
        self.assertEqual(Counter(f['role'] for f in missing), {'combat': 58, 'support': 19})
        self.assertNotIn('MARV', [f['family'] for f in missing])
        self.assertIn('VULC', [f['family'] for f in missing])
        self.assertNotIn('DEVO', [f['family'] for f in missing])
        self.assertNotIn('T-34', [f['family'] for f in missing])

    def test_mixed_roles_and_special_cases_are_not_hidden(self):
        text = report()
        for token in ('Soviet_Miner', 'choutpost', 'TRUK.Test', 'SFTNK', 'CDRN',
                      'Challenger_Tank', 'Leclerc_Tank', 'OCAR.Eagle', 'LST', 'PAC.support'):
            self.assertIn(token, text)
        self.assertNotIn('helix.reconupgrade', text)

    def test_committed_report_is_current(self):
        self.assertEqual((REPO / 'docs/modular/missing-vehicles.md').read_text(encoding='utf-8'), report())


if __name__ == '__main__':
    unittest.main()
