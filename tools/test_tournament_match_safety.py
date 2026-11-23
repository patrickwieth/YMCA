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

    def test_dotnet_seven_digits_are_rounded_up_not_released_early(self):
        now = datetime.datetime(2026, 11, 23, 12, 0, 0, 123456, tzinfo=datetime.timezone.utc)
        state = {'Matches': {'M1': {'Status': 'Completed', 'ServerRetainUntilUtc': '2026-11-23T12:00:00.1234567Z'}}}
        self.assertTrue(module.is_busy(state, now))
        self.assertFalse(module.is_busy(state, now + datetime.timedelta(microseconds=1)))
        self.assertEqual(module.parse_retention('2026-11-23T13:00:00.1234560+01:00'), now)
        self.assertEqual(module.parse_retention('2026-11-23T11:59:59.9999999Z'), now.replace(microsecond=0))
        for fraction in ('1', '12', '123', '1234', '12345', '123456', '1234567'):
            self.assertIsNotNone(module.parse_retention('2026-11-23T12:00:00.' + fraction + 'Z'))
        for invalid in ('2026-11-23T12:00:00', '2026-11-23T12:00:00.12345678Z', 'nonsense'):
            with self.assertRaises(ValueError): module.parse_retention(invalid)

    def test_bad_state_cannot_be_treated_as_idle(self):
        now = datetime.datetime.now(datetime.timezone.utc)
        for state in [{}, {'Matches': []}, {'Matches': {'M1': {'Status': 'unknown'}}},
                      {'Matches': {'M1': {'Status': 'Completed', 'ServerRetainUntilUtc': 'bad'}}}]:
            with self.assertRaises((ValueError, KeyError)):
                module.is_busy(state, now)


if __name__ == '__main__':
    unittest.main()
