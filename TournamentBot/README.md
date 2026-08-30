# YMCA Tournament Bot

Discord tournament orchestration for YMCA/OpenRA. The bot registers players, queues matches, starts a dedicated OpenRA server per active match, sends private join details, imports the server replay result, and asks both players to confirm, request a rematch, or dispute the result.

## Commands

- `/register openra-name` — associate a Discord account with an exact OpenRA player name.
- `/match player-one player-two map-uid [map-title]` — admin: queue a match.
- `/matches` — show recent matches.
- `/resolve match-id winner` — admin: resolve a disputed match.
- `/map-add map`, `/map-remove map`, `/map-pool` — choose official YMCA maps from autocomplete and manage the shared round map pool.
- `/tournament-create name format` — admin: open a single- or double-elimination tournament.
- `/tournament-join tournament-id` — enter an open tournament.
- `/tournament-leave tournament-id` — leave before the tournament starts.
- `/tournament-start tournament-id` — admin: close registration and queue the first round.
- `/tournament-status [tournament-id]` — show entrants, losses, and champion.

## Setup

1. Create a Discord application and bot, invite it with `bot` and `applications.commands` scopes.
2. Copy `TournamentBot/tournament-bot.example.json` to `TournamentBot/tournament-bot.json` and adjust paths and IDs.
3. Set the token without storing it in Git:

   ```bash
   export YMCA_TOURNAMENT_DISCORD_TOKEN='...'
   ```

4. Build and run:

   ```bash
   dotnet run --project TournamentBot -- TournamentBot/tournament-bot.json
   ```

The configured OpenRA port range and join-page port must be allowed through the host firewall. Put the HTTP join page behind HTTPS (for example nginx or Caddy) and set `joinPage.publicBaseUrl` to that public URL.

## Match lifecycle

The `maxConcurrentServers` workers each own one port. Matches beyond this limit remain queued. A worker starts `OpenRA.Server`, waits until its TCP port accepts connections, then the bot DMs both players a HTTPS join button plus manual host/password details.

Each match gets an isolated `Engine.SupportDir`, so its logs and server replay cannot collide with other matches. Once a completed replay can be read with `OpenRA.Utility --replay-metadata`, the server process is terminated and the slot is returned to the queue.

Automatic replay results are not final until player feedback agrees. Conflicting reports, explicit disputes, missing players, and ambiguous OpenRA outcomes are sent to the admin channel for manual review. A technical rematch is queued automatically only when both players request one and does not advance the bracket.

Tournament scheduling supports single elimination and double elimination. In double elimination a player moves to the losers pool after the first loss and is eliminated after the second. If the one-loss finalist beats the undefeated finalist, the bot schedules the required grand-final reset. Odd player counts receive automatic byes between rounds.

Admins maintain one shared tournament map pool. Each tournament snapshots that pool when it starts. The scheduler randomly draws one map for the whole round, avoids consecutive repeats, and cycles through every configured map before reusing maps. Technical rematches retain the original round map.

## Notes

- OpenRA currently opens a lobby; one participant still starts the game from that lobby.
- Registered OpenRA names are used to match replay players. This is suitable for community tournaments but is not cryptographic identity verification.
- State is persisted as JSON using atomic file replacement. On restart, unfinished matches are queued again with new server processes and passwords.
