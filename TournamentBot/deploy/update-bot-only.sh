#!/bin/sh
# Manual, guarded bot-only update. Never rebuild or replace the installed game.
set -eu
umask 077
revision=$1
repo=$2
printf '%s\n' "$revision" | grep -Eq '^[0-9a-f]{40}$'
cd "$repo"
test "$(git rev-parse "$revision^{commit}")" = "$revision"
deploy="$repo/TournamentBot/deploy"
stage="$deploy/data/recovery/bot-only-$(date -u +%Y%m%dT%H%M%SZ)-$(printf '%s' "$revision" | cut -c1-12)"
mkdir -p "$stage/code" "$stage/image/bot"
chmod 700 "$stage"
git archive "$revision" TournamentBot OpenRA.Mods.CA/MapGeneration/TournamentAttendanceProtocol.cs OpenRA.Mods.CA/MapGeneration/TournamentMapDownloadInfo.cs | tar -x -C "$stage/code"
base=$(docker inspect --format '{{.Image}}' ymca-tournament-bot)
image="ymca-tournament-bot:routing-$(printf '%s' "$revision" | cut -c1-12)"
docker run --rm --cpus=1 --memory=1536m -v "$stage/code:/src" -v "$stage/image/bot:/out" -w /src mcr.microsoft.com/dotnet/sdk:6.0-jammy dotnet publish TournamentBot/TournamentBot.csproj -c Release -o /out --no-self-contained /p:UseAppHost=false > "$stage/build.log" 2>&1
printf 'FROM %s\nCOPY --chown=1000:1000 bot/ /app/bot/\n' "$base" > "$stage/image/Dockerfile"
docker build -t "$image" "$stage/image" > "$stage/image-build.log" 2>&1
cd "$deploy"
safe() {
    python3 "$stage/code/TournamentBot/deploy/check-match-safety.py" data/tournament-state.json && python3 - <<'PY'
import json,pathlib,subprocess,sys
s=json.loads(pathlib.Path('data/tournament-state.json').read_text())
# This rollout intentionally waits for completed tournaments, not an idle gap
# between rounds. A new round must not start during the container replacement.
if any(t['Status']=='Running' for t in s['Tournaments'].values()):
    print('BLOCKED: running tournament');sys.exit(1)
p=subprocess.check_output(['docker','top','ymca-tournament-bot','-eo','pid,args'],text=True)
if 'OpenRA.Server.dll' in p:
    print('BLOCKED: game server process');sys.exit(1)
PY
}
safe
# No configuration edits, remote-host changes or state restoration on this path.
cp tournament-bot.json "$stage/config-before.json"
cp data/tournament-state.json "$stage/state-before.json"
cp check-match-safety.py "$stage/check-match-safety-before.py"
test -f docker-compose.override.yml
cp docker-compose.override.yml "$stage/compose-before.yml"
printf '%s\n' "$base" > "$stage/image-before.txt"
export STAGE="$stage"
# Require the known image-only pin rather than discarding arbitrary overrides.
python3 - <<'PY'
import pathlib,re
text=pathlib.Path('docker-compose.override.yml').read_text()
assert re.fullmatch(r'\s*version: [\x27\x22]3\.7[\x27\x22]\s+services:\s+tournament-bot:\s+image: [a-zA-Z0-9:._/-]+\s*',text), 'Unexpected compose override: inspect before deploying'
PY
switched=0
rollback() {
    result=$?
    trap - EXIT
    if [ "$result" -ne 0 ] && [ "$switched" = 1 ]; then
        running=$(docker inspect --format '{{.State.Running}}' ymca-tournament-bot 2>/dev/null || printf false)
        if [ "$running" = false ] || safe; then
            echo 'Verification failed; restoring prior image pin, NOT overwriting tournament state.'
            cp "$stage/compose-before.yml" docker-compose.override.yml
            docker-compose up -d --no-build tournament-bot || true
        else
            echo 'ROLLBACK_BLOCKED: newly active work; preserving running container for inspection.'
        fi
    fi
    exit "$result"
}
trap rollback EXIT
test "$(docker inspect --format '{{.Image}}' ymca-tournament-bot)" = "$base"
safe
switched=1
docker-compose stop tournament-bot
cp data/tournament-state.json "$stage/state-quiescent.json"
printf 'version: "3.7"\nservices:\n  tournament-bot:\n    image: %s\n' "$image" > docker-compose.override.yml
docker-compose up -d --no-build tournament-bot
started=$(docker inspect --format '{{.State.StartedAt}}' ymca-tournament-bot)
i=0
until docker logs --since "$started" ymca-tournament-bot 2>&1 | grep -q 'Registered tournament commands in '; do
    i=$((i+1)); test "$i" -lt 90; sleep 2
done
# Container recreation can change the address cached by host nginx.
docker exec herd nginx -t && docker exec herd nginx -s reload
curl --fail --silent --output /dev/null http://127.0.0.1:15080/tournaments
curl --fail --silent --output /dev/null https://tournament.crowdcontrol.network/tournament/T013
expected=$(sha256sum "$stage/image/bot/TournamentBot.dll" | cut -d' ' -f1)
actual=$(docker exec ymca-tournament-bot sha256sum /app/bot/TournamentBot.dll | cut -d' ' -f1)
test "$expected" = "$actual"
cmp tournament-bot.json "$stage/config-before.json"
python3 - <<'PY'
import json,os,pathlib
stage=pathlib.Path(os.environ['STAGE'])
before=json.loads((stage/'state-quiescent.json').read_text())
after=json.loads(pathlib.Path('data/tournament-state.json').read_text())
def normalized(value):
    if isinstance(value,list):return [normalized(v) for v in value]
    if isinstance(value,dict):return {k:normalized(v) for k,v in value.items() if not ((k=='BracketRoutingVersion' and v==0) or (k=='BracketSlotId' and v==''))}
    return value
for collection in ('Tournaments','Series','Matches'):
    assert normalized(before[collection])==normalized(after[collection]), 'Historical data changed: '+collection
print('HISTORY_VERIFIED: tournaments, series and matches unchanged')
PY
cp "$stage/code/TournamentBot/deploy/check-match-safety.py" check-match-safety.py
printf '%s\n' "$revision" > data/bot-only-deployed-revision
switched=0
printf 'DEPLOYMENT_VERIFIED %s\nPRIVATE_BACKUP %s\n' "$revision" "$stage"
