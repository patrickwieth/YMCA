# China / Scrin upper building-test displays

Local Game/modular change, requested by the user. `test-gebaeude.cmd` now prepares
two additional blocks above the existing four factions: **China upper-left,
Scrin upper-right**. The current `mods/ca/maps/testmap1-debug.oramap` was regenerated.
Original `testmap1.oramap` and all existing actor coordinates remain unchanged.
No running game was stopped; restart the building test to see the additions.

## Current supplied-art follow-up (Game/isometric)

The display actors now also use the supplied China PNGs: both reactors, barracks,
Command Center/radar upgrade, supply depot, War Factory, airfield, propaganda and
internet centers, Nuke Silo, Naval Yard and Speaker Tower (including PDL). CHSYRD now
uses its own supplied naval yard instead of SPEN's sprite; the Soviet yard is unchanged.
Chinese land buildings use Soviet decorative bibs except the ordinary/fortified
bunkers and their variants, whose supplied PNG foundations now stand alone.
The naval yard remains without a land bib. Scrin buildings/defenses now use the
native Scrin1x1/2x2/3x3 plating; the displayed Biometallic Wall has no bib.
Scrin Repair Bay, Storm Column and Rift
Generator join the earlier supplied Scrin buildings and native Shard/Plasma turrets.
Gameplay footprints, production, repairs and docking remain unchanged. The existing
slots need no map edits. See [new-faction-buildings](../../art-source/new-faction-buildings/README.md).
Engine map/sprite checks pass; live appearance/drive-out/landing still need review.

## Startup checker fix after bunker bib removal

`CheckExtraBuildingTestCommand` previously used `Single(...)` to require a Soviet bib
on every Chinese land actor, causing `Sequence contains no matching element` after
the user requested bib-free bunkers. It now explicitly expects no bib for bunkers,
the naval yard and Scrin wall; all other display buildings must have exactly one
bib of their faction's expected type. Scrin bib sprites are now loaded too. Missing,
duplicate or wrong-faction bibs produce actor-specific diagnostic errors.

The actual `tools/test-game.cmd --check-only` path passes through build, selection,
map preparation, water and sprite/footprint checks. The game was not started; the
authoritative map hash stayed unchanged. `test_extra_faction_buildings.py` passes,
including deliberately missing Barracks and unwanted Bunker bib regressions.

## Historical follow-up: doubled art, Soviet bibs and shared Naval Yard

China/Scrin building art is now2× its former size, including body detail, turrets
and registered attachments; unchanged gameplay footprints/production. Chinese land
buildings use Soviet decorative bibs. CHSYRD shares SPEN's exact `spen.soviet`
image and native size (not2× the new large PNG). Both Chinese and Soviet Naval Yards
now receive water pools; the test has five prepared yards total. Checks include
actual shared-image binding and water terrain. See
[`art-source/china-scrin-scale2x/README.md`](../../art-source/china-scrin-scale2x/README.md)
for frozen baselines, merge compatibility, isolation and validation details.
Live appearance review remains pending; restart the normal building test.

## Layout / inventory

`tools/prepare_extra_faction_buildings.py` inserts stable `TestChinaNN` and
`TestScrinNN` actor IDs, owned by Multi0 and supplied with explicit faction
initializers (China tank/infantry/atomic variants, Scrin Reaper). This avoids the
mixed test player's Nod faction silently selecting the wrong visuals.

- China origin CPos36,−12; Scrin origin90,−66.
- Five columns; column stride6,−6; row stride5,5.
- All new origins have screen-row coordinate x+y24…54, above the original top
  building row76. Column ranges in map coordinates: China24…48, Scrin78…102.
- All map bounds, occupied cells and terrain validity checked with the engine.

China (18): Command Center, both power plants, Barracks, Supply Center, War Factory,
Propaganda Center, Internet Center, Airfield, Naval Yard, Gatling Cannon, ordinary
and fortified Bunker, both advanced-infantry Bunker variants, Speaker Tower and
PDL variant, Nuclear Missile Silo.

Scrin (17): Construction Yard, both reactors, Refinery, Portal, Warp Sphere, Nerve
Center, Technology Assembler, Gravity Stabilizer, Signal Transmitter, Repair
Platform, Manipulator, Rift Generator, Plasma Turret, Shard Launcher, Storm Column,
Biometallic Wall.

Coverage is tested against the actors declared in both faction structures files.
Excluded intentionally: non-standalone radar plug, AI bunker duplicate, invisible
airfield helper, nonbuildable Nukular duplicate yards, temporary storm/column
actors and the previously disabled resource-storage silo. No global gameplay,
production prerequisites or faction balance was changed.

## Water / map-only compatibility

The existing water-preparation command now includes CHSYRD, adding a5×5 water pool
with its exit border. Existing Allied/GDI/Nod water pools remain. The Scrin wall
needs two **disposable-map-only** fixes: omit its `AppearsOnMapPreview` signature
because Rubberduck lacks the `Wall` terrain color; add `Interactable` to satisfy its
Tooltip mouse-bounds dependency. Wall rendering/gameplay and global rules remain.
These issues were caught during engine validation before handing off the map.

## Validation / reproduction

`tools/test-game.cmd` runs the new map validation after preparing water and before
launch. It still builds the current modular worktree first. `--check-only` does not
start a game (and removes its disposable map as before).

- Release configuration build passed.
- `--check-extra-building-test PATH`: loads the real map, resolves all35 new actor
  dependency orders and idle/damaged/turret sprites, checks map bounds, terrain,
  and footprint overlaps against existing buildings.
- `tools/test_extra_faction_buildings.py`: complete declared-building coverage with
  explicit exclusions, deterministic/idempotent placement, original actor/source
  preservation, and actual engine map/water/sprite validation.
- Existing building-test faction and Nod SAM regressions pass.

This is engine/load validation, not a live match screenshot or firing review.
No commit, push, release or deployment.
