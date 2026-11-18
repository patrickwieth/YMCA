# Dedicated lobby: select a map before starting

Prepared locally in Game, Game-master and Game-release. No commit, push, release
or live-server deployment is authorized by this follow-up; v0.97.2 is unchanged.

Ordinary dedicated servers start on the bundled `please-select-map` waiting map,
not the building test map, even if an old default `Server.Map` is configured.
Its title is **Please select a map**. The client hides the normal preview/details,
shows that prompt, and disables Ready and Start. Change Map remains available
under the existing administrator permissions; no extra rights are given to guests.

The hidden map has eight waiting slots and cannot be chosen from normal map lists.
It has no buildings or combat actors. `tools/generate_lobby_placeholder.py`
reproduces its small flat map and transparent preview; source art is untouched.

`MapSelectionLobbyCommands` wraps the engine lobby commands, without engine source
patches. It temporarily admits the placeholder during initial pool loading, then
restores the operator's configured or automatically derived pool. Repository-
enabled servers regain an unrestricted pool. Selection/generation then follows
the normal engine map-change path, including resetting player readiness.

Server-side enforcement rejects Start and every Ready spelling accepted by the
engine enum parser (including `Ready, Ready`). This also prevents ready-based
auto-start; the disabled button is not the security boundary. Once a real map is
selected, the existing start rules apply unchanged.

Local/skirmish servers and reserved matches with `AllowedPlayerNames` keep their
explicitly assigned map. Tournament generation, fixed maps, and rematches are
therefore not delayed by an unnecessary manual choice. Selecting a map is not
reset merely because another player joins; late joiners see the chosen map.

Validation:
- Native `--check-map-selection-lobby`: map metadata, manifest handler, visibility,
  normal selection commands, Ready aliases and Start policy.
- Real dedicated admin/guest/late-joiner checks with automatic local pools,
  repository-enabled pools, and an explicit single-map pool. Direct Ready/Start
  attempts were rejected; generated and pre-existing selections enabled Start.
- The explicit pool remained exactly the operator's original pool.
- Real two- and four-player tournament host tests still selected the assigned map
  and served its verified download.
- Full local Singleplayer/Skirmish and repeated Change Map UI regression passed.
- Screenshots were inspected; the old map preview is replaced by the waiting text.

Evidence is in `docs/validation/dedicated-waiting-lobby/` and TEMP probe directories
`waiting-lobby-final-01`, `waiting-lobby-existing-pool-02`, and
`callback-args-5787f8c51e164b7bb48f83db9ad019f3`.

## Local installation

The user's Game process was still running at the end of validation and was not
stopped. Updated CA/Cameo assemblies are staged in
`.art-validation/allied-bib-remap/runtime/bin/`. After closing the game, its
`install.ps1` can replace both locked runtime assemblies with backups. The source,
manifest and bundled map changes are already in the worktrees, but the normal
Game runtime must receive those new assemblies before testing this feature.
Updated clients and dedicated-server assemblies must be shipped together later.
