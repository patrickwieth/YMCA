# Cliff joint review and low-foot soil fringe

Follow-up: [material occlusion](rubberduck-material-occlusion.md) replaces the
whole-cell fringe suppression described below with clipping of covered portions.
The convex-foot follow-up at the end also extends donor recognition and edit
invalidation. Earlier sections retain their historical implementation/evidence.

## Findings

Fresh production-map closeups distinguish three different features:

1. The original inward connector (grassland_1x1 slot 16) includes a brown earth cap.
   Later inspection found its missing authored grass overlay; see
   [original layer matching](rubberduck-original-layering.md). Recoloring the cap
   was the wrong approach.
2. Low template-3992 apron cells are deliberately blocked collision footprints.
   Painting them green or silently deleting them would misrepresent/change routes.
3. The exposed outer dirt-to-grass edge of those footprints can be softened without
   modifying either the blocked cell or the terrain plan.

Two cap recoloring trials were rejected after actual runtime inspection: exact-color
replacement produced speckles; continuous rock replacement looked like a dark hole.
The original installed `plateau-native-16.png` was restored byte-for-byte and its
fitting generator was restored. Neither candidate is a production improvement.

## Accepted, narrower change

`RubberduckMaterialTransitions` adds a cell-contained soil fringe **onto ordinary
low grass beside** a blocked low cliff foot. It never paints grass over the blocked
foot. Eligibility requires a height-zero 3992 donor adjacent to raised terrain;
isolated blocked dirt does not acquire the fringe.

Receivers are only 1000/1010 at height zero without ramps. Water, sand, dirt,
blocked banks/feet, raised roofs and ramps are excluded. The existing normal/shaded
Dirt A texture fades inward over 0.16 cell units, preserving the grass cell center.
This is procedural material blending, not new native rock geometry.

A first candidate exposed an occlusion issue: ground overlays can project over a
nearer raised roof because this renderer has no global depth buffer. The accepted
version conservatively skips an entire fringe cell when its screen footprint may
intersect a nearer raised roof. It does not paint that roof or change its geometry.
Some otherwise visible fringe pixels may therefore be omitted deliberately.

Tile/height changes invalidate the second ring (donor -> raised neighbor). Distant
foreground roof edits cause a batched refresh of the candidate fringe cells before
rendering; previously occluded candidates remain eligible for later recovery.
The layer is allocated lazily, uses at most 15 sprites, and is disposed with its
world. The exhaustive combined cache still fits four 2048x2048 atlas pages.

## Repeatable joint cameras

```
--rubberduck-rock-coast-playtest MAP OUTPUT cliff-review 16
--rubberduck-rock-coast-playtest MAP OUTPUT cliff-review-shaded 30
```

Slots 16, 24, 26, 30 are supported. The utility selects a deterministic connector
near the map center, writes `cliff-location.txt`, and exports a diagnostic camera
copy using shared production artwork. It rejects missing connectors and maps with
custom rules/art rather than silently replacing those definitions. These are
visual-only fixtures, not movement certification.

## Evidence

Root: `%TEMP%/ymca-cliff-joints-review`.

- `joint-{16,24,26,30}.png`: normal baseline closeups from Mountain Valleys 4P42;
  `restored-shaded-*`: shaded baseline with restored cap art.
- `candidate`, `cap-continuous`: rejected recoloring evidence only.
- `apron-fringe.png`: rejected unoccluded overlay showing a dirt stripe on a roof.
- `apron-occluded.png`, `apron-shaded.png`: accepted normal/shaded views inspected;
  the erroneous roof stripe is absent, blocked dirt remains visibly dirt.
- `editor-materials-large`, `editor-corners-shaded`: **95 live editor assertions each**,
  including all four fringe directions, isolated-dirt rejection, receiver exclusions,
  height restoration, second-ring invalidation, nearer-roof occlusion/recovery,
  exact transactions and save/reload.
- Large 192x192 exhaustive cache: 624 ground + 1152 water + 15 apron sprites,
  four pages (64 MiB), 259 ms precache, 53,053,080 thread-allocated bytes,
  67 ms full refresh. Shaded fixture: 242 ms / 25 ms. Not a GPU/long-match benchmark.
- `production-play`: actual Mountain Valleys 4P42 with shroud: **32 movement legs,
  eight blocked-foot checks, eight height-edge checks, MCV/home deployment**.
  Screenshot inspected. Production `map.bin` and `map.yaml` remain identical to
  the earlier export; diagnostic terrain is identical to production terrain.

## Occlusion scan follow-up

