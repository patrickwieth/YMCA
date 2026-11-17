# Rubberduck material-edge blending (first visual pass)

`RubberduckMaterialTransitions` is a mod-side terrain overlay shared by gameplay and
editor worlds. It is enabled only for Rubberduck tilesets. It changes no terrain tiles,
height, resources, actor definitions or movement costs; old map packages benefit without
being rewritten. Terrain and height events refresh the edited cell and its neighbors,
including when Undo/Redo restores them.

## What it currently blends

- Grass into adjacent sand or ordinary dirt (templates 1020/1030).
- Sand into ordinary dirt, including grass/sand/dirt junctions.
- Water into ordinary grass/sand/dirt banks: narrower blend inside the land diamond.
- Blocked dirt-looking cliff template 3992 is **not** treated as ordinary dirt;
  these overlays must not hide impassable cliff feet.
- Matching flat heights only. Ramps and height discontinuities are excluded.
- Rock banks do not receive water blending: their native face meets the bank instead
  of adding a blue gutter behind the rocks. Grass/sand material joins still work there.
- Normal and shaded grass and sand sources match their tileset. The existing water marker source
  is common to both, as in the current tileset definitions.

The mask uses CPos-aligned edge distances, not screen-axis rectangular gradients. It is
rotation symmetric, restricted to the source diamond including its antialiased pixel
fringe, and zero at every cell center. No water
cell receives a land overlay, so this pass does not invent decorative land bridges or
change collision to hide an artwork defect. Multiple edge masks are composited before
upload, avoiding stacked translucent strips at corners. Cached RGBA sprites and their
terrain layer are released with the world. Other tilesets allocate no overlay layer.

## Provenance

Pixels are sampled from the existing derived `grass_a_base.png` (or shaded counterpart)
plus `sand_base.png` (or shaded counterpart) and `water_marker_v01.png`,
first 128x64 frame, using `PngSheetLoader`. Alpha and mixed RGB
are generated in memory by smoothstep masks. Partially transparent source-edge RGB may
contain a light matte: those pixels use nearby opaque texture sampled with u/v clamped
to 0.06–0.94. Keeping the original fringe coverage but replacing its matte RGB removes
the one-pixel bright seams found during screenshot review. Overlay sprites use the same
Z-ramp as flat terrain. This is **derived material blending**, not
native shoreline artwork or animated/phased-water integration. No original PNG or
historical generated sheet is rewritten; no new PNG assets are installed.

## Cliff-foot fringe update

A separate, narrowly bounded [soil fringe](rubberduck-cliff-foot-fringe.md) now
extends from low blocked 3992 feet onto neighboring ordinary low grass. This does
not classify 3992 as ordinary dirt or paint over its blocked cells. Foreground roofs
conservatively suppress the projected fringe, and editor edits refresh occlusion.
The additional 15 sprites retain the four-page combined atlas bound.

## Native spatial water update

The current default uses [native spatial water](rubberduck-native-water.md), with
624 ground + 576 water sprites rather than the earlier combined 1295-key cache.
The measurements and marker-water descriptions below document the earlier stage
and the retained `NativeWater: false` fallback. Terrain and gameplay stay unchanged.

## Authored base variants and bounded cache

The overlay also recognizes Grass B (1010) and Dirt B (1040). Source precedence is
water, Grass A, Grass B, sand, Dirt A, then the destination Dirt B. Thus each of the
five ordinary land bases can meet the others without borrowing the wrong grass
variant. Normal/shaded source frames are selected consistently. Composite sprites
still use one terrain layer; collision templates, water cells, slopes and unequal
heights retain their exclusions.

Five four-bit masks fit in the cache key. Each neighbor contributes at most one
source or none, giving at most 6^4 = 1296 keys including the empty key. The runtime
asserts the nonempty limit of 1295. This replaces the earlier 256-key bound below.

Evidence at `%TEMP%/ymca-material-variants`:
- `materials` and `materials-shaded`: inspected five-base galleries, 18 cargo/coast
  assertions each. No PNG/source assets were modified.
- `editor-normal`, `editor-shaded`, `editor-materials-large`: 93 editor assertions
  each, including all 144 ordered material-pair/direction cases, 1296 coverage
  combinations, exclusions, Undo/Redo and exact save/reload.
- The 192x192 editor fixture (`editor-materials-large`) precached every valid key:
  **1295 sprites, three 2048x2048 atlas pages (48 MiB capacity)**. Recorded precache:
  298 ms and 40,451,344 thread-allocated bytes; full 36,864-cell refresh: 13 ms.
  The shaded 112x128 fixture recorded 556 ms precache and 10 ms full refresh.
  These are single-machine CPU measurements with a partially warm cache, not GPU
  residency/FPS, long-match memory, or dense-combat performance certification.
- Precache/refresh does not change terrain or actor data. Atlas pages/caches/events
  are released with the world. Large-map shroud and all-tileset performance remain
  broader acceptance gates.

## Earlier ordinary-dirt extension

`%TEMP%/ymca-material-ground/materials` and `materials-shaded` contain inspected
runtime galleries with grass/sand/dirt patches and a small ordinary lake, plus
18 coast/cargo assertions each. The cargo assertions exercise the outer coastline,
not swimming or transport navigation inside the small lake.

`editor-corners` and `editor-corners-shaded` additionally check all four rotations of dirt junctions,
height-change invalidation and exclusion of water, ramps and blocked cliff tiles,
then restores the exact previous terrain. Combined with existing coast operations,
Undo/Redo and save/reload this gives 91 editor assertions each. The final normal
run also executes the expanded pure mask checks, which cover
all 256 valid three-material neighborhood combinations and bounded compositing
weights. Water takes priority over grass, then sand; masks never paint land onto
water or across a height discontinuity. No movement rules or assets change.

Three masks share a cache key. Each direction contributes at most one of water,
grass, sand or no overlay: at most 256 combinations, not 4096 independent masks.
This first extension covered canonical 1000/1020/1030/1050 materials only; the
subsequent base-variant support above extends it to 1010 and 1040. Other authored
shore/transition template families are not automatically reinterpreted.

## Earlier evidence

Under `%TEMP%/ymca-rock-coast`:

- `material-editor-corners` and `material-editor-corners-shaded`: 93 real-editor checks
  each, including pure mask rotation/range/center invariants, live overlay invalidation
  on terrain edit/restoration, and the existing coast shape/Undo/Redo/save-reload tests.
- `material-play-editor-corners` and `material-play-editor-corners-shaded`: actual saved
  editor maps, with joined-gap rock rejection, beach cargo unloading, inland movement,
  and reboarding. Four runtime assertions each. These were rerun after edge correction;
  final reviewed images are `material-matte-final.png` and
  `material-matte-shaded-final.png`. Earlier `material-edge-final`,
  `material-depth-final.png` and `material-fringe-final.png` retain intermediate
  seam/matte experiments, not the accepted visual result.
- `materials`: earlier 17-assertion four-direction gallery run; this predates the final
  wider grass blend and exclusion of water blending on blocked rock banks. It is not
  evidence for final pixels.

The new `editor-corners-shaded` coast fixture mode supports repeating the shaded checks.
Build success is separate from these actual editor and cargo checks.

## Not yet solved

This pass softens hard material boundaries. It does **not** smooth the underlying
rasterized coastline, replace water marker artwork, repair every native rock joint,
texture all cliff roofs, or certify shroud, all factions and large-map performance.
A separate [conservative geometry pass](rubberduck-coast-smoothing.md) now reduces
eligible corners in new generated maps; protected or unsupported contours remain.
Do not describe this as all transitions
finished or long-match balance certified. Bridges remain deferred.
