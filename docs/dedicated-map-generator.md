# Dedicated-server Change Map generator

Local production fix in `Game-master` (`master`). No commit, push, release or deployment.
`Game` / `isometric` and `Game-release` are not part of this change.

## Player behavior

The **lobby administrator** on a dedicated server can open the normal random-map
setup from **Change Map**, with Tactical, Operational, Strategic, presets and
custom settings. This uses the existing lobby map-change permission, not ownership
of the dedicated-server process. Ready/start restrictions remain in place and
permissions/current map are checked again when submitting the dialog.

Non-admin players retain the original read-only map browser. Hand Made Map uses
the existing server-aware chooser, including its map pool. Local skirmish
behavior is retained. v0.97.3 left peer-hosted multiplayer on the old chooser;
the local [LAN follow-up](lan-map-generation-fix.md) now extends recipe sync and
the administrator's generator to that path as well (not yet released).

The previous failure was the `if (!skirmishMode) return` guard in
`GeneratedMapLobbyLogic`. Removing that guard alone would have offered generation
but selected a map that existed only on the administrator's client.

## Server-approved generation and distribution

- A versioned, bounded recipe carries all generator settings and the seed. It
  accepts no paths, uploaded packages, scripts, custom rules or type names.
- The dedicated server validates admin permission, lobby/ready state, current map,
  enum values, size (64–256 cells per dimension), supported players and two teams.
- Requests are rate-limited to one every ten seconds, with at most one generation
  in flight. Planning runs asynchronously. Before export, the server rechecks the
  administrator connection, permission, readiness, selected map and match state.
- Successful maps are installed on the server, then selected through the existing
  `map` command, retaining standard slot reassignment, readiness resets and rules.
- The selected map's UID and recipe travel in locked lobby state. Existing clients
  and late joiners generate locally and must match the server's UID before they
  report NotReady/map availability. No public repository upload is performed.
- Client failures and checksum mismatches remain explicit errors; they do not
  silently select a different map or mark the client ready.
- Recipes for earlier generated maps remain available during that server session,
  so selecting one again also supports late joiners.
- An explicitly configured server map pool is respected. The automatic local pool
  created by `QueryMapRepository=false` is extended with server-generated maps and
  synchronized to clients; it is not mistaken for an explicit restriction.
- Clients retain existing temporary-map cleanup on game exit. The server tracks
  only its own generated files, closes their previews and attempts hash-guarded
  cleanup on normal server shutdown. Force-killing a process can leave its
  temporary files, as with other temporary-map generation.

**Release requirement:** updated client **and dedicated-server** mod assemblies
are needed. The new client recognizes server capability; an old server produces a
clear update-required message instead of a broken map selection. This has not
changed any currently deployed server or installed player client.

## Validation

- Full `Modular.Tests`: 319 passing tests, including the new recipe roundtrip and
  malformed/oversized input tests. All supported preset/mode combinations and
  custom setting combinations are covered by recipe serialization checks.
- Both mod assemblies build against a clean copy of the pinned engine with the
  committed engine patches applied. The existing local `Game-master/engine` was
  partially patched and was **not** overwritten. Validation runtime is isolated
  under the workspace `.tmp/dedicated-generator-check/` directory.
- `tools/test-dedicated-map-generator.ps1` runs a real loopback dedicated server,
  a remote admin and an ordinary player, each with separate support directories.
  It drives real Change Map UI callbacks, checks all three modes/custom screens
  and the ordinary map browser, sends an unauthorized generation request, then
  generates through the administrator's dialog. Both clients obtain the server's
  UID with map-repository queries/downloads disabled. A fresh late joiner with an
  empty generated-map cache obtains the same UID. The first guest is retired
  before the late joiner starts to keep only two GPU renderers active.
- Final native test output is recorded in `docs/validation/dedicated-map-generator/`.
  The diagnostic UI probe is opt-in through explicit environment variables and
  does not run in ordinary clients. Only test-created processes are stopped.

Example (Windows; use an unused output directory beneath the named TEMP folder):

```powershell
./tools/test-dedicated-map-generator.ps1 `
  -EngineDirectory <built-engine-directory> `
  -FixtureMap <ordinary-playable-map.oramap> `
  -OutputDirectory "$env:TEMP/ymca-dedicated-map-probes/run"
```

## Separate requested menu-scene adjustment

`mods/ca/maps/liberty-stream-shellmap/liberty.lua` now schedules recurring Kirovs
with `DateTime.Seconds(14)`, matching the final requested isometric interval. The opening
18-airship column, 48-live-airship cap and normal-match Kirov production/balance
are unchanged.
`tools/test_liberty_spawn_timing.py` checks the new interval and those preserved
scene settings.

The user authorized these changes for v0.97.2. Keep the separately implemented
Battlefield News feature in the next approved release as documented in
`battlefield-news.md`; nothing here publishes or deploys it.
