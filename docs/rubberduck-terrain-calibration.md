# Rubberduck cliff and shoreline calibration

## Status

New Mountain Valleys exports now use height-4 cliff faces. Other presets still use
flat Cliff placeholders, and production shores remain unchanged. The normalized
rectangle/notch fixtures remain separate from this integration. Package reload alone
is never sufficient: rendering, actor initialization and movement must also be checked.

The newly supplied original sheets and both example scenes are audited separately:
see [Original Rubberduck sources](rubberduck-original-sources.md). That audit identifies
native plateau fill, multi-slot cliff corners and the correct 6x6 water-patch geometry.

The original four-orientation fixture is documented in
[Walkable plateau and ramps](rubberduck-plateau-ramps.md). Production now also has filled
starting plateaus and real ascents: see [current implementation and runtime evidence](mountain-valleys-start-plateaus.md).

## Reproduce

From the Game directory, after building the CA assembly:

```cmd
utility.cmd ca --rubberduck-terrain-lab OUTPUT
```

The command generates checkerboard atlases using **OpenRA's PngSheetLoader**, an
alpha-bounds report for every frame (`frames.tsv`), three raw `.oramap` fixtures
with plateau heights 2, 4, and 6, and two normalized height-4 fixtures (rectangle
and south-facing notch). Derived PNGs are embedded in those maps, not installed
as replacements for global assets. It needs no Pillow installation. It does not
rewrite source PNGs, their metadata, or the tilesets.

Copy a fixture to `mods/ca/maps` and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/capture-map-screenshot.ps1 `
  -MapPackage rubberduck-calibration-height-4-normalized.oramap `
  -Destination OUTPUT/height-4-ingame.png -StartupDelaySec 85 -CaptureTimeoutSec 15 `
  -AutomaticFramebuffer
```

The fixture fixes spawn assignments, disables fog/shroud through map-local lobby
options, and adds a calibration-only camera trait. That trait requests a framebuffer
screenshot after 75 simulation ticks, so no desktop focus is needed for these maps. The screenshot launcher disables
edge scrolling: leaving the cursor at (0,0) otherwise scrolls away from the plateau
while waiting. Capture now waits for a real Ctrl+P framebuffer file, including on
installations with no existing Screenshots directory. Failed focus/capture must
fail the command, not produce a misleading window image.

## Measured asset facts

- Tile size: 128x64. A height step projects vertically by 32 pixels.
- High cliff frames: 128x192 with embedded Offset=0,0.
- Ground frames: 128x64. Transparent cliff atomics replacing these frames remove
  the ground under their transparent pixels; they are not complete ground tiles.
- SW first-frame bounds: (15,0)..(125,190).
- SE first-frame bounds: (2,0)..(112,188).
- NE rear-edge first-frame bounds: (55,116)..(127,155).
- NW rear-edge first-frame bounds: (0,116)..(72,155).
- North outer-corner first-frame bounds: (55,18)..(127,53). Its neighboring
  decoded frame contains the other segment and the third frame is empty. Treat
  this sheet as needing reconstruction/verification, not three interchangeable
  corner variants.
- `grass_cliff_trans`, `sand_cliff_trans`, and `dirt_cliff_trans` are transparent
  edge overlays. They must retain an underlying surface when composed.
- `rock_cliffs` decodes into 384 frames of 128x64, slicing the visibly taller
  rock objects into strips. Frame zero contains only a few bottom-row pixels.
- `water_v01`/`water_v02` are large diamond textures decoded into 36 rectangles;
  frame zero is empty. Do not interpret all 36 as complete water diamonds.
- Previously referenced `grass_shore` and `sand_over_water_shore` PNGs are absent.
  The historical generator also references additional missing derived assets;
  running it wholesale is not a safe restoration procedure.

## Renderer observations

Framebuffer captures `height-4-no-scroll.png` and `height-2-no-scroll.png` from the
calibration run show:

- Front wall chains are continuous when stepped along CPos axes.
- A black band remains at the lower edge; changing height changes the band but
  does not supply missing ground coverage.
- Rear strips and north/side corner pieces do not share a common anchor.
- The raw-water/sand control strip deliberately remains hard-edged.

These are **baseline failures**, not approved cliff or shore rendering. Screenshots
are generated into OUTPUT; successful map save/reload is not visual acceptance.

## Normalized prototype observations

`RubberduckCliffCalibrationSprites` generates a 256x320 canvas with an explicit
(0,128) sprite offset. The top of that canvas corresponds to the upper diamond's
top, 32 pixels above its center. The north corner retains both source-frame
segments; NE/NW strips are shifted by their measured 116-pixel inset.

The first combined grass-and-rock prototype failed: the backing on a later actor
painted grass over an earlier wall, producing green triangular cuts. This was
rejected after a real framebuffer capture. The current version separates substrate
and rock into independent sprites and draws substrate behind the rock layer.
West/east end cells include their adjoining straight face as well as the cap.

Reviewed screenshots from the current run:

- `normalized-split-layers.png`: continuous front chains, closed north corner,
  attached western end, no black foot band in the visible test area.
- `normalized-notch.png`: the south-facing concave corner connects both front
  chains without the former projection holes.

Both were produced by the running game at simulation time 00:03, not by an
offline rasterizer. The eastern end is partly hidden by the sidebar, so this is
not an all-orientations acceptance test. The rear rim still needs art blending.

The normalized map also contains a sand-on-water mask probe made from existing
sand atomic overlays and a complete water marker tile. This is not a restored
Rubberduck surf/foam set and is not a general shoreline picker yet.

## Connected shoreline prototype

