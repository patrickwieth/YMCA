"""Recurring menu-scene Kirovs: fourteen-second spacing, unchanged initial formation."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


class LibertySpawnTimingTests(unittest.TestCase):
    def test_recurring_interval(self):
        script = (ROOT / 'mods/ca/maps/liberty-stream-shellmap/liberty.lua').read_text()
        self.assertEqual(re.findall(r'Trigger\.AfterDelay\(DateTime\.Seconds\((\d+)\), SpawnNext\)', script), ['14'])
        self.assertNotIn('spawnDelayRemainder', script)
        self.assertIn('if #raiders < 48 then SendBomber(Cell(-25, 96)) end', script)
        self.assertIn('for i = 1, 18 do', script)
        self.assertIn('local t = i / 18', script)


if __name__ == '__main__':
    unittest.main()
