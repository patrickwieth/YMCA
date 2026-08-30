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

if [ "${FORCE_DEPLOY:-0}" != "1" ] \
    && docker top ymca-tournament-bot -eo args 2>/dev/null | grep -q 'OpenRA.Server.dll'; then
    echo "Deployment blocked: tournament match servers are running." >&2
    echo "Wait for the tournament to finish, or use FORCE_DEPLOY=1 for an intentional restart." >&2
    exit 1
fi

mkdir -p data/matches
chmod 700 data
chmod 600 .env

docker-compose build tournament-bot
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
