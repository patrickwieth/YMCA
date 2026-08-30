# YMCA Tournament Bot

Discord tournament orchestration for YMCA/OpenRA. The bot registers players, queues matches, starts a dedicated OpenRA server per active match, sends private join details, imports the server replay result, and asks both players to confirm, request a rematch, or dispute the result.

## Commands

- `/register openra-name` — associate a Discord account with an exact OpenRA player name.
- `/match player-one player-two map-uid [map-title]` — admin: queue a match.
- `/matches` — show recent matches.
- `/resolve match-id winner` — admin: resolve a disputed match.

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

Automatic replay results are not final until player feedback agrees. Conflicting reports, explicit disputes, missing players, and ambiguous OpenRA outcomes are sent to the admin channel for manual review. A rematch is queued automatically only when both players request one.

## Notes

- OpenRA currently opens a lobby; one participant still starts the game from that lobby.
- Registered OpenRA names are used to match replay players. This is suitable for community tournaments but is not cryptographic identity verification.
- State is persisted as JSON using atomic file replacement. On restart, unfinished matches are queued again with new server processes and passwords.
