#!/usr/bin/env python3
"""Exit zero only when no tournament host needs to be preserved."""
import datetime
import json
import pathlib
import re
import sys


def parse_retention(value):
    # Python 3.9 accepts 3/6 fractional digits, .NET emits seven. Round UP
    # sub-microsecond precision so normalization cannot release a host early.
    match = re.fullmatch(r'(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d{1,7}))?(Z|[+-]\d{2}:\d{2})', value)
    if not match:
        raise ValueError('Invalid timezone-aware retention timestamp')
    base, fraction, zone = match.groups()
    fraction = fraction or ''
    normalized = base + '.' + fraction[:6].ljust(6, '0') + zone.replace('Z', '+00:00')
    parsed = datetime.datetime.fromisoformat(normalized)
    if any(c != '0' for c in fraction[6:]):
        parsed += datetime.timedelta(microseconds=1)
    return parsed.astimezone(datetime.timezone.utc)


def is_busy(state, now):
    matches = state['Matches']
    if not isinstance(matches, dict):
        raise ValueError('Invalid Matches collection')
    for match in matches.values():
        if match.get('Status') not in ('Queued', 'StartingServer', 'WaitingForPlayers', 'Playing', 'AwaitingConfirmation', 'Completed', 'RematchRequested', 'Disputed', 'Failed', 'Cancelled'):
            raise ValueError('Unknown match status')
        if match.get('Status') in ('Queued', 'StartingServer', 'WaitingForPlayers', 'Playing'):
            return True
        retain = match.get('ServerRetainUntilUtc')
        if retain and parse_retention(retain) > now:
            return True
    return False


if __name__ == '__main__':
    try:
        path = pathlib.Path(sys.argv[1])
        busy = path.exists() and is_busy(json.loads(path.read_text()), datetime.datetime.now(datetime.timezone.utc))
        sys.exit(1 if busy else 0)
    except Exception as error:
        print('Cannot verify tournament safety: ' + str(error), file=sys.stderr)
        sys.exit(1)
