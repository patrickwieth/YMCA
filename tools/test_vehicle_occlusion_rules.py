"""Resolve real production rules: construction vehicles must not rely on probe overrides."""
import os
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[1]
ENGINE = ROOT / 'engine'

class VehicleOcclusionRules(unittest.TestCase):
    def test_construction_and_combat_vehicles_have_one_occlusion_trait(self):
        env = dict(os.environ, ENGINE_DIR=str(ENGINE), MOD_SEARCH_PATHS=f'{ROOT / "mods"},{ENGINE / "mods"}')
        for actor in ['CHNMCV', 'CHNMCV.Nukular', 'AMCV', 'SMCV', 'MTNK']:
            with self.subTest(actor=actor):
                text = subprocess.check_output(['dotnet', str(ENGINE / 'bin/OpenRA.Utility.dll'), 'ca', '--resolved-rules', actor], env=env, text=True, encoding='utf-8')
                self.assertEqual(len(re.findall(r'^WithRockOcclusion:', text, re.M)), 1)

if __name__ == '__main__':
    unittest.main()
