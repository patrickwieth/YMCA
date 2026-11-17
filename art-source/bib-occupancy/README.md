# Building ground masks and unit selection — current status

**Newest batch:** [missile/Chem Tower integration, Soviet masks/service zone and China4x2 selection](../../docs/modular/building-art-latest-followups.md).

**Previous incremental changes and runtime-build status:**
[placement / hover / crate follow-up](../../docs/modular/placement-hover-crate-followup.md).
That follow-up supersedes the earlier exit/docking-reservation and runtime-build
statements below where explicitly noted.

## Ground and placement

The global bib reservation pass is now followed by an explicit per-building ground
review in `rules/building-ground-masks.yaml`. `ground-review/masks.json` records the
cell decisions and their reasons. `tools/art/complete_building_ground_masks.py`
reproduces them from the frozen `ground-review/baseline-geometry.json`; never recapture
that baseline from already-modified rules. The earlier `binding.json` describes the
first reservation-only pass and is retained as historical input.

Reviewed scope: all decorative-bib faction buildings and their inherited variants,
plus native faction shipyards, the bibless Soviet Barracks, Chinese bunkers and the
Allied flak base. Neutral scenery/mission actors and unrelated art studies are not
silently changed. Small approved defense bases retain their pixels/pivots/muzzles.

- Every bib tile prohibits construction. `x`/`X` are structural ground obstacles;
  `=` remains walkable; `+` is a passable service/exit cell where units cannot park.
- Rear/side workshops, towers and root clusters replace the old displaced rectangles.
  Airport runways, factory aprons and service entrances are not treated as walls.
- Ground silhouettes were reviewed on labelled native-art composites, NOT inferred
  from roof alpha, tall antennas or selection bounds. Cells are coarse collision
  approximations, not pixel-perfect polygons.
- Actor centers and existing production exit/docking offsets remain unchanged,
  except the subsequently requested GDI Airfield landing point (see follow-up).
  Refinery docking cells retain access. Each ground producer has an open exit route;
  all marked service cells connect to the outside of the building footprint.
- Shipyard support cells stay solid, corner-water exits stay free, and the platform
  envelope cannot be overlapped by another building.
- Negative footprint anchors are supported. Base-adjacency scanning now also honors
  FootprintOffset, rather than searching the old location-relative rectangle.

Explicit user patterns:

China Power / Advanced Power / Barracks, GDI Upgrade Center, Scrin Nerve Center:
```
xx=
xx=
===
```
Scrin Portal (two-by-one building, two-by-two bib):
```
xx
==
```
Scrin Repair Bay (left-middle repair tile):
```
xx=
+x=
===
```
Only `(0,1)` is a Scrin repair destination. The existing Resupply activity targets
transit-only cells, so this is functional repair routing, not just a visual marker.
Both China power bodies had already been raised32px in the preceding feedback pass;
no additional artwork changes are made by the ground-mask pass.

## Construction and selling

Scrin bibs render first; body presentation stays hidden during build-incomplete.
Animation clocks and make/sell completion callbacks keep ticking. The regression
check exercises the real RenderSprites visibility wrapper and condition observer in
both animation directions, not only YAML flags. Other factions keep their Chinese
construction-site animation.

## Unit-selection crash

The new body-fitting selection code previously queried the trait dictionary from a
late ScreenMap bounds update after a C17 had been disposed. Fixed in two places:
- selection decoration returns no bounds for disposed actors and caches live body
  providers instead of looking up traits every frame;
- ScreenMap processes queued removals/disposal before querying any bounds provider.

`--check-selection-lifecycle` tests pending update+removal, disposed updates, repeated
render ticks, cleanup of both partitions, empty disposed selection bounds, and prism
containment for short/tall/wide/off-center body rectangles. It deliberately supplies
no usable actor world/traits, so accidental late provider access fails the regression.

## Additional requests

- GDI AMCV and Nod AMCV.Nod deploy/redeploy into their own normal/Nukular construction
  yards. Nod starting presets, doubled starts, recovery husks, crates, AI recognition,
  squad exclusion and Nukular cargo conditions are separate. Existing MCV art/stats
  are shared; this is an actor-identity split, not newly drawn MCV artwork.
- Airship of the People and its promotion have hard Operational/Soviet production
  roster restrictions. Unlike ordinary tech prerequisites these cannot be bypassed
  by All Tech. Empty/default/Tactical/Strategic maps and Scrin owners are excluded.

## Validation and remaining live observation

The preceding occupancy batch's normal Release build and
`tools/test-game.cmd --check-only` passed and updated `engine/bin`. The subsequent
placement/hover batch initially needed isolation due to DLL locks, but its final normal
build also succeeded after the game exited; see the newer follow-up for current checks. The test startup loaded rules/sprites and prepared
only the disposable test map, without starting or terminating a game.

Runtime checks: `--check-selection-lifecycle`, `--check-faction-mcvs`,
`--check-bib-occupancy`, `--check-building-presentation`, `--check-chinese-gatling-art`,
`--check-nod-war-factory`. Logs: `.art-validation/occupancy-completion/`.

Native composites are under `ground-review/after/`. They are not a live match replay.
Live firing, driving, production, selling and transport loss remain observations to
make after restarting; do not describe compile/rules/path checks as those observations.
No user game was stopped; no source PNG was rewritten; no commit/push/release/deploy.
