# T009 / M0020 recovery — 2026-11-18

The user explicitly authorized fixing the blocked active tournament, after first
requiring no disruption while investigating. No general release/deployment was
performed and no new tournament was created.

## Root cause

The deployed v0.97.3 manifest had BOTH User map paths stamped as
`~^SupportDir|maps/ca/v0.97.3: User`. The engine packaging helper's set_mod_version
rewrote every User folder, including `generated`. The bot correctly copied the
map into `/data/matches/M0020/maps/ca/generated/`, but the packaged server never
searched that directory. It aborted with `Unable to resolve a valid initial map`
and Linux exit code 134. Earlier source-tree hosting tests missed the packaging
transformation. The map itself was valid.

Map: Tactical MountainValleys 2P, seed 409274986.
UID: `93bb080d4c65f274a1040b11d4ee065c2e8bac03`.

## Authorized live repair

- Read-only diagnostics through the existing GitHub Actions SSH credentials on
  the actual tournament host. Local SSH authentication was unavailable; the first
  local lxgr.xyz alias was a different host and was not the tournament server.
- Confirmed M0020 Failed, T009 Running, S0001 Playing; no queued/starting/waiting/
  playing matches and no child game-server processes before the restart.
- Backed up the deployed manifest and quiescent tournament state under deployment
  `data/recovery/M0020-20261118T221015Z-93a6b3db/` (private backup directory).
- Added the missing stable generated-map search path to the running container's
  manifest. Did not rebuild or replace the image.
- Started an isolated, unadvertised preflight using the ACTUAL deployed Linux
  executable on unmapped container port 12409 and a private /tmp support folder.
  It selected the exact requested UID. The diagnostic timeout stopped only that
  test process.
- Rechecked active-match/process guards, briefly stopped the idle bot, backed up
  state, changed ONLY M0020 status Failed -> Queued and cleared FailureReason,
  then started the same container. Startup reused the immutable generated map.
- No player, series, bracket, result, seed, UID or generated-package checksum was
  changed. Bot startup generated fresh join credentials normally.

Recovery run: https://github.com/patrickwieth/YMCA/actions/runs/44408906743
Verification: https://github.com/patrickwieth/YMCA/actions/runs/44409488816
At 22:13:43 UTC, M0020 was WaitingForPlayers on port 12400, both participant join
pages returned HTTP 200, two join DMs were stored, and lobby state was WaitingPlayers.
No claim is made that the players had already started or completed the game.

Maintenance commits are confined to `maintenance/m0020-recovery`, modifying only
that branch's copy of the existing manual SSH workflow. The normal release/bot
branches were not pushed or deployed. After recovery the maintenance workflow was
returned to read-only verification, not left as a repeatable mutation job.

## Client workaround and permanent correction

Published v0.97.3 clients have the same stamped-folder issue. Until updated,
manually downloaded tournament maps belong in `OpenRA/maps/ca/v0.97.3/`, followed
by restarting the client, rather than the generated folder suggested by the old
join page. Use the bot's new personal join links after this recovery.

`tools/patches/stable-generated-map-folder.patch` and its regression test preserve
`maps/ca/generated` while still versioning the ordinary User folder. The patch is
included in `tools/prepare-engine-patches.sh`, covering Docker and platform builds.
A repeat-version-stamping test passes for v0.97.3 and v0.97.4. This permanent source
fix is local and UNRELEASED. The live container manifest repair survives a restart
but NOT container recreation from the old image: ensure the source correction is
in the next approved image before recreating it.

The duplicate-registration UX fix is still pending locally: current Entrants
already deduplicates by Discord ID, but repeated joins receive misleading success.
Do not lose this request; add explicit feedback and concurrent/modal regression
coverage without changing the ongoing tournament's participants.
