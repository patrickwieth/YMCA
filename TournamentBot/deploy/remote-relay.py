#!/usr/bin/python3
"""Live compatibility bridge for the legacy bot's *future* dedicated processes.

Install as /usr/local/bin/dotnet only after preflight. Non-server dotnet calls
exec the real binary unchanged. Existing processes are never touched. The old
public game port remains a byte-for-byte TCP relay; simulation runs remotely.
"""
import json
import hashlib
import os
import pathlib
import re
import select
import socket
import socketserver
import sys
import threading
import time
import urllib.error
import urllib.request
import uuid

REAL_DOTNET = '/usr/bin/dotnet'
CONFIG = '/app/remote-relay/config.json'


def atomic_json(path, value):
    path = pathlib.Path(path)
    tmp = path.with_name(path.name + '.relay-tmp')
    tmp.write_text(json.dumps(value))
    os.replace(tmp, path)


def server_arguments(args):
    if not args or not args[0].endswith('/OpenRA.Server.dll'):
        return None
    values = dict(arg.split('=', 1) for arg in args[1:] if '=' in arg)
    name = values.get('Server.Name', '')
    match = re.fullmatch(r'YMCA Tournament (M[0-9]{4,12})', name)
    return (match[1], values) if match else None


class FatalError(Exception):
    pass


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise FatalError('Remote API redirect rejected')


class Api:
    def __init__(self, config):
        self.url = config['BaseUrl'].rstrip('/')
        if not self.url.startswith('https://'):
            raise FatalError('Game-host API must use HTTPS')
        self.opener = urllib.request.build_opener(NoRedirect())
        self.token = pathlib.Path(config['TokenFile']).read_text().strip()
        if len(self.token) < 32:
            raise FatalError('Game-host token is missing')

    def request(self, method, path, body=None, download=None):
        req = urllib.request.Request(self.url + path,
            data=None if body is None else body if isinstance(body, bytes) else json.dumps(body).encode(), method=method,
            headers={'Authorization': 'Bearer ' + self.token, 'Content-Type': 'application/json'})
        try:
            with self.opener.open(req, timeout=20) as response:
                if download:
                    temporary = pathlib.Path(str(download) + '.downloading')
                    total = 0
                    try:
                        with temporary.open('wb') as output:
                            while True:
                                chunk = response.read(65536)
                                if not chunk:
                                    break
                                total += len(chunk)
                                if total > 512 * 1024 * 1024:
                                    raise FatalError('Remote replay exceeds size limit')
                                output.write(chunk)
                        os.replace(temporary, download)
                    finally:
                        temporary.unlink(missing_ok=True)
                    return None
                data = response.read(256 * 1024 + 1)
                if len(data) > 256 * 1024:
                    raise FatalError('Remote status exceeds size limit')
                return json.loads(data) if data else None
        except urllib.error.HTTPError as error:
            if error.code in (400, 401, 403, 409):
                raise FatalError('Remote host rejected request (HTTP %d); not falling back locally' % error.code) from None
            raise


class Forwarder(socketserver.ThreadingTCPServer):
    allow_reuse_address = True
    daemon_threads = True

    def __init__(self, port, destination):
        self.destination = destination
        super().__init__(('0.0.0.0', port), ForwardConnection)

    def handle_error(self, request, client_address):
        # Socket closures are normal; do not dump addresses or command arguments.
        pass


class ForwardConnection(socketserver.BaseRequestHandler):
    def handle(self):
        with socket.create_connection(self.server.destination, timeout=10) as upstream:
            upstream.settimeout(30)
            self.request.settimeout(30)
            sockets = (self.request, upstream)
            while True:
                readable, _, _ = select.select(sockets, [], [], 1)
                for source in readable:
                    block = source.recv(65536)
                    if not block:
                        return
                    (upstream if source is self.request else self.request).sendall(block)


