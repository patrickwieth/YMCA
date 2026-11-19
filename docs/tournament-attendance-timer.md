# Admin-triggered 15-minute attendance timer

`/tournament-timer tournament-id` requires the configured tournament admin role
(or Discord administrator permission). It snapshots all currently queued, starting
or waiting matches of that running tournament. Each gets the same deadline,
exactly 15 minutes after the explicit admin command. Running games and matches
created later are not included. A second command cannot silently reset a pending
timer. No timer is enabled by default or automatically repeated.

## Presence rules

At expiry, the dedicated server's own thread checks validated non-spectator
players against the reserved team rosters:

- Exactly one complete side present, the other entirely absent: the absent side
  loses this game automatically. This is a recorded no-show forfeit, not a replay
  outcome or an unconfirmed player report.
- Both sides present (even if not Ready), both absent, or partial teams: no winner
  is invented; the admin receives a review message.
- A game already started: no forfeit and no host shutdown.
- Spectators do not count as players. Presence is deliberately not the Ready flag.

Forfeits advance the normal BO series/bracket once. A later game in that series
requires another explicit admin timer. The original deadline and triggering admin
are persisted, along with the decision/audit reason. Existing results and manual
resolutions take precedence over stale timer processing.

## Safety and recovery

The bot writes a nonce-bound local request into the owned match support directory.
The host makes the presence decision and shuts down the forfeited waiting lobby
on its server thread, avoiding a bot-poll race with StartGame. Only then does the
bot accept the matching decision and record the result. Request ID, match ID,
map UID, current match/tournament phase and decision time are checked. Decisions
made later than the short expiry polling window cannot forfeit a player.

A lobby that was not yet available when the timer started is ineligible for an
automatic loss: queue/build delays must not steal the players' 15 minutes. A bot
restart preserves deadlines but disables automatic forfeits for restarted hosts.
Missing, malformed, mismatched or late server decisions lead to admin review,
never a guessed winner. A crash between recording a result and advancing the
bracket is recovered using the persisted processed-match IDs.

The admin-trigger notice is announced publicly and sent to affected participants;
a two-minute warning is persisted to avoid repeat spam. Notifications are best
effort and failures are logged. Deadline/forfeit reason appear on the join page
and public tournament page. Private admin IDs and request tokens are not exposed.

## Validation / deployment

95 bot tests passed, including presence/team combinations, trigger scope, exact
15-minute deadlines, persistence/restart behavior, duplicate starts, one-time
advancement, stale decisions, unavailable hosts, warnings and manual-result safety.

Four native isolated GUI/server tests passed:
- One player plus a spectator: OneWins, authoritative waiting-host shutdown.
- Both players present but not Ready: Review, host remains available.
- Neither player present: Review, host remains available.
- Already started game: Started, host remains available.

Native fixtures use a short explicitly injected deadline; the real bot command's
15-minute interval is covered separately. They do not create/modify a live tournament
or start any production timer. The harness retains process handles to verify exit
codes for quickly terminating owned servers.

Deployed after T010 completed, together with the public-page/post-game changes.
Source commits: `a4496dc4`, `4c91a094`; Windows .NET, Linux .NET and Linux Mono CI
succeeded. Normal guarded deployment succeeded without cancellation/force:
https://github.com/patrickwieth/YMCA/actions/runs/44516265491 .
Discord command registration and host v0.97.4 were verified at 11:38 UTC on
November 19. Follow-up read-only verification at 11:41 UTC confirmed no active
matches, no pending attendance timers and the deployed attendance trait:
https://github.com/patrickwieth/YMCA/actions/runs/44517021570 .
No production attendance timer was activated by the assistant.
