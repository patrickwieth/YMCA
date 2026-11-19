# Public tournament pages, post-game grace, and admin match choices

## Public pages

`/tournaments` lists tournaments; `/tournament/T010` shows the corresponding
round-by-round bracket, actual progression connections, upper/lower/final/bronze
labels, series scores and match statuses. Future rounds are not invented: this
scheduler pairs them after the preceding round is complete. The page refreshes
in place every 10 seconds and preserves horizontal bracket scrolling. Registration
and status announcements link to the page. Spectator links are limited to public
waiting lobbies; replay links require completed matches.

Rendering takes a consistent state-store read. Only HTML-encoded public names,
results and IDs are rendered, never participant passwords, spectator passwords,
Discord account IDs, private participant URLs or filesystem paths. The HTML is
not a raw serialization of tournament state. Responses use no-store and a CSP;
scripts are served from the same origin. Unknown tournaments return 404.

## Three-minute post-game window without delayed reporting

The existing host configuration exited five seconds after game over. The bot also
killed the process as soon as it read a finished replay. Merely changing that delay
would either delay replay-based reporting or still immediately kill the server.

`tournament-postgame-replay.patch` separates replay finalization (the existing
five-second defeat/sync settling window) from host shutdown (180 seconds). The
engine continues serving connected clients after publishing the completed replay.
The dedicated entry point now actually exits after an ExitOnGameOver shutdown;
previously its outer loop could remain alive even after its server thread stopped.
A shutdown-complete signal ensures cleanup finishes before disposal/exit.

The bot sends result/confirmation messages as soon as the replay is readable, then
retains the worker and its port until the process exits. Other free workers can
start matches immediately; if all ports are occupied, new games queue, but result
reporting is not held back. No client update is needed for this server-only behavior.
If all players leave voluntarily, the existing empty-server shutdown can finish
early. A bounded post-result timeout retires a stuck owned process after the grace.

A persisted ServerRetainUntilUtc lease blocks deployment during the statistics
window, even after a result is confirmed; it is cleared on process exit. The
normal deployment guard checks this before and after building, and fails closed
on unreadable/malformed state. Late result/cleanup callbacks do not undo completed
admin resolutions or other recorded terminal decisions.

## /resolve and BO3 context

The winner option was a general Discord User selector. It is now a match-specific
string autocomplete containing only that match's two players/teams, independent
of Discord's member suggestions. Values bind the choice to the selected match ID;
stale choices from a different match are rejected. Team choices resolve to their
representative. Match-ID autocomplete lists unresolved matches.

A reported extra BO3 map choice was checked read-only against production. T010's
first series S0002 was recorded 0:2 after M0021/M0022; M0023 was game 1 of the next
series S0003 in round 2, not game 3 of S0002. No results were edited. Pick/result
messages now state round, series and game context, and describe that the deciding
map is already fixed. Stale pick requests are suppressed outside the pick phase.

## Validation and rollout

- 77 ordinary bot tests passed, including real HTTP page requests, encoding/secret
  exclusion, persistence visibility, result immediacy, late admin-result safety,
  match-specific winner choices and a generated BO3 1:1 frozen-decider regression.
- Python tests cover active hosts, post-game leases, expiry and fail-closed guards.
- All three opt-in native host tests passed (2-/4-player generated hosts plus the
  retained-port/result callback probe).
- Source commit `d7894eed` was pushed. Windows .NET, Linux .NET and Linux Mono CI
  succeeded: https://github.com/patrickwieth/YMCA/actions/runs/44492102039 .
- Native postgame-03: two real GUI clients finished a match, the completed replay
  became readable promptly, both clients remained connected in the post-game UI
  at 170 seconds, and the owned server process exited at approximately three
  minutes. Evidence: temp `ymca-map-download-probes/postgame-03`.
- A separate explicit native host/worker test injects a completed diagnostic replay
  into its own isolated host, verifies immediate ResultAvailable, and verifies a
  second queued match cannot reuse its still-occupied port. It does not claim a
  fabricated match was played on that host; actual gameplay is covered above.
- Full engine patch stack applied cleanly to the pinned upstream sources, including
  reverse/repeat application checks. CA/Cameo and dedicated host builds succeeded.
- Desktop and narrow-layout HTML rendered in an isolated headless Edge profile;
  bracket links, encoded hostile fixture text and horizontal scrolling inspected.

Native testing found the lingering dedicated-process issue and fixed it before
rollout. The initial replay poll probe also needed to tolerate an unfinished
metadata footer; no production error was suppressed.

Rollout pending: at the latest read-only check (November 19, 08:59 UTC), T010
was running and M0024 was WaitingForPlayers (M0023 had completed). Do not cancel
a tournament, edit results, or bypass deployment guards for this update.
