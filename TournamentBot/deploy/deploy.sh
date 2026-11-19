#!/bin/sh
set -eu

cd "$(dirname "$0")"

if [ ! -f .env ]; then
    echo "Missing TournamentBot/deploy/.env. Copy .env.example and set the Discord token." >&2
    exit 1
fi

if grep -q 'replace-with-the-discord-bot-token' .env; then
    echo "The Discord token placeholder is still present in .env." >&2
    exit 1
fi

check_matches() {
if [ "${FORCE_DEPLOY:-0}" != "1" ] \
    && ! python3 ./check-match-safety.py data/tournament-state.json; then
    echo "Deployment blocked: a match is active, players are viewing post-game statistics, or state could not be verified." >&2
    echo "Wait for the matches to finish, or use FORCE_DEPLOY=1 for an intentional restart." >&2
    exit 1
fi
}

check_matches
mkdir -p data/matches
chmod 700 data
chmod 600 .env

docker-compose build tournament-bot
# A match may have started while the image was building. Never skip this recheck.
check_matches
docker-compose up -d tournament-bot

printf 'Waiting for the join page'
i=0
until curl -sS -o /dev/null http://127.0.0.1:15080/join/health-check; do
    i=$((i + 1))
    if [ "$i" -ge 60 ]; then
        echo
        echo "Tournament bot did not become reachable. Recent logs:" >&2
        docker-compose logs --tail=100 tournament-bot >&2
        exit 1
    fi

    printf '.'
    sleep 2
done

echo
docker-compose ps
echo "Tournament bot is reachable locally on http://127.0.0.1:15080/."

# HTTP alone is insufficient: also require a successful Discord login and command registration.
started=$(docker inspect --format '{{.State.StartedAt}}' ymca-tournament-bot)
i=0
until docker logs --since "$started" ymca-tournament-bot 2>&1 | grep -q 'Registered tournament commands in '; do
    i=$((i + 1))
    if [ "$i" -ge 60 ] || [ "$(docker inspect --format '{{.State.Running}}' ymca-tournament-bot)" != true ]; then
        echo "Tournament bot has not confirmed Discord command registration; deployment is not verified." >&2
        exit 1
    fi
    sleep 2
done
echo "Discord connection and tournament command registration verified."
expected_version=$(awk '$1 == "YMCA_VERSION:" { print $2; exit }' docker-compose.yml)
actual_version=$(docker exec ymca-tournament-bot awk '$1 == "Version:" { print $2; exit }' /app/game/mods/ca/mod.yaml | tr -d '\r')
if [ -z "$expected_version" ] || [ "$actual_version" != "$expected_version" ]; then
    echo "Tournament host version mismatch: expected $expected_version, got $actual_version." >&2
    exit 1
fi
echo "Tournament host version verified: $actual_version"
