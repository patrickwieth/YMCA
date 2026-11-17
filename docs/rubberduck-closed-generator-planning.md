# Closed cliffs: topology-first production generation

Historical partial rollout. The [required closed-plateau migration](rubberduck-closed-plateau-completion.md)
now rejects unsupported candidates before commit instead of retaining plateau fallbacks;
72 exports covering 866 filled plateaus passed. Counts below document the earlier stage.

## Implemented

Mountain Valleys now plans compatible closed **home and raised-island plateaus**
before economy placement. This changes newly generated maps, not saved maps. The
renderer does not secretly block land to fit artwork.

- Fit roof bands entirely inside the original footprint: a centered five-cell band
  and paired two-cell outer bands. Keeping complete blocks yields contour runs with
  nonoverlapping art sockets. Unlike a plain 2x2 lattice, this fitting commutes with
  all four rotations and reflections; eight transforms are regression-tested.
- Homes retain both complete 25-cell starting fields and the flat five-by-five CPos
  construction core. Only fringe cells may be removed during fitting.
- Search supported ramp positions/directions before accepting a legacy candidate.
  The actual three-wide, four-level ramp remains unchanged. Already blocked ramp
  guards may become height four: this is an explicit terrain-height change.
- Reserve the complete low-foot ring while the component is still a draft. Reject
  occupied, non-land, out-of-bounds, protected, resource or existing-component feet.
  Commit new blocked cells with the plateau, before resource placement.
- Use the **same `TryPieces` socket/contour validation** in planning and rendering.
  Rejected attempts do not mutate terrain/features or the input surfaces.
- Retain established fallback rendering for unsupported components. Low expansion
  valleys, blocked mountain ridges and water/coast planning are not reinterpreted.
- Preserve existing height connectivity, spawn/build-core, expansion, resource and
  gameplay validation. Strategic checkpoint placement still retries whole layouts
  rather than flattening ramps or clearing protected terrain.

## Ordinary editor continuity

An early production grow probe exposed an unsupported edit that silently reverted
one new component to the old cliff family. `PlateauShapePlan` now **rejects** edits
that would lose an existing closed contour before committing any transaction.
Supported edits retain the family, foot reservations and world-coordinate variants.
Legacy components retain their existing planning path.

The real-editor regression also encountered a projected cell center that was not
selectable. Its probe now searches for a selectable pixel **inside that cell's
projected diamond**, confirms it using the real viewport picker, and exercises the
ordinary mouse-driven brush. It does not bypass picking or weaken edit validation.

## Final evidence

Root: `%TEMP%/ymca-closed-planning`.

- `balanced-final-runtime`: actual generated 4-player seed-42 map. The four home
  plateaus use **436 closed-cliff actors**; 108 legacy native actors remain on other
  unsupported plateau geometry. The separate blocked mountain renderer also stays.
- `balanced-final-runtime` and `balanced-shaded-runtime`: actual client captures;
  **each** run passed 32 ascent/descent legs, 14 blocked-foot checks, two clear-terrain
  steep-edge checks and MCV deployment. Production terrain was copied unchanged
  into the probes. Shaded mode changes only the diagnostic package's tileset.
- `balanced-editor-visible`: actual ordinary-editor extension at `41,5`, two changed
  cells and five PASS categories (draft, edit, exact Undo, exact Redo, save/reload).
  Saved result retains 436 closed and 108 legacy native actors.
- `balanced-matrix`: 21 exports at seed 43: 2/3/4/6/8/12/16 players in each of
  Tactical, Operational and Strategic. Existing generation/gameplay validators
  passed. These are exports, **not 21 additional client sessions**.
  - 4 players: all eight filled plateaus select the family, 736 closed actors.
  - 12 players: 14 of 28 filled plateaus select it, 1210 closed actors.
  - 16 players: 23 of 40 select it, 1887 closed actors.
  - 2/3/6/8-player cases select all their filled plateaus.
- Twelve additional generation checks cover None/Glitter/Patches, finite on/off,
  at 4 and 16 players, seed 42. Four further 4-player seeds (40, 41, 44, 45) pass.
- `other-presets`: eight other presets, 4 players, seed 42. `map.bin` and `map.yaml`
  match the preceding rollout exports byte-for-byte. They are compatibility checks,
  not claims that those generators acquired new plateau geometry.
- `--rubberduck-closed-contour-test` additionally checks eight fitting transforms,
  draft foot reservation, unchanged walkable roofs/ramps, guarded height conversion,
  five protected-foot rejection cases and no premature terrain/feature mutation,
  alongside existing contour, composition, lowering and editor-growth checks.

Earlier `complete-runtime`, `shaded-runtime`, `final-matrix`, `editor-preserved`,
`runtime`, `first`, `coarse`, `retry`, `matrix` and `balanced-runtime` are intermediate
fits. In particular, the earlier 624-actor fit used an asymmetric 2x2 lattice and is
not the retained implementation. `editor` reproduced contour downgrading;
`balanced-editor` and the first `balanced-editor-visible` launches reproduced the
picker-center issue. Only the completed five-PASS session is final editor evidence.

## Remaining scope

Compatible generated plateaus are no longer test-only. Unsupported small/irregular
components still fall back. This does not certify every seed, arbitrary contour,
zoom/filtering combination, large-map frame pacing, combat elevation interaction,
or conversion of the separate blocked mountain renderer. Bridges remain separate
pending their sprites. Original artwork was not changed.