The production occlusion search now visits only the three diagonals whose projected
128-pixel diamonds can overlap the receiver, rather than iterating a square and
rejecting the other positions. The old square search remains a diagnostic reference.
No roof intersection, source eligibility or collision rule changed.

`cliff-review[-shaded]` now verifies both searches at every in-bounds map cell before
its screenshot and logs CPU-only timings via `runtime-results-path.txt`. The
benchmark alternates order after warming both paths. It is opt-in diagnostic work,
not a scan added to ordinary gameplay ticks.

Evidence: `%TEMP%/ymca-occlusion-band`:

- `editor-large`, `editor-shaded`: **96 live editor assertions each**, including all
  four foreground ramp orientations, distant height-eight roof edits and recovery,
  reference equality and exact terrain restoration. Save/reload passed.
- Reference equality at 33,060 / 12,100 editor cells; mean full scans 23.12 / 9.73 ms
  for three bands versus 33.19 / 15.05 ms for the square reference.
- `checked-cliff-review`, `checked-cliff-review-shaded`: actual production-map clients,
  9,216 cells checked each, 230 candidate foot-fringe cells. Mean full scans
  4.13 / 4.11 ms versus 6.24 / 6.25 ms. Both screenshots inspected. No new movement
  certification is claimed for these visual-only runs.
- Total **63,592 cell comparisons** across these four runs. Timings cover the
  occlusion search, not full refresh, GPU time or frame rate. The large full material
  refresh measured 71 ms; this is not evidence of an overall refresh improvement.
- The initial `cliff-review` attempt failed because its results path was outside the
  probe's restricted temporary directory. The utility now uses the existing hashed
  log convention. That failed run is not counted; only `checked-*` runs are evidence.

## Convex-foot qualification and invalidation follow-up

The topology planner reserves both cardinal feet and a diagonal convex foot when
both cardinal flanks fit. The material renderer previously recognized only a
cardinal raised neighbor. Consequently it skipped the exterior fringe around
legitimate diagonal feet, leaving hard cuts in the soil outline.

`RubberduckMaterialTransitions.IsApronDonor` now also recognizes a diagonal flat
height-four roof **only with both intervening height-zero 3992 flanks**. An isolated
diagonal dirt cell, one missing flank, water/bank/ramp flanks, and a diagonal ramp
roof do not qualify. The prior cardinal rule is unchanged. Receivers remain only
ordinary low grass; blocked dirt is not repainted or unblocked.

The dependency from receiver through donor to diagonal roof spans offsets `(2,1)`
and `(1,2)`. All eight rotated offsets now participate in tile/height invalidation.
Refreshing existing candidates alone would be insufficient: raising the roof must
also discover receivers that were previously ineligible. No texture, mask width,
clipping geometry or cache-size change was needed; this remains the existing
**procedural soil fringe**, not authored cliff-foot art.

`cliff-review[-shaded]` additionally emits a read-only `cliff-apron-audit.tsv` and
summary. The Mountain Valleys 4P42 census found 328 cardinal-edge and 178 closed
convex low blocked cells, with no unclassified low blocked dirt. The convex cells
have 230 geometrically visible receiver edges (not unique cells; native actors may
cover them further). Nearby actor names are diagnostic context, **not proof of
pixel coverage**. In particular, this census is not permission to remove footprint
cells and does not prove that their sprites cover the entire reserved area.

Evidence: `%TEMP%/ymca-convex-apron`:

- `red/expected-regression.log`: the new live regression failed against the old
  renderer with `Closed convex cliff foot lost its outward soil fringe.`
- `editor`, `editor-shaded`: the corrected renderer passes the new eight-orientation
  cases, flank and ramp exclusions, third-ring Do/Undo/Redo, injected rollback,
  and the existing material/water/occlusion/editor save-reload checks. Each fresh
  session has 99 `COAST EDITOR PASS` lines plus the separate occlusion oracle line.
- `cliff-review`, `cliff-review-shaded`: actual-client production closeups inspected;
  comparison baselines are the preceding `ymca-end-junctions` views. The outer soil
  border is softer, but the broad flat brown core remains. Review map binaries are
  identical; review YAML differs only in its dedicated temporary result path.
- `production`: 32 movement legs, eight blocked-foot and eight height-edge checks,
  home MCV placement/deployment under shroud. Screenshot inspected. Production
  `map.bin` and `map.yaml` are byte-identical to the authored-foot export.
- `vergleich.png`: matching crop of the exposed foot, before/after. No new artwork
  or source-file edits. Atlas size remains bounded; no GPU performance claim.

The broad side/end/foot acceptance gate remains open. This improves the material
edge around an existing footprint; it does not erase protected raster teeth, fill
all native/fallback geometry mismatches or supply authored surf artwork.
