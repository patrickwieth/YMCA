# Walkable plateau and real ramp calibration

## Ramp matte and shaded-roof correction

Ramp artwork no longer resamples the flat source diamond's alpha matte into the
projected slope. Pixel centers inside the slope are opaque; outside pixels remain
transparent. Fringe RGB comes from a nearby opaque source sample (u/v clamped to
0.06–0.94), avoiding a solidified matte-colored border. This changes only the eight
small derived ramp PNGs, not height, RampType, resources or collision.

The shared `terrain.rubberduck.cliff17` roof now selects a matching shaded grass
image on the shaded tileset instead of always drawing normal grass. Two extracted
128x256 roof PNGs replace its reference to frame 17 of the historical atlas. The
normal roof is checked RGBA-for-RGBA against that old frame; placement and ground
Z ordering stay identical. The historical atlas itself is not regenerated.

Provenance: `mods/ca/bits/terrain/rubberduck/derived/plateau-ramp-provenance.txt`
records source-frame hashes, sampling and extraction. Eight decoded ramp checks
verify exact RGBA roundtrip, opaque projected areas (2048 pixels for directions 1/2,
6144 for 3/4), and no partial-alpha pixels. The roof extraction also checks the old
normal frame explicitly.

Latest evidence: `%TEMP%/ymca-ramp-fringe/final-normal` and `final-shaded`:
- **16 actual movement legs and eight steep-edge checks per tileset**, infantry and
  tanks, all four ramp orientations, ascent/descent through all intermediate levels.
- Both screenshots inspected; isolated probe paths are recorded in
  `runtime-results-path.txt`. Earlier `normal`/`shaded` runs predate the roof fix.
- `ingame.png` at the root is an additional review of the earlier editor-ramp save
  with corrected slope art, not a new editor transaction certification.

The laboratory still uses old calibration wall textures and substrate actors; these
screenshots are not a full native plateau-art approval. The rear retaining rims have since received a bounded native-texture correction;
see [rear-rim evidence](rubberduck-native-cliff-faces.md). Repetitive fallback walls
and broader roof/foot joins remain separate visual work. Short retaining faces now
also retain the full-height wall's texture scale rather than shrinking/changing
material beside the ramp; the same linked document records that later correction.
No bridges or original-source images were changed.

Reproduce with `--rubberduck-plateau-lab OUTPUT` or
`--rubberduck-plateau-lab OUTPUT shaded`. Install only `plateau-ramp*.png`,
`plateau-roof*.png` and their provenance when updating this art; do not replace the
historical face atlas with the laboratory's wall output.

## Original calibration milestone

Production now includes filled starting plateaus and mountain islands. See
[Mountain Valleys starting plateaus](mountain-valleys-start-plateaus.md) for the current
implementation and evidence. The remainder of this document records the earlier fixture.

A filled height-4 plateau with four three-cell-wide ascents now works in an isolated
runtime fixture. These are real engine ramps, not flat gaps dressed as ramps.
At this milestone, MountainValleysGenerator had not yet been switched to this topology.

Implementation:

- `MapGeneration/PlateauSurface.cs`: immutable height/ramp/collision values, common-edge
  corner matching and the symmetric calibration layout. No artwork dependencies; intended
  to be reused by the generator and editor. Editor integration was absent at this milestone;
  [atomic shape/join brushes](editor-plateau-shaping.md) are now available.
- `UtilityCommands/RubberduckPlateauLabCommand.cs`: reproducible slope artwork, exact
  side-wall profiles and a reload-checked test map.
- `Traits/PlateauMovementProbe.cs`: real pathfinder, Mobile movement and elevation assertions.
- Templates **14001–14004** in both Rubberduck terrain definitions, with RampType 1–4.
  Their category is explicitly experimental. Grass 1000 remains the first/default template.
- Eight derived PNGs: normal and shaded grass slopes. Original assets are untouched.

## Geometry and collision

Each ascent has four ramp cells with base heights 0, 1, 2, 3, ending at flat height 4.
The engine's half-height ramp corner is 724 world units, exactly one terrain height step.
The corner values are checked against `MapGrid.Ramps`, and consecutive shared edges must
match at **both** endpoints, including entry and exit onto flat ground.

