# Authored shoreline rollout — installed families

Source/fringe evidence: `%TEMP%/ymca-shore-native`. Rollout evidence:
`%TEMP%/ymca-shore-editor`, `%TEMP%/ymca-shore-production`, `%TEMP%/ymca-shore-enabled`.

## Fixed in ordinary rendering

`RubberduckMaterialTransitions` now blends secondary spatial water into primary
water receivers, including legacy 1070/1080 IDs. Previously the neighbor scan
considered only land: adjacent water textures formed a hard diagonal color seam.
The fix uses the existing bounded phase/mask cache and does not modify terrain,
collision, resources or water phase coordinates. There is no engine patch.

## Source interpretation and explicit derivatives

`--rubberduck-shore-art SOURCE OUTPUT` exports 480 pixel-identical ground frames
from the three immutable `grassland/{grass,sand,dirt}_tiles.png` sheets. There are
five materials, each normal/shaded, with 48 frames per block. The installed atlas
and provenance live under `bits/terrain/rubberduck/derived/shores/`.

Directions follow the plateau topology: SE, SW, NW, NE. Authored edge groups are
20..23, 28..31, 24..27, 16..19. Concave diagonal cuts are 32/33 (south), 34/35
(west), 38/39 (north), 36/37 (east). Diagonal cuts are suppressed when either
adjacent cardinal edge is water. All 256 neighborhoods normalize to 47 cases;
normalization is idempotent and rotation-compatible.

Frames 40..47 are half-diamond wedges. The first experiment intersected these as
full-cell convex corners. It reduced some walkable land centers to alpha 19 and
made isolated cells almost disappear. **That composition is rejected.** Full-cell
convex shores instead intersect the two authored edge cuts. Each resulting RGBA
pixel is copied at the same coordinate from a source donor; no color synthesis,
rescaling or invented foam is involved. These are derived compositions, not an
assertion that the artist supplied 47 original whole-cell tiles.

`validated-art` passed 1880 compositions (47 cases x four variants x ten material/
lighting blocks), exact donor RGBA checks and center alpha >=128. Source wedges
are preserved in the atlas, not silently reclassified as safe full-cell terrain.

## Runtime implementation and tests

`RubberduckAuthoredShoreLayer` uses a separate bounded cache (at most 920 sprites),
the existing height/depth clipping layer and shared runtime/editor invalidation.
It only qualifies ordinary flat height-zero land adjoining ordinary water;
blocked feet, rock banks, ramps and elevated roofs are not repainted.

**`AuthoredShores` is now enabled by default** in both runtime and editor. The
normal/shaded source and fringe checks were followed by real editor history tests,
ten production-map closeup captures and actual dock/ship regressions before rollout. The striped mixed-material
fixture exposed material blends reaching the transparent source shore fringe.
Ground-overlay geometry is now clipped against the actual nonzero source alpha,
without duplicating water-phase textures. The first majority-alpha/geometric-
diamond clipping trial produced thin seams at adjacent land edges and is rejected;
`fringe-normal` / `fringe-shaded` preserve the authored antialias fringe and remove
those seams. The subsequent editor and production-map review is recorded below.

Commands:

```
--rubberduck-shore-art SOURCE OUTPUT
--rubberduck-authored-shore-test OUTPUT [shaded] [mixed] [baseline]
```

`mixed-normal` and `mixed-shaded` each passed five actual Challenger routes, five
DD routes, forbidden-domain assertions and 95 primary/secondary water-join checks.
Each used 78 cached water sprites. Actual framebuffer captures are in their output
directories. Original `normal` and `shaded` captures also establish the isolated
material/island shapes, but predate the secondary-water seam fix.

After the fringe correction, `fringe-normal` and `fringe-shaded` repeat all twenty
movement/domain assertions and both 95-cell water-join checks. `fringe-art` also
checks all 1880 exclusion masks for exact once-only rectangle coverage, with no
per-cell texture copies. `baseline-normal` and `baseline-shaded` independently
passed twenty movement assertions and both water-join checks with authored shores
disabled, exercising the production water-to-water fix alone.

The first movement fixture incorrectly placed a ship outside playable bounds.
Its failed capture is rejected; bounds are corrected and all start/target/forbidden
positions are checked before export. No locomotor restrictions were relaxed.

## Rollout verification

- `ymca-shore-editor/final-{normal,shaded}`: six PASS records each. The ordinary
  `EditorTileBrush` paints a real water tile, not a direct diagnostic tile write.
  Undo/Redo restores the entire terrain/resource/actor snapshot and all source-shore
  keys exactly. Save/reload compares every tile, height and resource entry.
- The complete shoreline cache is exercised in both editor sessions: exactly
  920 sprites, two atlas sheets and 7536640 exclusion-mask bytes; CPU precache
  measured 165/151 ms. The renderer does not create a texture per water phase
  or per edited cell for these shores.
- `ymca-shore-production/{archipelago,migration,continents,two-sides,water-basins}-
  {normal,shaded}`: ten actual-client closeup captures on unchanged 4P42 terrain.
  The Two Sides baseline comparison shows its protected sand crossing already
  had the remaining grid-shaped land contacts; no terrain was silently smoothed.
  The source-backed shoreline improves its water-facing edges.
- `ymca-shore-enabled/{archipelago,migration,continents-final,two-sides-final,
  water-basins-final}`: **20 dock placements, 40 productions, 80 reciprocal legs**
  with native shores enabled by default.
- The first Continents rerun exposed a diagnostic actor-selection problem: the
  probe could select an unrelated existing DD instead of the new yard's ship.
  Production now snapshots existing actors and identifies a new nearby vessel.
  The final Continents run passed without changing locomotion, exits or terrain.
- One initial editor capture lacked the probe on `EditorWorld`; this test-fixture
  registration was fixed before the successful normal/shaded sessions.

These are finite installed-family checks, not a claim that every decorative source
sheet is a compatible terrain family. Ordinary painting still uses its existing
editor semantics; the renderer itself does not edit resources or actor footprints.

## Subsequent fog and network checks

The [fog/network/seed follow-up](rubberduck-fog-network-and-seeds.md) adds real
source removal on both sides of twenty shoreline positions per tileset, selected
foreground-roof/cargo reruns, a two-client synchronization smoke test, and an
expanded-seed generator regression fix. These do not imply complete hardware or
long multiplayer coverage.

## Still open

- Broader fog/occlusion closeup combinations and the remaining acceptance matrix.
- Wider balance, multiplayer and hardware coverage tracked in the main acceptance
  checklist. None of these are marked complete by the source-atlas tests above.
- Bridges remain excluded.