def run_match(match_id, args, config):
    support = pathlib.Path(args['Engine.SupportDir']).resolve()
    root = pathlib.Path(config['MatchDirectory']).resolve()
    if support != root / match_id:
        raise FatalError('Unexpected match support directory')
    state_path = pathlib.Path(config['StateFile'])
    state = json.loads(state_path.read_text())
    match = state['Matches'][match_id]
    if match['Status'] not in ('Queued', 'StartingServer', 'WaitingForPlayers', 'Playing'):
        raise FatalError('Match is not active')
    tournament = state.get('Tournaments', {}).get(match.get('TournamentId'), {})
    if tournament.get('Status') in ('Completed', 'Cancelled'):
        raise FatalError('Parent tournament is closed')
    if match['MapUid'] != args['Server.Map']:
        raise FatalError('Persisted map and launch map disagree')
    marker = support / 'remote-relay.json'
    if marker.exists():
        identity = json.loads(marker.read_text())
        if identity['HostId'] != config['HostId'] or identity['PublicHost'] != config['PublicHost']:
            raise FatalError('Assigned remote host changed')
    else:
        identity = {'RequestId': uuid.uuid4().hex, 'HostId': config['HostId'], 'PublicHost': config['PublicHost']}
        atomic_json(marker, identity)
    request = dict(match)
    request.update(RemoteRequestId=identity['RequestId'], Password=args['Server.Password'],
                   SpectatorPassword=args.get('Server.SpectatorPassword', ''))
    api = Api(config)
    created = False
    forwarder = None
    replay_saved = False
    armed_timer = None
    last_warning = 0
    try:
        while True:
            try:
                if not created:
                    health = api.request('GET', '/v1/health')
                    if health['Protocol'] != 1 or health['GameVersion'] != config['ExpectedGameVersion'] or health['PublicHost'] != config['PublicHost']:
                        raise FatalError('Remote host identity/version mismatch')
                    if request.get('Generation') and request.get('GeneratedMapSha256'):
                        artifact = pathlib.Path(request['GeneratedMapPath']).resolve()
                        if root / 'generated-maps' not in artifact.parents or artifact.stat().st_size > 32 * 1024 * 1024:
                            raise FatalError('Invalid original map artifact path or size')
                        package = artifact.read_bytes()
                        if hashlib.sha256(package).hexdigest() != request['GeneratedMapSha256']:
                            raise FatalError('Original map checksum mismatch')
                        api.request('PUT', '/v1/maps/' + request['Generation']['Id'] + '/' + request['GeneratedMapSha256'], package)
                    api.request('POST', '/v1/matches', request)
                    created = True
                snapshot = api.request('GET', '/v1/matches/' + match_id)
                remote = snapshot['Match']
                if remote['RemoteRequestId'] != identity['RequestId'] or remote['Id'] != match_id:
                    raise FatalError('Remote request identity mismatch')
                if remote['Status'] == 'Failed':
                    raise FatalError('Remote dedicated server failed; inspect agent state')
                if remote['Status'] in ('WaitingForPlayers', 'Playing', 'AwaitingConfirmation') and forwarder is None:
                    for field in ('MapUid', 'GeneratedMapSha256', 'Password', 'SpectatorPassword'):
                        if remote[field] != request[field]:
                            raise FatalError('Remote launch mismatch in ' + field)
                    if remote['HostingPublicHost'] != config['PublicHost']:
                        raise FatalError('Unexpected remote public host')
                    identity['RemotePort'] = remote['Port']
                    atomic_json(marker, identity)
                    if not snapshot['Stopped']:
                        forwarder = Forwarder(int(args['Server.ListenPort']), (config['PublicHost'], remote['Port']))
                        threading.Thread(target=forwarder.serve_forever, daemon=True).start()
                    logs = support / 'Logs'
                    logs.mkdir(exist_ok=True)
                    (logs / 'dedicated-server.log').write_text('Initial map: ' + remote['MapUid'] + '\n')
                    print('Remote tournament game ready: ' + match_id, flush=True)
                lobby = snapshot.get('LobbyJson')
                if lobby:
                    atomic_json(support / 'lobby-status.json', json.loads(lobby))
                # The live legacy controller owns timers and results, not this adapter.
                latest = json.loads(state_path.read_text())['Matches'][match_id]
                timer = latest.get('AttendanceTimer')
                if timer and timer.get('EligibleForForfeit') and timer.get('Outcome') == 'Pending' and timer['Id'] != armed_timer and remote['Status'] == 'WaitingForPlayers':
                    try:
                        api.request('PUT', '/v1/matches/' + match_id + '/attendance', timer)
                        armed_timer = timer['Id']
                    except FatalError:
                        # The game may have started since the preceding status response.
                        if api.request('GET', '/v1/matches/' + match_id)['Match']['Status'] == 'WaitingForPlayers':
                            raise
                if snapshot.get('AttendanceDecision'):
                    atomic_json(support / 'tournament-attendance-decision.json', snapshot['AttendanceDecision'])
                if snapshot.get('Result') and not replay_saved:
                    api.request('GET', '/v1/matches/' + match_id + '/replay', download=support / 'remote.orarep')
                    replay_saved = True
                if snapshot['Stopped']:
                    if not replay_saved and not snapshot.get('AttendanceDecision'):
                        raise FatalError('Remote host stopped without a result')
                    return
            except (urllib.error.URLError, TimeoutError, ConnectionError) as error:
                if time.monotonic() - last_warning > 30:
                    print('Remote host temporarily unavailable for ' + match_id + '; retrying, no local fallback.', file=sys.stderr, flush=True)
                    last_warning = time.monotonic()
            time.sleep(1)
    finally:
        if forwarder:
            forwarder.shutdown()
            forwarder.server_close()
        # Deliberately no remote DELETE/stop: controller interruption must not kill games.


def main():
    parsed = server_arguments(sys.argv[1:])
    if parsed is None:
        os.execv(REAL_DOTNET, [REAL_DOTNET] + sys.argv[1:])
    config = json.loads(pathlib.Path(CONFIG).read_text())
    if not config.get('Enabled', False):
        os.execv(REAL_DOTNET, [REAL_DOTNET] + sys.argv[1:])
    try:
        run_match(parsed[0], parsed[1], config)
    except Exception as error:
        # Never print arguments/passwords or server response bodies.
        print('Remote relay failed: ' + (str(error) if isinstance(error, FatalError) else type(error).__name__), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
