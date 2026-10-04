# Spatial native water in the shared terrain renderer

`RubberduckMaterialTransitions` now defaults to `NativeWater: true` in Rubberduck
runtime and editor worlds. Other tilesets are unaffected. `NativeWater: false`
retains the previous marker-water renderer for comparison or rollback.

## Source and derivation

The source is the existing 768x384 `bits/terrain/rubberduck/water_v01.png` patch.
Its decoded RGBA pixels match `rubberduck tileset/grassland/water_v01.png` exactly.
Different PNG metadata/encoding accounts for the distinct file hashes:

- Original SHA256: `2FCC96B7765CCC260A5D0FAF919ADCDA116B26CDA185402FCEFFBF1F2D6EAECA`
- Installed SHA256: `6909C90FD349D9496504899F5A243B10FE02330493911EC0C8E74628726C8B63`

No source or installed PNG was rewritten. The runtime crops 36 unscaled 128x64
geometric diamonds at `(320+64*(x-y),32*(x+y))`, for x/y in 0..5. These cover every
pixel center inside the source's geometric diamond exactly once; this is asserted
when loading. The authored outer antialias fringe beyond that diamond is cropped,
not claimed as reconstructed: 2674 nonzero-alpha pixels lie in that fringe.
Interior RGBA values are copied without resampling. These are spatial phases, not
36 animation frames. No animation was invented.

Cell phase is `modPositive(CPos.X,6) + 6*modPositive(CPos.Y,6)`. Negative coordinates
and both period boundaries are validated in the live editor probe. Do not phase on
staggered MPos coordinates or choose frames randomly.

## Rendering and invariants

- Template 1050 and the blocked water footer 14010 receive the same phased water
  surface. Their collision, heights, resources and actor definitions are untouched.
- Ordinary land cells next to 1050 receive the same phase through a narrow alpha
  transition. Rock banks, blocked dirt, ramps and unequal heights remain excluded.
- Water is a separate layer over ground blending. This avoids multiplying every
  ground-material combination by all 36 phases and preserves compositing priority.
- Maximum cache: 624 nonempty ground combinations + 576 water sprites (36 phases
  times 15 edge masks and one complete diamond). ProfileCache precaches both bounds
  and verifies their counts. Both layers, caches, source arrays and event handlers
  are released with the world.
- Height edits that project cells outside map bounds clear their old overlays.
- This is native **water texture** plus procedural shore fading. Authored shoreline
  pieces/foam, coastal raster smoothing, and naval fairness are not thereby
  completed. Template 1060 support was added subsequently, as documented below.

## Legacy templates 1070/1080: holes and rectangular cuts fixed

These editor water templates previously indexed rectangular 128x64 chunks of the
large diamond-shaped source patch. Depending on their saved frame index, they
showed black holes, rectangular scraps and hard shore edges. This was reproduced
in an actual client, not inferred just from the PNG metadata.

- Both normal/shaded tilesets now give all **16 existing indices** a valid marker
  diamond as the underlying terrain sprite (`Frames: 0` repeated sixteen times).
  IDs, index values, `Water` terrain type and collision are preserved. The original
  large PNG sheets are unchanged. This removes the malformed base sprite rather
  than merely putting another sprite over its holes/rectangular overhangs.
- Shared runtime/editor rendering treats 1070 as water A and 1080 as water B,
  including phase selection, shore masks and lazy secondary loading. No additional
  source textures, material families, atlas pages or frame-dependent caches.
- With `NativeWater: false`, these templates now have complete marker diamonds,
  not the old malformed raw-sheet chunks. Secondary shore blending remains a
  native-renderer feature.
- Existing tile/index/resource/height data is not migrated or rewritten. Isolated
  pond geometry stays the same. This is not authored foam or contour smoothing.

Evidence: `%TEMP%/ymca-legacy-water`:

- `before/ingame.png`, `after.png`, `vergleich.png`: actual same-package before/after
  views and a crop showing the removed holes/cuts. Early diagnostic logs are not
  counted in the final assertion totals. The first fixture export tried indices
  beyond the legacy templates' sixteen entries and was corrected before capture.
- `checked-legacy-water`, `checked-legacy-water-shaded`: both closeups inspected;
  **18 cargo assertions + two actual legacy-water boat movements each**, with
  physical water contact and forbidden land targets checked.
- `checked-editor-materials-large`, `checked-editor-corners-shaded`: **97 live editor
  assertions each**, including 640 legacy family/frame/land-base/direction cases,
  cache reuse, height/blocked-bank exclusions, exact restoration and save/reload.
- `reload-legacy-water`, `reload-legacy-water-shaded`: fresh export/reload assertions
  preserve every tile/index, height and resource entry.
- `generated-fog`: six cargo assertions on unchanged Water Basins 4P42 terrain;
  `marker-regression`: eighteen cargo assertions with native water disabled.
- Total **194 editor assertions, 60 cargo assertions and four legacy-water moves**.
  The final legacy fixture terrain is byte-identical to the before package.
- Combined cache remains 624 ground + 1152 water + 15 apron sprites / four pages
  (64 MiB). The 192-square editor measured 249 ms precache and 66 ms full refresh;
  this is not a GPU or long-match certification.

## Secondary water (1060) and mixed shores

The renderer also phases `water_v02.png` for template 1060. Its decoded RGBA matches
`rubberduck tileset/grassland/water_v02.png` exactly; neither file was rewritten.

