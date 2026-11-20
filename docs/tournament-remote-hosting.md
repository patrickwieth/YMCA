# Remote tournament hosting

The bot can delegate new games to an authenticated game-host agent while retaining ownership of registrations, rounds, reports and results. Game host contains no Discord credentials. Gameplay and v0.97.6 assets are unchanged.

## Native controller configuration

Add to `server` (keep old local configuration for legacy pinned matches):

```json
"remoteHost": {
  "id": "worlds",
  "baseUrl": "https://ymca-worlds.echtkpvl.de/game-host/",
  "publicHost": "ymca-worlds.echtkpvl.de",
  "expectedGameVersion": "v0.97.6",
  "tokenEnvironmentVariable": "YMCA_GAME_HOST_TOKEN",
  "maxConcurrentServers": 8
}
```

Set an independent random API token (32+ characters) in both environments; never put it in URLs, committed JSON, public links or logs. HTTPS is mandatory except loopback test APIs. Public player/spectator links use the match's pinned hostname, not the current global default.

Run the agent with `dotnet TournamentBot.dll --game-host /config/game-host.json`; see `TournamentBot/deploy/game-host.example.json`. Publish game TCP ports 12400–12407. Bind API only to host loopback; TLS proxy forwards `/game-host/*` after stripping the prefix. Every API route, including health and artifacts, requires Bearer auth. API HTTP listener is 5081, not the public join website.

The public map-download base URL points to the controller, which mirrors generated packages with checksum validation before sending join DMs. Already-generated maps are uploaded byte-for-byte via the authenticated, size-limited map endpoint before creating the job: ZIP timestamps mean regenerating the same seed can preserve the UID but change the archive SHA-256. The bridge was tested with an original archive produced at a different time, verifying identical UID and SHA-256 on the remote game. Finished replays and lobby snapshots are copied back to the controller. Attendance requests go to the authoritative game process; the controller applies the decision using existing validation.

## Lifecycle and failure rules

- Host/request identity is committed before POST. Concurrent/retried creation is idempotent. Conflicting IDs are rejected rather than starting duplicates.
- New unassigned matches go to the remote host. Already assigned matches stay pinned. Legacy active games remain local unless a verified relay marker identifies a remote job.
- Temporary API/network outage retries without starting a local replacement. Existing remote games keep running when controller disconnects/stops.
- Host agent restart does NOT silently restart active games with new credentials: existing results remain available; interrupted games fail for admin review. Guard agent restarts against active games.
- Private file paths, executables, ports and winners from POST input are ignored; only allowlisted game details reach the agent.
- Version/hostname/protocol checked before a new remote job. Map identity and checksum checked before publishing credentials.
- Remote native controller supports direct remote links. This source update is not required on player clients (v0.97.6 remains the required game version).

## Live legacy compatibility bridge (no bot restart)

The existing deployed controller cannot load new C# code while running, and restarting its container would kill local games. `TournamentBot/deploy/remote-relay.py` is a transitional adapter installed atomically as `/usr/local/bin/dotnet`, ahead of the real `/usr/bin/dotnet` in PATH. Only newly invoked `OpenRA.Server.dll` commands named `YMCA Tournament M...` are intercepted. All utilities and unrelated .NET commands exec the real runtime unchanged. Existing child processes are untouched.

For each new game the bridge creates an authenticated remote job and preserves the bot-generated player/spectator passwords. Agent must explicitly enable `allowRelayCredentials`. The old advertised TCP port relays bytes to the remote server, so original join pages and links remain valid. Simulation and dedicated process run on the new host; old host retains lightweight forwarding, bot, initial generation and replay-metadata polling. This is not zero old-host load or a direct client-to-new-host path yet.

The bridge mirrors lobby/attendance files and finished replays into the original match support directory. Native bot then processes results normally. Each match stores a `remote-relay.json` marker containing host ID, durable request ID and remote port (no passwords/token). Updated native controller can adopt those jobs directly at a later safe deployment. No tournament-state JSON is patched by the live bridge.

Do NOT remove the bridge or restart/recreate the old container while native local games are active. Container recreation removes the runtime-installed Python/bridge. For a later deployment use the native remote-host controller config and token; it reads relay markers and reattaches. Keep old public proxy ports alive until clients move to direct links. Do not run a second Discord bot against copied state.

## Validation and staging (2026-11-20)

- Unit/integration suite covers legacy routing, identity pinning, duplicate requests, API auth, conflicting identities, traversal rejection, expected version, map checksum, credential validation and relay adoption.
- Python relay tests check exact bidirectional TCP forwarding, command interception and redirect rejection.
- Native isolated agent on new host generated Continents seed 20261120 and served exact map UID `72a0bf67d3c7c096115e447f56d2029db456044d`; remote controller test disconnected/reconnected without changing port/password or stopping the game.
- Isolated compatibility relay exposed public port 12408 and forwarded the actual v0.97.6 engine handshake. Player and spectator passwords matched the controller's original passwords exactly. Isolated test processes were stopped afterwards.
- Agent prepared under `/opt/ymca-game-host`, container `ymca-game-host`, API behind Caddy at `/game-host/`. Source game image remains v0.97.6, separately mounted new agent assemblies. Token stored in protected `host.env` and deployment secret, not source.
- Live bridge activated 2026-11-20 at about 23:35 UTC. Install run 44837893551 reached activation, then failed its final `dotnet --version` probe because the runtime image contains no SDK (not a relay failure). Verification run 44838042393 succeeded with `--list-runtimes` and matching installed wrapper SHA-256. Container start remained **2026-11-20T20:24:51.142816642Z**; no bot or existing game process restarted.
- At 23:36 UTC M0068 was the only remaining open local match (created before activation). No actual tournament game had yet started on the remote agent. Do not conflate isolated smoke tests with the first real remote-match verification.
- Runtime bridge artifacts/installation log under old deployment `data/remote-host-bridge`. API token is `YMCA_GAME_HOST_TOKEN` GitHub secret and protected files only. Maintenance workflow returned to verification, not left as an automatic installer. **Do not recreate the old bot container from the unmodified release image:** native remote hosting code is on `feature/tournament-remote-host`, not merged into release; recreation would remove the live bridge.
