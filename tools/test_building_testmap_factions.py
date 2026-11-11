import hashlib
import re
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def actors(path):
    with zipfile.ZipFile(path) as archive:
        text = archive.read('map.yaml').decode('utf-8-sig')
    return dict(re.findall(r'^\t(Actor\d+): ([^\r\n]+)', text, re.M))


class BuildingTestMapFactionTests(unittest.TestCase):
    def test_soviet_slots_only_and_immutable_source(self):
        source = ROOT / 'mods/ca/maps/testmap1.oramap'
        original = source.read_bytes()
        before = actors(source)
        expected = {23: 'fact.soviet', 24: 'powr.soviet', 25: 'apwr.soviet',
                    27: 'sentry.soviet', 30: 'weap.soviet', 31: 'dome.soviet', 32: 'hpad.soviet', 35: 'fix.soviet'}
        with tempfile.TemporaryDirectory() as temp:
            destination = Path(temp) / 'test.oramap'
            subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass',
                            '-File', str(ROOT / 'tools/prepare-test-game-map.ps1'),
                            '-SourceMap', str(source), '-DestinationMap', str(destination)], check=True)
            after = actors(destination)
            with zipfile.ZipFile(destination) as archive:
                text = archive.read('map.yaml').decode('utf-8-sig')
            def location(number):
                body = re.search(r'^\tActor' + str(number) + r':[^\n]*\n((?:\t\t[^\n]*\n)*)', text, re.M)[1]
                self.assertIn('Owner: Multi0', body)
                return tuple(map(int, re.search(r'Location: (-?\d+),(-?\d+)', body).groups()))
            self.assertNotRegex(text, r'(?m)^\tFTUR:')
            tx, ty = location(27)
            hx, hy = location(32)
            self.assertEqual((tx, ty), (140, -64))
            self.assertEqual(tx-ty, hx-hy)  # same isometric screen column
            self.assertEqual((hx+hy)-(tx+ty), 8)  # clear screen-up separation
            for number, actor in expected.items():
                self.assertEqual(after[f'Actor{number}'], actor)
            for number in (33, 34, 45, 46, 47, 51, 52, 53, 55, 56):
                self.assertEqual(after[f'Actor{number}'], before[f'Actor{number}'])
        self.assertEqual(hashlib.sha256(source.read_bytes()).digest(), hashlib.sha256(original).digest())


if __name__ == '__main__':
    unittest.main()
