# Native cliff pieces in the Mountain Valleys generator

## Current integration

The generator now places complete source-based wall, convex-corner, concave-corner and
end pieces on supported plateau fronts. This is no longer limited to the connection lab.
The original shapes retain lateral spread; their vertical drop is fitted to height four.
They are therefore **height-fitted derivatives**, not unmodified source rectangles.

`RubberduckNativeCliffPlanner` runs after the starting/island plateau topology is created
and **before** resource, economy and Strategic checkpoint placement. It reserves low,
blocked footing cells using the same rule on all four compass sides. Existing plateau
surfaces, ramps and their protected approaches are not overwritten. Spacious low-ground
checks constrain reservations; an exact before/after flood-fill rejects an apron if any
previously reachable, non-apron cell becomes disconnected.

Front edges are grouped by shared roof vertices. Straight joins retain full faces;
convex joins use slot 24; concave joins replace the two overlapping central faces with
slot 16. Slots 26/30 close rear ends. Unsafe endpoints are trimmed before grouping so a
single protected ramp mouth does not discard a whole usable front. Unsupported contours
continue to use the derived edge renderer rather than forcing native art over paths.

Planning and export are separate: `MapPlan.NativeCliffFaces` records which derived faces
to suppress, and `NativeCliffPieces` records each source slot and anchor. Debug output adds
`native-cliff-pieces.tsv`; the existing surface TSV includes low blocked footing cells.

The 4-player seed-42 plan uses **228 of 335 plateau front faces**, with 361 native pieces
and 506 reserved low-foot cells. This is partial contour coverage, not a finished all-native
terrain renderer. Expansion-valley rings still use the older geometry/derived faces.

## Rendering

The shared files are registered in `mod.yaml`:

- `rules/rubberduck-native-cliffs.yaml`
- `sequences/rubberduck-native-cliffs.yaml`
- `bits/terrain/rubberduck/derived/plateau-native-{0,1,2,5,6,7,16,24,26,30}.png`

`NativeCliffBody` clips source pixels against nearer solid terrain columns, using depth
that varies down the leaning rock face. This prevents native caps from appearing through
foreground plateau roofs. Equal neighboring column spans are merged into cached sprite
slices instead of drawing every column separately. No fetched-engine changes are needed.

Rebuild the fitted assets with `--rubberduck-native-plateau-lab SOURCE OUTPUT`, then copy
only the ten `plateau-native-*.png` files to the derived asset directory. Originals and
`ruin` remain untouched. The native connection lab still supplies the unwarped reference.

## Evidence

`%TEMP%/ymca-native-production` contains the current evidence:

- `matrix/`: **54/54 generation, gameplay-validation and export/reload cases**, across
  Tactical/Operational/Strategic, players 2–12/16, seeds 42/43/44. Every export contains
  native pieces (50–981 per map). These are not 54 runtime matches.
- `clipped4/`: Tactical 4P42, 32 movement legs, 8 low-foot checks, 8 paired clear-edge checks.
- `operational8/`: Operational 8P43, 32 legs, 8 low-foot checks, 8 paired clear-edge checks.
- `strategic3/`: Strategic 3P42, 12 legs, 3 low-foot checks, 3 paired clear-edge checks.
- All three runs also passed starting MCV placement and actual construction-yard deployment.
- `followers8/`: 16 real ArmyFollower observers over 1500 ticks, fog enabled, plus MCV/deployment.
- `finite4/`: complete starting patches, finite resources, no generators, five tech buildings.
- `none8/`: no resource fields, generators or tech buildings.

Screenshots from the runtime runs were opened and reviewed. `runtime-results.txt` archives
each safe diagnostic log. Earlier `first4`/`second4`/`trimmed4` images are development
comparisons, not the final visual evidence. The initial untrimmed import had low coverage;
the initial uncropped renderer visibly leaked rock posts through foreground roofs.

The native matrix now deliberately re-exports native plans even under `reuse`: equality
of `map.bin` alone would not certify equality of the new actor-placement graph.

## Remaining work

- Native coverage is incomplete, especially rear/side transitions, protected ramp mouths
  and expansion rings. Angular outlines, exposed square footing patches and mixed-material
  joins still need visual refinement.
- Connectivity and equal complete starting fields are checked; broader expansion-distance
  and resource fairness after adding footings is not comprehensively certified.
- Full behind-plateau unit occlusion and shaded artwork need broader review.
- Sprite-slice/GPU cost and long-match performance need profiling on large maps.
- An [atomic editor stamp tool](editor-plateau-stamps.md) now supports isolated plateaus,
  removal and Undo/Redo. Both wall renderers now have editor previews and terrain-revision
  cache invalidation. A separate [lowering tool](editor-plateau-removal.md) can remove
  supported isolated empty generated plateaus with their native pieces and feet in one
  Undo step. The [shape brushes](editor-plateau-shaping.md) now support growth, trimming
  and joining with shared native contour replanning. [Ramp relocation](editor-ramp-relocation.md)
  now has its own atomic tool. Arbitrary elevations, full artwork coverage and large-map
  editor profiling remain open.

The installed ordinary 4-player seed-42 package is updated separately from diagnostics.
It contains no movement probes, diagnostic player locks or map-local calibration rules.
