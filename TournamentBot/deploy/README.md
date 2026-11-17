# Debian Docker deployment

This deployment runs the Discord bot and all OpenRA dedicated-server child processes inside one container. Eight host ports (`12400`–`12407`) map one-to-one to the container. Persistent state, match logs, and replays are stored under `deploy/data`.

## Initial installation

```sh
cd ~/src
git clone --branch tournament-bot https://github.com/patrickwieth/YMCA.git YMCA-tournament
cd YMCA-tournament/TournamentBot/deploy
cp .env.example .env
chmod 600 .env
nano .env
./deploy.sh
```

Put the Discord token only in `.env`. This file and `data/` are ignored by Git. The image downloads and compiles the exact OpenRA revision from `mod.config` for Linux, then builds YMCA and the bot.

Follow logs with:

```sh
docker-compose logs -f tournament-bot
```

Update and redeploy with:

```sh
cd ~/src/YMCA-tournament
git pull --ff-only
./TournamentBot/deploy/deploy.sh
```

## Firewall

Allow inbound TCP ports `12400`–`12407`. The join page is deliberately published only on host loopback port `15080`; public access goes through the existing nginx container.

## HTTPS and existing nginx container

The existing `herd` nginx container must:

1. mount `nginx-tournament.conf` into `/etc/nginx/conf.d/tournament.conf`;
2. mount `/etc/letsencrypt` read-only at the same path;
3. mount `/var/www/certbot` at the same path;
4. remain attached to the external `cardchain_default` network.

The tournament bot joins this network as `ymca-tournament-bot`, which is the upstream name used by the nginx configuration.

Issue the certificate using the webroot after the HTTP server block has been loaded:

```sh
sudo mkdir -p /var/www/certbot
sudo certbot certonly --webroot -w /var/www/certbot \
  -d tournament.crowdcontrol.network
```

Test and reload nginx:

```sh
docker exec herd nginx -t
docker exec herd nginx -s reload
```

Certificate renewal should reload nginx after success. For example, a root cron/systemd renewal hook can execute `docker exec herd nginx -s reload`.

## Operations

```sh
# Status
docker-compose ps

# Logs
docker-compose logs --tail=200 tournament-bot

# Graceful stop (SIGINT, allowing child servers to be terminated)
docker-compose stop tournament-bot

# Rebuild after an update
./deploy.sh
```

Do not run multiple orchestrator containers against the same `data` directory or port range.
