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
        source = ROOT / 'mods/ca/development-maps/testmap1.oramap'
        original = source.read_bytes()
        before = actors(source)
        expected = {10: 'weap.nod', 23: 'fact.soviet', 24: 'powr.soviet', 25: 'apwr.soviet',
                    27: 'sentry.soviet', 30: 'weap.soviet', 31: 'dome.soviet', 32: 'hpad.soviet', 35: 'fix.soviet', 37: 'flak.soviet.sprite'}
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
            normalized = text.replace('\r\n', '\n')
            player = re.search(r'(?m)^\tPlayerReference@Multi0:\n(?:\t\t[^\n]*\n)*', normalized)[0]
            self.assertIn('\t\tFaction: england\n', player)
            self.assertIn('\t\tLockFaction: True\n', player)
            launcher = (ROOT / 'tools/test-game.cmd').read_text()
            self.assertIn('PlayerFaction.Multi0=england', launcher)
            self.assertNotIn('PlayerFaction.Multi0=blackh', launcher)
            for number in (3,4,5,6,8,10,11,12,13,14,15,16,17,18,19,20,21,22):
                body = re.search(r'^\tActor' + str(number) + r':[^\n]*\n((?:\t\t[^\n]*\n)*)', normalized, re.M)[1]
                self.assertIn('\t\tFaction: blackh\n', body, f'Nod display Actor{number}')
                self.assertIn('\t\tOwner: Multi0\n', body)
            self.assertIn('\tTestTeslaReactor: tpwr\n\t\tOwner: Multi0\n\t\tLocation: 122,-58\n\t\tFaction: russia\n', normalized)
            self.assertEqual(len(re.findall(r'(?m)^\t[^:]+: tpwr$', normalized)), 1)
            self.assertNotRegex(text, r'(?m)^\tFTUR:')
            self.assertNotIn('Actor9', after)
            self.assertNotIn('sentry.soviet.voxel', text)
            self.assertEqual(text.count(': aa.practice'), 5)
            self.assertIn('\tSovietSamDisplay: sam\n\t\tOwner: Multi0\n\t\tLocation: 154,-62', text.replace('\r\n', '\n'))
            self.assertIn('AirTargetSovietSamDisplay: aa.practice', text)
            self.assertIn('Inherits: TRAN', text)
            self.assertIn('IdleBehavior: None', text)
            self.assertIn('TakeOffOnCreation: true', text)
            for slot in (14, 37, 59, 80):
                self.assertIn(f'AirTargetActor{slot}: aa.practice\n\t\tOwner: Multi0', text.replace('\r\n', '\n'))
            self.assertEqual(after['Actor71'], 'weap.td')
            self.assertEqual(location(37), (148, -56))
            self.assertIn('\tFLAK.SOVIET.SPRITE:\n\t\tArmament:\n\t\t\tWeapon: SOVIET-CLUSTER-AA.Test', text.replace('\r\n', '\n'))
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