- Original SHA256: `FA3973BD08252268D27B6D1DBECE0E8283E2520D47E6178474F3E17A8F594D7E`
- Installed SHA256: `24AC2F6E686033620165ACA72F9F47D24DA30386EB3EFF10037EF8FB93EFFF63`

A second water layer/source is loaded only when needed. Primary-water-only maps
retain their original allocation/render-layer count. Both water families can
contribute shore masks to the same land cell; secondary water composites last.
No land texture is painted onto water cells. Height/ramp, cliff and protected-bank
exclusions apply to both families. Repainting/removing secondary water updates its
layer along with the primary and ground layers.

With both families, the cache limit is 624 ground + 1152 water sprites: four atlas
pages (64 MiB) at the exhaustive bound, rather than multiplying all ground keys by
72 phases. `NativeWater: false` preserves the older marker behavior; it does not
add secondary-water shore blending. Direct water-A/water-B boundaries keep the
artist's distinct surface colors; this change does not invent a blended third
water material. Legacy templates 1070/1080 were corrected in the subsequent update below.

Evidence: `%TEMP%/ymca-water-variants`.

- `checked-water-variants` and `checked-water-variants-shaded`: reviewed closeups
  of two tiny ponds and their mixed land junction. Eighteen outer-coast/cargo
  assertions each, plus one real LST movement inside water B each (tick 26), with
  forbidden dirt entry rejected and physical water/height-zero contact checked.
  These tiny-pond tests are not a naval-width/fairness benchmark.
- `editor-materials-large` / `editor-corners-shaded`: 94 live editor assertions each,
  including lazy loading, all four secondary-water shore directions against five
  land bases, mixed water sources, height invalidation, blocked-bank exclusions,
  2401 valid neighborhood coverage combinations and exact save/reload.
- Large 192x192 exhaustive cache: 624 + 1152 sprites, four pages, 269 ms precache,
  52,690,784 thread-allocated bytes, 49 ms full refresh. Shaded fixture: 223 ms and
  18 ms refresh. These are CPU observations, not GPU/long-match certification.
- `marker-regression`: 18 assertions with explicit old renderer; `generated-regression`:
  six cargo assertions on actual Water Basins 4P42 with shroud. Production terrain
  is unchanged. The older independent Challenger shore-calibration failure is not
  resolved by these distinct boat tests.

Use rock-coast-test modes `water-variants` / `water-variants-shaded` to reproduce.

## Shore source audit

The current original set contains `grass_tiles_w_trans.png` and
`sand_tiles_w_trans.png`, with authored ground cutouts/edge detail. Files named
`grass_shore` and `sand_over_water_shore` appear in historical generation tooling,
but are not independent original shoreline assets in the current source tree.
Their names do not establish native-foam provenance. The bright cutout fringes
have therefore not been installed as purported surf. Authored bank integration
still needs a verified frame/socket mapping that preserves passable cell centers;
current shore fading remains explicitly procedural.

## Earlier primary-water evidence

Root: `%TEMP%/ymca-native-water-production`.

- `native-water`, `native-water-shaded`: 18 cargo/coast assertions each; inspected
  both screenshots. `marker-water`: 18 assertions for the explicit old fallback.
- `editor-materials-large`, `editor-corners-shaded`: 93 editor assertions each,
  including mask/phase checks, transactions and exact save/reload.
- `editor-final`: another 93 assertions after projected-bounds cleanup. On the
  192x192 fixture, all 1200 sprites occupied three atlas pages (48 MiB capacity):
  223 ms precache, 34,679,728 thread-allocated bytes, 34 ms full-cell refresh.
  Earlier native large/shaded measurements were 332/248 ms and 45/14 ms refresh.
  These are CPU measurements, not GPU residency, FPS or long-match profiling.
- All five water presets (`two-sides`, `water-basins`, `continents`, `archipelago`,
  `migration`): actual 4P42 generated-map cargo tests, six assertions each; actual
  8P43 shaded overviews. `contact.png` reviewed. Every diagnostic `map.bin` is
  byte-identical to its production source.
- `editor-save-play`: five cargo/landing/boarding assertions on the actual saved
  192x192 editor geometry; `map.bin` remains byte-identical to that save.
- `water-basins/generated-fog`, `archipelago/generated-fog-shaded`: six cargo
  assertions each with unexplored shroud enabled. Inspected screenshots show
  water and coast artwork covered outside the revealed area. This does not certify
  all cliff/vehicle/projectile occlusion combinations.

Reproduce with `--rubberduck-rock-coast-test SOURCE OUTPUT native-water` (or
`native-water-shaded`, `marker-water`, `editor-materials-large`) and
`--rubberduck-rock-coast-playtest MAP OUTPUT generated-fog` (or
`generated-fog-shaded`). Run the exported package in the actual client and inspect
its `runtime-results-path.txt`; utility export alone does not execute the probes.

A subsequent full-shroud replacement experiment did **not** remove the thin
map-edge slivers. It was reverted; neither the generated opaque mask nor the
sequence override is installed. `shroud-candidate.png` and
`rejected-opaque-shroud.png` preserve that rejected trial. Partial-edge artwork and
projected boundary coverage needed further diagnosis; opacity of the full tile alone
was not the cause. The subsequent [shroud edge correction](rubberduck-shroud-edges.md)
replaces the partial-mask coverage as well, with separate runtime evidence.

Build/diff checks and original-asset hashes are separate from visual acceptance.
See [remaining acceptance](rubberduck-terrain-acceptance.md).
