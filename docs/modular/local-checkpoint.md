# Local modular checkpoint

Worktree: `Game`, current Git branch `isometric`. This is a local development
checkpoint, not a production/release merge or deployment.

Included: current runtime rules/sequences/assets, modular traits and utilities,
art preparation tools, regression tests, placement/binding metadata and selected
final texture/chemical-tower/silo artifacts. The pinned-engine patch now preserves
all current engine source deltas, including Build Everything and launch-URI session
reset fixes. It was checked against pristine pinned engine source and against the
current working source.

Excluded intentionally: `mods/ca/bits/nod/buildings/conyard.png` (explicitly
protected), generated build/validation binaries and caches, generated test maps,
Blender backup files, and the bulk of intermediate model/render passes. These are
left untouched locally, not deleted. Some historical art-study tools/tests depend
on those local intermediate studies and are not standalone checkout tests.

Checked immediately before committing:
- Current building follow-up, GDI turret texture-only and Sentry replacement tests.
- Engine patch applies to the pinned base and exactly reproduces current source.
- Recent full sprite-load and silo regression tests passed.
- Engine crash-fix regression tests passed in isolated builds. Full engine suite
  previously had a separate LaunchUriBroker IPC timeout; no claim of full-suite green.

Latest state supersedes historical art notes: GDI factory solid3x3 body plus reserved
passable4x4 bib; Nod factory rightmost cells solid and bottom cell passable; Scrin
refinery/warp sphere/advanced reactor upper2x2 solid. Chemical tower pass06 on a
correctly aligned1x1 bib with final12px downward body placement. GDI ATWR pass16 is
a texture-only VXL update; existing base, geometry, scale and pivot are preserved.
