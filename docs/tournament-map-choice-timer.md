# Admin map-choice deadline

## Direct admin resolution (live 2026-11-21 13:06 UTC)

`/resolve` now offers pending-map-choice series (`S…`) alongside individual games (`M…`). Select S0027 / Boris–Fux and then the winning side to award the ENTIRE series without any map pick or timer. This does not create fictitious games, gives the losing side one tournament loss, closes any series timer and advances the bracket normally. Started/queued series cannot be resolved through a stale S-selection; use their individual game instead. Completed/cancelled tournaments and repeated resolutions are rejected. Discord acknowledgement is deferred while result notifications run.

Deployed source `2cc6f35a3d681a369c2615764807a831aafc6890`, successful workflow **44907162472**; image `ymca-tournament-bot:series-resolve-2cc6f35a3d68`. Idle/no-pending-timer checks passed, Compose/runtime validated before recreation, updated binary hash and Discord command registration verified, public HTTP 200. S0027 remained unmodified AwaitingMapPicks with zero matches: no winner selected automatically. 123 tests pass (one optional smoke skipped). First attempted rollout failed CLOSED on Docker's required PID output column; bot was untouched, then probe was corrected.

**Live as of 2026-11-21 12:58 UTC**, source `aa867b48c5fd...`, verified recovery run **44906521381**. Original run 44905181409 safely stopped the idle bot after retention ended but failed on Compose v1 override schema mismatch; adding `version: "3.7"` to match the base restored service. Public HTTP 200, Discord registration and new binary hash verified; T013 participants/results/round/series preserved. S0027 remains awaiting picks with no timer automatically armed. User must trigger `/tournament-timer` again.

`/tournament-timer` now includes both pending map-choice series and previously supported queued/starting/waiting individual games. It remains an explicit admin action; no automatic timer on registration, new rounds, or future games.

## Rules

- At trigger time, awaiting-map-picks BO3/BO5 series receive a persisted deadline exactly 15 minutes later. Existing picks count; BO5 requires both picks from each representative.
- Both sides receive a DM explaining the deadline and entire-series penalty; missing-side DMs include the actual map-selection controls. A reminder is sent about two minutes before expiry.
- Both sides complete in time: map-choice timer ends, game 1 is queued normally. No game-attendance timer is inherited.
- Only one side complete at deadline: that side wins the **entire series** by walkover. Record a full-series score, explicit reason/winner/timestamp, and ONE tournament loss for the loser. Double elimination still requires two series losses to eliminate.
- Neither side complete: admin review, no arbitrary winner. Admin can explicitly restart the timer. Late selections remain closed until a new deadline is set.
- A map-choice click processed at or after the deadline is rejected, even before the background watcher's next tick. Choice acceptance and expiry use the same state-store gate.
- Already running games are not affected. Existing individual-game attendance/forfeit rules are unchanged.
- A running timer cannot be silently extended by repeating the command.
- Bot restart preserves the recorded deadline but disables automatic map-choice forfeits for that timer: the bot could not receive choices while offline. Expiry then requires review; no loss caused by bot downtime.

## State and display

Series add `MapChoiceTimer`, `ForfeitWinnerDiscordId`, `ForfeitReason`, `ForfeitedAtUtc`. Final series result, processed-series marker, one loss, podium updates and next-round scheduling are saved atomically. Concurrent/repeated watcher ticks cannot award twice. No fabricated individual game or replay is inserted. Website shows deadline/review or an explicit whole-series walkover.

Use `/tournament-timer tournament-id:T013` after the update is live to start a new deadline. This implementation does not retroactively treat the earlier failed timer command as a running timer and does not automatically start a timer during deployment.

## Regression coverage

Tests cover no-game-yet trigger; exact 15 minutes and persistence; repeated trigger refusal; either-side BO3 walkover; BO5 partial picks; strict late-click rejection; concurrent expiry; two-sided absence/rearm; timely completion; future-game/future-round timer isolation; double elimination, champion and runner-up; round advancement; cancelled tournament; restart outage protection; visible deadline/walkover without fake replay links. Existing attendance, best-of, podium and remote-host suites also run.

## Deployment constraints

M0069 was observed Playing on the old native host on 2026-11-21. The PATH-only legacy compatibility bridge was installed but did not intercept that actual launch. Do not claim all games were already remotely hosted. The guarded bot update adds native remote-host configuration directly, avoiding the PATH interception dependency. Never stop/recreate the old controller while any game process, open match, or post-game retention remains. New map-choice behavior is not live until the guarded deployment succeeds and Discord commands are re-registered.

Guarded run **44905181409** built bot-only image `ymca-tournament-bot:map-choice-aa867b48c5fd` successfully. Read-only verification **44905699766** at 2026-11-21 12:48 UTC confirmed it was still waiting on M0070 Playing, with only S0027 awaiting map picks and no map-choice timer armed. It waits up to 20 minutes for no open games, no live server processes and no post-game retention, then backs up state/config/env and switches. No match-safety bypass. Check final run status; do not announce deployment success based only on the image build.
