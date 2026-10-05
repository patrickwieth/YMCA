# Mountain Valleys: filled starting plateaus

## Current scope

Player homes are now filled, traversable height-4 terrain, not low interiors surrounded
by blocked raised rings. The same treatment applies to vacant home slots. Closed mountain
islands retain the filled plateau implementation; expansion valleys remain low and still
use the older ring layout. Their wall rendering is now shared with the plateaus; see
[original-source wall faces](rubberduck-native-cliff-faces.md). Supported plateau fronts now
use [complete native pieces with planned footing and roof clipping](rubberduck-native-cliff-production.md).
This is not a claim that all terrain artwork or the editor is finished. The first
[editor stamp workflow with atomic Undo/Redo](editor-plateau-stamps.md) and
[shape/join brushes with native replanning](editor-plateau-shaping.md) are now available,
with protected resources, actors and existing ramps.
The installed `mods/ca/maps/ymca-generated-mountain-valleys-players-4-seed-42.oramap`
has been replaced with the ordinary, probe-free export.

### Planning

- `MountainPlateauPlanner.AddHome` reserves a filled footprint and a three-cell-wide
  four-level ascent at its perimeter. A shallow lip cut accommodates curved boundaries;
  the ramp foot and approach lie outside the footprint. Retaining strips descend with
  the ramp and are blocked independently of the walkable roof.
- Ramp shared-edge corners are checked at both endpoints. Cardinal height-aware
  connectivity covers every unblocked plateau cell, all spawns and expansion valleys.
  Small isolated raster tips are treated as cliff rim, not falsely advertised as usable land.
- Each start has a clear, flat 5x5 CPos construction core. With resources enabled, two
  complete 5x5 plan-cell ore fields are guaranteed on the roof (50 cells, density 12).
  Nearest starting ore is 3 plan cells away in the checked cases. None remains empty.
- Homes leave extra projection margin at map boundaries. Subsequent terrain placement
  cannot overwrite committed surfaces or ramp approaches.
- Expansion placement first tries its old target orbit, then searches outer valley gaps
  when central packing fails. Expansion distances can consequently vary; broader balance
  review is still needed. Starting fields remain equal.
- Economy placement protects a footprint-sized area from plateaus, slopes and cliff edges.
  Dense tech placement falls back to other safe sites instead of silently producing an
  empty cluster when the first patch is unsuitable.
- Strategic routes prefer wide valley space suitable for checkpoint pads. They can leave
  elevated homes through their ramps, but checkpoint preparation cannot flatten roofs or
  approaches. Other presets keep the previous route search.

### Rendering and movement

Walkable roofs use actual terrain tiles. Duplicate grass and substrate actors were removed
from these surfaces: they obscured resource overlays with the mod's depth buffer disabled.

`PlateauFaceBody` clips the shared wall sprites against nearer solid terrain columns. It
uses a bounded atlas (100 height profiles, now with three source-art variants), grouping equal column cuts into cached
sprite slices. This removes rear-edge rock posts showing through the roof without changing
the global depth-buffer setting or fetched engine. Terrain revisions now invalidate both
wall-renderer caches during editor terrain changes and Undo/Redo.

`PlateauPathFinder` retains the earlier unsafe-diagonal correction. Ground units must not
cut across the corner of a steep cell simply because the two diagonal endpoints are low.

## Validation of this revision

Evidence is under `%TEMP%/ymca-home-plateau`:

- **54 planner/gameplay cases:** all three modes; players 2–12 and 16 at seed 42, plus
  4/8/16 players at seeds 43 and 44. These are planner checks, not 54 new runtime matches.
- Representative production export/reload checks cover Tactical 4P, Operational 8P,
  Strategic 3P and Tactical 16P, including tile/index/height/ramp round trips.
- Extra exports: Tactical patches+dense tech+finite (6 tech buildings), Strategic
  patches+dense tech+finite (5 after checkpoint preparation), and Operational None+finite.
- `checked-tactical-4`: **32/32 movement legs**, 16 paired negative steep-edge checks.
- `checked-operational-8`: **32/32 movement legs**, 16 paired negative checks.
- `checked-strategic-3`: **12/12 movement legs**, 6 paired negative checks.
- These runs use actual Light Infantry and Heavy Tank movement. Home probes walk into the
  base interior, not just onto the first high landing. Every leg visits all four ramp
  levels, settles at the expected height and rejects cliff entry / steep jumps.
- Each of those three runs verifies a real starting MCV at Z=2896 and its actual
  transformation into a construction yard at the same elevation.
- `follow8`: **16/16 real ArmyFollower observers**, 1500 ticks each, plus MCV/deployment
  checks. Fog is enabled. This observes movement safety, not long-match AI performance.
- Final screenshots are `checked-*/ingame.png` and `follow8/ingame.png`.
- `secure-smoke` repeats Tactical 4P after restricting result-file writes: 32 movement
  legs, 16 paired edge checks and MCV/deployment all pass using the dedicated log path.

Early screenshots and partial buffered debug logs are not the final movement evidence.
One earlier unpinned Strategic run failed the MCV assertion. Diagnostics now explicitly lock
Multi0 to Black Hand and spawn 1 in their map copy; command-line faction/spawn overrides
were not reliable through all lobby paths. This is not an all-factions deployment certification.

The ordinary exported gameplay package is not modified by these diagnostic player locks,
probe actors, automatic deployment or screenshot rules.

## Reproduce

After building CA, use the normal engine utility environment:

```
OpenRA.Utility ca --debug-map-generator OUTPUT preset=mountain-valleys players=4 seed=42 mode=tactical validate-only=true export-map=true
OpenRA.Utility ca --rubberduck-generated-plateau-test OUTPUT players=4 seed=42 mode=tactical
OpenRA.Utility ca --rubberduck-generated-plateau-test OUTPUT players=8 seed=43 mode=operational army-followers=true fog=true
```

The test command creates both an unchanged production export and a diagnostic copy.
Runtime evidence is appended synchronously to a hashed `.log` in
`%TEMP%/ymca-terrain-probes`; `OUTPUT/runtime-results-path.txt` identifies that file.
Map rules cannot direct these writes outside the dedicated log directory. Older archived
runs have `runtime-results.txt` in their output folder. Regenerate diagnostics after code changes.

Normal screenshots occur at tick 900, after the movement legs; observer screenshots at 1550.
Use automatic framebuffer capture with enough startup time and explicitly check the expected
PASS counts. A screenshot alone is not movement evidence. Remove diagnostic maps from the
lobby afterward.

## Still open

- Native cliff/corner composition and less repetitive, less angular contours.
- Complete low-unit/behind-plateau occlusion and broader shroud/selection coverage.
- Shaded rendering and other starting factions.
- Long matches, expansion balance and profiling of clipped decoration slices.
- Arbitrary elevations and broader editor profiling; basic shaping, joining, removal,
  [ramp relocation](editor-ramp-relocation.md), neighbour clipping updates and atomic Undo
  are implemented.
