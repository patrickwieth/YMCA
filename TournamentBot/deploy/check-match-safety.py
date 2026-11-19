#!/usr/bin/env python3
"""Exit zero only when no tournament host needs to be preserved."""
import datetime
import json
import pathlib
import sys


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
        if retain and datetime.datetime.fromisoformat(retain.replace('Z', '+00:00')).astimezone(datetime.timezone.utc) > now:
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
