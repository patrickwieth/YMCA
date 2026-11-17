# Local isometric / tournament engine integration

The engine revision `484afea27c973b843edc09346b2bd0110370057e` introduced by
Game merge `9d8a9dec` returns HTTP 404 from the configured engine archive URL.
Reconstructed the integration from the two published engine parents:

- `e9c4af27df97521237cfe380addcaf4e29404a69`: isometric rendering, autotiling,
  building anchors and repair transit cells.
- `f31038bf1f3ccecb20e55eaa281288d6ec430360`: tournament lobby, spectator/replay
  links, updater and one-minute connection timeout.

The conflict-free merge is `2dfb43c864804ede5efca9e31f7add52f7233aa3`, on branch
`isometric-tournament-integration` in the sibling OpenRA repository. Its separate
worktree is `../.ymca-engine-integration`; the original OpenRA checkout was not
switched or modified. Both parents are ancestors of this merge.

`mod.config` now references this revision. The ignored `engine/` directory was
installed from its Git archive and built, without ad-hoc engine source patches.
The previous engine is backed up at
`../.ymca-engine-before-tournament-merge-e9c4af2`.

## Validation

- Engine Release build and YMCA Release build passed.
- Engine NUnit suite: 141 passed, none skipped.
- Strategic front generator: 24 cases, each generated twice, passed.
- Modular preview rules: 89 bindings passed (not a visual acceptance test).
- Actual client wizard, chooser/cancel, Customize, back navigation and opposing
  default bot passed in a fresh UI probe.
- User's Strategic Mountain Valleys seed `234373457`, four players: production
  export passed; separate gameplay diagnostic passed four team/spawn assertions
  and twelve autonomous checkpoint visits in both directions, ending at tick 1978.
  This diagnostic uses accelerated waves and disabled victory; it is not a full
  match or multiplayer acceptance test.

Evidence is in `%TEMP%/ymca-merged-engine-*.log`,
`%TEMP%/ymca-ui-probes/engine-merge-2dfb43c8/`, and
`%TEMP%/ymca-merged-engine-front-play/` (framebuffer and runtime-results pointer).

The reported `CHNukeCannon.Impact` rules failure occurred in a client left running
across the Game merge: its cached weapons predated the newly read actor rules.
The weapon definition exists; neither the weapon nor the generator needed a
workaround. Restart the client after updating.

## Publication

The engine merge is now published to `patrickwieth/OpenRA` on branch
`isometric-tournament-integration`, at `2dfb43c864804ede5efca9e31f7add52f7233aa3`.
The configured GitHub archive URL was downloaded successfully before committing
the Game pin. All 2,486 matching source/project/YAML files in the downloaded archive
matched the locally built `Game/engine` tree (ignoring line-ending differences).
Evidence: `%TEMP%/ymca-publish/engine.zip`. The original engine checkout and remote
default branches were not changed; publication used the dedicated integration branch.