Ramp types increase along CPos +X, +Y, -X, -Y respectively. The images are affine
projections of the grass texture onto those slopes. Their embedded PNG metadata is
128x128, Offset 0,-32; they are terrain tiles, not slope-shaped decoration actors.

High flat ground is Clear and walkable. Most steep boundaries are between two Clear cells
with a four-level discontinuity: the real locomotor rejects crossing them. Narrow explicit
Cliff guards flank the ascents, because the engine otherwise permits one-level side steps.
Those guards are retaining-wall collision strips, not a blocked plateau interior.

This fixture proves access to a filled plateau. It does not yet solve arbitrary irregular
plateau generation, symmetrical ascent allocation, resources, or the distinction between
existing home-valley passages and mountain-island ascents in the production planner.

## Rendering corrections discovered by runtime tests

1. Drawing rear-facing vertical walls through the top created false holes. These polygons
   are now culled in the fixture; only the upper rear rock rim remains visible.
2. Upper grass was sorted as a vertical billboard. Voxel tanks were partly buried and
   sprite infantry could disappear entirely while their physical height was correct.
   The shared upper-ground sequence now uses **ZRamp 1 and ZOffset -1024**: ground is drawn
   before units within its cell, rather than through them.
3. Calibration camera settings now support a screenshot tick and a diagnostic minimum-zoom
   override. The final map uses a larger backing field and an offset camera so the sidebar
   does not hide the fourth entrance.

The final screenshot shows all four tanks and four infantry standing on the high central
surface simultaneously. Wall texture remains repetitive calibration artwork; the native
open-cliff pieces and plateau-fill material are not fully composed into production terrain.
Complete behind-plateau occlusion, shroud and selection coverage still need broader tests.

## Actual validation

`%TEMP%/ymca-plateau-lab/movement-results.txt` records:

- **16/16 movement legs passed:** Light Infantry and visible Heavy Tank, all four ramp
  orientations, ascending and descending.
- Every leg physically visited all four ramp base levels and an intermediate world height.
- Settled endpoints were checked at Z=2896 on the plateau and Z=0 in the valley, with feet
  on the actual terrain surface.
- **8 paired steep-edge checks passed:** the two unit types reject direct crossing in
  both directions, for all four boundary orientations, even though both endpoints are Clear.
- No probe crossed a Cliff guard or jumped between cells with a height difference above one.
- Tanks reached the plateau at ticks 131/136, infantry at 142. After a 100-tick pause for the
  occupancy screenshot, descents completed at ticks 360/365 and 374 respectively.

Evidence:

- `plateau-unit-sorting-fixed.png`: final complete plateau and all eight units on top;
- `plateau-ground-depth-fixed.png`: intermediate version still hiding sprite infantry;
- `plateau-top-occupancy.png`: earlier camera/occlusion defects, not accepted final evidence;
- `mountain-regression.png`: archived Mountain Valleys review re-rendered with the shared
  upper-ground sequence correction. Rendering only: that archived package emitted no
  movement-probe results during this run.

The CA Release build and `git diff --check` passed. Existing Mountain Valleys seed-42 exports
for 2/4/8 players still passed planner/gameplay structure checks and package reload. Those
exports retain the old blocked mountain topology and are not evidence of production ramps.

## Reproduce

Build CA, then from the engine utility environment:

```
OpenRA.Utility ca --rubberduck-plateau-lab OUTPUT
```

Copy `OUTPUT/plateau-ramp*.png` to `mods/ca/bits/terrain/rubberduck/derived/` when rebuilding
artwork. Temporarily install `OUTPUT/rubberduck-walkable-plateau.oramap` for the screenshot
helper, with automatic framebuffer capture and a 150-second startup allowance.

The utility checks both terrain definitions, default terrain, engine corner values and
map reload including tile type/index, heights and RampType. At that earlier milestone runtime tests were performed
on the normal tileset only. Both tilesets now have the targeted runtime evidence above.
Diagnostic maps are removed from the ordinary lobby map list after testing.
