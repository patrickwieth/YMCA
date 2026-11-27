# Game development checkpoint — 2026-11-27

User authorized committing/pushing the current intermediate development state, then
continuing Urban Warfare. Target: existing `isometric` development branch in Game.
This is not a release, tag, deployment or merge into Game-master/Game-release.

## Included

- Current map/building/cameo/construction/biome/chooser/projectile fixes, supporting
  engine patches, tools, tests, documentation and associated review evidence.
- Supplied urban source collection, source-only cutouts and provenance, guarded
  extraction tools, internal modular road-core/snow image assembly and planning.
- Development maps relocated out of the normal chooser: testmap1.oramap,
  testmap2.oramap and modular-gdi-lab.oramap match their prior committed bytes.

## Intentionally outside this checkpoint

- `mods/ca/bits/nod/buildings/conyard.png`: explicitly protected; its existing local
  modification is neither staged nor reverted. Working-tree art checks involving
  that file must not be represented as validation of the committed original.
- Unrelated turret/model art experiments, Blender backup files, build outputs,
  Python caches, private files and other worktrees. These remain on disk unchanged.
- Ignored engine working sources are represented by registered engine patches,
  not by vendoring the engine directory into this repository.

## Verification

- Isolated CA build succeeded, 0 errors; existing warning backlog remains.
- Urban source/road checks: 7 passed; missile-height policy: 6 passed; MLRS flight
  regressions: 3 passed; Windows engine patch tests: 3 passed.
- Selected text files checked for private-key/GitHub-token/AWS-key signatures;
  no matches. This limited scan is not a claim of a comprehensive security audit.
- Whitespace check reports only trailing blank lines in archived engine-resolved
  rules/diagnostic captures; original evidence was not rewritten for formatting.
- Workflows inspected: branch push does not invoke release packaging (tags only)
  or deployment (manual). No release validation or live deployment is claimed.

## CI follow-up

Checkpoint `87dd31d0` was pushed to `isometric`. CI run `45997669015` built the
engine and mod but failed when Modular.Tests tried copying the relocated lab from
its old path. Corrected the project input to
`mods/ca/development-maps/modular-gdi-lab.oramap`; all 365 Modular.Tests pass in an
isolated local output directory. This is a test-fixture path correction, not a map
content change. Follow-up `21d6b522` passed the .NET jobs but exposed Mono's
netstandard2.1 API limitations: the audit used .NET 6 hashing helpers and resource
field gathering used .NET 6 PriorityQueue. Replaced hashing with SHA256.Create /
ComputeHash and lowercase BitConverter hex; replaced the queue with a sorted set
of the same total (distance,x,y) priority. Duplicate priorities describe the same
cell and were already skipped by the visited set, so removing queued duplicates
does not change traversal order. Building-audit output, the 30-case economy report,
and all six 4P seed42 Plains planning/debug files remain byte-identical before/after.
The isolated CA build passes. Check the next CI run before claiming Mono success.

## Still unfinished

Urban Warfare is not a playable map or registered runtime art set. The current
road assembly is an internal image test, not an engine render. Sidewalks/curbs,
markings, transitions, more independently placeable buildings, river crossings,
actual footprints/garrisons and native validation remain open. Existing bunker
window-aligned fire-port work also remains open. See `urban-art-inventory.md`,
`urban-warfare-map.md`, `bunker-fire-ports.md` and `building-followup-pending.md`.
