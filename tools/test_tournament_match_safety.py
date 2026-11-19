import datetime
import importlib.util
from pathlib import Path
import unittest

path = Path(__file__).resolve().parents[1] / 'TournamentBot/deploy/check-match-safety.py'
spec = importlib.util.spec_from_file_location('match_safety', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class MatchSafetyTests(unittest.TestCase):
    def test_active_and_statistics_hosts_block_deployment(self):
        now = datetime.datetime.now(datetime.timezone.utc)
        for status in ['Queued', 'StartingServer', 'WaitingForPlayers', 'Playing']:
            self.assertTrue(module.is_busy({'Matches': {'M1': {'Status': status}}}, now))
        for status in ['Completed', 'AwaitingConfirmation', 'Disputed', 'RematchRequested']:
            match = {'Status': status, 'ServerRetainUntilUtc': (now + datetime.timedelta(minutes=3)).isoformat()}
            self.assertTrue(module.is_busy({'Matches': {'M1': match}}, now))
            match['ServerRetainUntilUtc'] = (now - datetime.timedelta(seconds=1)).isoformat()
            self.assertFalse(module.is_busy({'Matches': {'M1': match}}, now))
            match['ServerRetainUntilUtc'] = None
            self.assertFalse(module.is_busy({'Matches': {'M1': match}}, now))

    def test_bad_state_cannot_be_treated_as_idle(self):
        now = datetime.datetime.now(datetime.timezone.utc)
        for state in [{}, {'Matches': []}, {'Matches': {'M1': {'Status': 'unknown'}}},
                      {'Matches': {'M1': {'Status': 'Completed', 'ServerRetainUntilUtc': 'bad'}}}]:
            with self.assertRaises((ValueError, KeyError)):
                module.is_busy(state, now)


if __name__ == '__main__':
    unittest.main()
