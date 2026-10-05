# Closed-cliff contacts and first ramp integration

Follow-up: [four-direction ramps and live building protection](rubberduck-closed-ramp-directions.md).
That follow-up extends runtime/editor coverage but does not resolve front-mouth
visual defects. The one-direction evidence below records this earlier stage.

This continues the opt-in height-four test path. It does **not** enable the new
vocabulary in ordinary generators/editor buttons, certify all four ramp directions,
or repair the production 87,-23 junction.

## Side-contact reconstruction

Shortening the tall source corner exposes opaque background that was hidden behind
the nearer roof in the artist's original-height assembly. Two attempted fixes were
rejected after actual-client inspection:

- Pinning the near roof contact removed the black band but displaced the rock
  strata against neighboring pieces. Removed from the active fitter.
- A `rock_cliffs.png` family replacement used unverified lower-foot correspondence.
  Its client view showed different foot coverage and much broader exposed dirt.
  It was not accepted as a drop-in substitute and was removed.

The retained implementation keeps the original shared fit. Before fitting, side
pieces 18/22 reconstruct only the connected opaque matte within the front-facing
plane from complete original faces 4/3 of the **same** `grassland_1x1.png` family.
A flood starts at the verified opaque interior (16,128)/(111,128), not the
antialiased rectangle boundary. Its fill is restricted by the original front-face
quadrilateral and the front/rear dividing ray. Donor pixels must be opaque.

This is an **authored-pixel reconstruction with a geometric selection**, not an
unchanged artist-authored corner and not procedural rock synthesis. It preserves
all source alpha, all non-matte RGBA and all translucent shadows. Assertions check
these invariants before the common height-four fit. Per-slot provenance records
the donor and changed pixel count. No terrain cell or collision is changed by it.

`authored-underlay-east` and `authored-underlay-west` were inspected in the client.
The broad black contact is replaced without the rejected pinning's displaced
strata. This scoped improvement does not certify every thin shadow/contact pixel
at every zoom or the complete production terrain.

## Rear-facing interiors

A ramp opening exposes source interiors that previously sat under a full roof.
The initial ramp view showed a large opaque black rear triangle. In SourceGrid
mode only, the renderer now retains the authored rear roof lip (four pixels below
the matched edge), rather than drawing the hidden rear interior. Straight rear
pieces are also limited to their actual half-width edge, with two pixels of
antialias allowance; extrapolating the edge across the other half produced a
small black flag. These are spatial rendering limits, not color-key deletion or
painting grass over blocked cells. Legacy renderer behavior is unchanged.

## Actual rear ramp

```
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true shaded=true
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true editor=true
```

The rectangular stamp gains a ramp facing -X:

- 12 slope cells: three lanes at each of four successive levels, Ramp=1.
- 8 blocked height-four retaining guards.
- 6 clear low approach/landing cells; the three obsolete blocked mouth cells and
  their native actors are removed during planning, before committing the stamp.
- 21 remaining low foot cells, each with one native actor anchor.
- Every lane is checked for corner-height continuity from low approach through
  the slope to the high roof. Protected cells/resources at the roof, feet, ramp or
  approach reject the operation rather than being overwritten.

The retaining strips and slope artwork use the existing **derived** ramp
vocabulary. This is not a claim that original ramp templates have been identified.
The terminal retaining-wall appearance and exposed blocked dirt at the mouth
still need visual refinement. There is no green camouflage or silent unblocking.

## Editor and runtime checks

The same stamp plan uses `PlateauTerrainEditAction` and `PlateauActorEdit` in the
actual editor. Tests cover protection, preview clipping invalidation/restoration,
exact Undo/Redo, injected partial actor-add failure, and terrain/actor/embedded-art
save/reload. Ramp/landing cell values are checked explicitly.

A further save-probe bug was fixed: saving the live Map replaces its Package, and
disposing the output could crash a later lazy music read. The probe now saves a
separate map copy, retaining the live editor's package.

Runtime checks drive two tanks and infantry, one per lane, through all four levels
up and down. Guard checks require exactly 12 ramp cells (three per level), 8
unenterable high guards, and blocked outer foot entry/pathfinding. Six movement
legs are distinct from three foot checks and three guard checks.

Evidence root: `%TEMP%/ymca-closed-contacts`.

- `pinned-east`, `pinned-west-final`, `short-east`: rejected visual candidates.
  Archived code: `rejected-pinned-code`, `rejected-short-code`.
- `authored-underlay-east`, `authored-underlay-west`: retained side reconstruction,
  two roof/ground round trips each; source-specific provenance beside each map.
- `authored-editor-shaded`: side reconstruction in the shaded editor.
- `rear-ramp`: initial ramp with exposed black rear interior; not accepted visually.
- `rear-ramp-culled`: intermediate cull, before half-width rim limits.
- `ramp-editor-final`: normal editor, six passing transaction categories.
- `ramp-play-final`: shaded runtime, six ramp legs, three foot and three guard checks.
- `ramp-editor-shaded-final`: reached all six PASS records, then crashed on a lazy
  music read after saving. Do not count it as a clean completed editor session.
- `ramp-editor-shaded-pass`: rerun with the map-copy save fix; authoritative shaded
  editor session when its log and screenshot both complete.

Global activation, remaining ramp orientations, native ramp art, ramp-end visual
acceptance and broader production fairness/footprint validation remain open.