`RubberduckShoreCalibration.cs` adds five separate flat-height fixtures: island,
bay, lake, saddle, and saddle-east. These now use a shared vertex land field,
not independently guessed atomic-tile names. Sixteen N/E/S/W corner masks form
continuous sand/water contours, and a second sixteen-mask set blends an eroded
grass interior into the sand beach. The original sand, grass and complete water
marker textures are sampled without changing the source PNGs. A narrow darker
sand band represents damp ground; there is no surf animation or restored foam art.

All compatible mask pairs are checked along their shared edges. Field values and
90-degree rotational equivalence are checked for all sixteen masks. Ambiguous
opposite-corner masks keep a water-valued center, so diagonal land pieces do not
create a visual bridge. Eight-neighbor full-land connectivity must contain one
component for island/bay/lake and two for both saddle fixtures. Export reload
checks every terrain cell and the zero heightmap. Reports are written alongside
the maps as `shore-SHAPE-masks.tsv` and `shore-SHAPE-validation.txt`.

Real game framebuffer captures at simulation time 00:03 were reviewed:

- `shore-island-grass-ingame.png`: all four exterior edges and rounded corners;
  continuous grass/sand/water composition.
- `shore-bay-grass-ingame.png`: connected concave shoreline and inland grass band.
- `shore-lake-grass-ingame.png`: inverse coast with grass outside the beach.
- `shore-saddle-grass-ingame.png` and `shore-saddle-east-grass-ingame.png`:
  water gaps remain visible between diagonal land pieces in both orientations.

These screenshots are in `%TEMP%/ymca-terrain-lab`. Reproduction after generating
and copying the fixtures into `mods/ca/maps`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/capture-map-screenshot.ps1 `
  -MapPackage rubberduck-calibration-shore-bay.oramap `
  -Destination OUTPUT/shore-bay-grass-ingame.png `
  -StartupDelaySec 100 -CaptureTimeoutSec 30 -AutomaticFramebuffer
```

A shorter 65-second startup timed out on one run; no stale screenshot was accepted.
The 100-second rerun succeeded. Shore sprites are map-local, non-occupying actors
with a ground-level draw order; they are not yet production terrain templates.
Partial coastal cells remain Water and only full sand/grass cells are walkable.
This deliberately conservative boundary needs real foot/tracked/ship movement
and selection/shroud tests. Graph connectivity alone does not certify locomotors.
The current smooth contours also still need review on irregular natural coastlines.

## Mountain Valleys exporter integration

`RubberduckMountainRenderer` is now called by the production exporter for Mountain
Valleys. The planned mountain cells retain terrain template 3992 (impassable Cliff),
but acquire height 4 and global, non-occupying terrain decoration actors. No local
camera, probe or other custom map rules are added to normal exports. Existing maps
must be regenerated to gain the elevation and actors.

Directly applying the lab's original corner picker to irregular mountain rings failed:
it produced detached pillars and offset rims. The integrated set instead fits the
existing SW/SE rock artwork to exact shared cell-edge quads. The source's horizontal
lean is undone with an affine sampling transform; transparent silhouette margins are
extended from opaque pixels. This closes narrow ridges without changing walkability.

The derived file is `bits/terrain/rubberduck/derived/anchored-cliffs.png`. It is generated
as `terrain-cliff-faces.png` by the terrain-lab utility. Original PNGs are unchanged.
There are 16 exposed-edge masks, a low-priority substrate and a separate upper-ground
frame with normal depth ordering. The last is necessary to occlude units behind a
plateau instead of displaying them on its top. All actor behavior is shared through
`^RubberduckTerrainDecoration`; rules/sequences are global and reused across maps.

Reviewed real-game captures in `%TEMP%/ymca-mountain-integration`:

- `ingame-1.png`: rejected original corner-picker integration.
- `ingame-2.png`: rejected per-column rock stretching, before affine correction.
- `ingame-final-center.png`: final derived faces on actual seed-42 terrain.
- `ingame-movement-depth.png`: normal fog/shroud with corrected upper-ground ordering.

A diagnostic copy of the exported map added only camera/probe rules. Light Infantry
and Heavy Tank moved along 12-cell graph routes, arrived at ticks 201/194 and rejected
the blocked cliff target 34,6. Neither entered a Cliff cell. Evidence is recorded in
`movement-results.txt`. One screenshot-helper invocation timed out after the game had
already written its framebuffer captures; the timestamped engine screenshot was
reviewed and copied explicitly rather than treating the helper as successful.

15 seed-42 exports (Tactical/Operational/Strategic x 2/4/8/12/16 players) passed planner
and roundtrip checks. An independent binary-layer audit confirmed height 4 exactly on
Cliff cells and height 0 elsewhere, with no diagnostic actors/rules in the exports.
All 15 maps contained zero actual Dirt (1030) cells: the reported brown Mountain
Valleys surfaces were the dirt-artwork Cliff placeholder, not a missing dirt autotransition.

Remaining limitations: the integrated faces are angular and repetitive, not final
natural-rock artwork; large-map runtime performance still needs profiling (about
16,000 decoration actors at 16 players). Additional seeds, selection tests and other
presets remain to be reviewed. Shore integration is not included in this change.

## Next acceptance gates

1. Normalize derived sprites to explicit upper/lower anchors, preserving originals.
2. Reconstruct and verify complete corner frames before selecting variants.
3. Provide ground coverage without repeating grass down a vertical rock face.
4. Validate straight edges, convex/concave corners, and all four orientations.
5. Validate grass/sand/water composition independently of tall cliffs.
6. Check plateau height, projected shroud coverage, selection, and foot/tracked
   paths in the exported map before enabling the production picker.
