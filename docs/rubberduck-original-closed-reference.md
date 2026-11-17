# Closed original-cliff reference

The source-first reference now includes a complete rectangle and four closed
L-shaped variants. Every visible component comes from the original grassland
family: no fitted walls, recoloring, procedural edges, alpha keying or substitute
geometry. Original grass roof layers use ordinary source-over composition.

## Reproduction

```
--rubberduck-native-corner-lab SOURCE OUTPUT layers=closed
--rubberduck-native-corner-lab SOURCE OUTPUT layers=closed-0
--rubberduck-native-corner-lab SOURCE OUTPUT layers=closed-1
--rubberduck-native-corner-lab SOURCE OUTPUT layers=closed-2
--rubberduck-native-corner-lab SOURCE OUTPUT layers=closed-3
```

`RubberduckOriginalClosedReference` defines a 5x5 roof rectangle at x=0..4,
y=-4..0. Each numbered case removes one 2x2 corner: south, west, north, east.
The base has 25 complete roof diamonds; each L shape has 21. All five have a
20-edge boundary and 24 wall/corner placements. Positions are derived from the
roof-cell set, not manually patched for each orientation.

For each exposed roof-cell edge, the corresponding wall is placed at the
neighboring cell. Two adjacent wall directions at the same position are replaced
by their authored inward connector. An outward corner occupies the diagonal
neighbor of its two incident exposed faces.

## Matched original components

Coordinates below are local to the original 128x256 source rectangle.

| Role | Logical/source slot | Roof edge or vertex |
|---|---|---|
| Front southwest | 0..2 / 36..38 | (64,32) to (128,64) |
| Front southeast | 5..7 / 41..43 | (0,64) to (64,32) |
| Rear northeast | 8..10 unchanged | (0,64) to (64,96) |
| Rear northwest | 12..14 unchanged | (64,96) to (128,64) |
| Inward front | 16 unchanged | (0,64) through (64,32) to (128,64) |
| Inward side | 18 unchanged | (64,32) through (0,64) to (64,96) |
| Inward rear | 20 unchanged | (0,64) through (64,96) to (128,64) |
| Inward opposite side | 22 unchanged | (64,32) through (128,64) to (64,96) |
| Outward south | 24 / 44 | (64,32) |
| Outward east | 26 / 46 | (0,64) |
| Outward north | 28 unchanged | (64,96) |
| Outward west | 30 / 48 | (128,64) |

Inward slots 16/18/20/22 use `grass_cliff_trans.png` frames 12/14/16/18,
respectively, at offset (0,32). These are original overlays, not painted patches.
The established straight/front-outward overlays remain unchanged. Rear straight
pieces and slot 28 retain their entire original RGBA, including opaque dark
interiors. Those interiors are covered by subsequent roof tiles: they are not
indiscriminately deleted as black matte.

The offline compositor interleaves roof and wall sprites by actor-cell depth,
matching the client fixture. Drawing every wall after every roof is incorrect for
mixed front/rear inward pieces. Source hashes, rectangle mappings, placements,
layer provenance and decoded PNG pixel/anchor checks are exported with each case.

## Evidence and checks

`%TEMP%/ymca-original-closed/{closed,closed-0,closed-1,closed-2,closed-3}` contains
both `original-closed-section.png` and actual-client `ingame.png`. All five client
views were inspected on a contrasting sand floor, including both side notches and
the rear notch. The final camera keeps the full composition clear of the sidebar.
`reference/ingame.png` is an earlier, partly sidebar-obscured rectangle capture,
not the authoritative final view.

Each shape validates equality between its complete roof boundary and the source
wall edges, plus every convex corner vertex. Four negative cases per shape reject
a displaced rear face, wrong corner, missing corner and missing rear face. These
are source-geometry checks, not pathfinding tests. Exported maps reload with valid
custom rules. Existing production cliff images are checked separately for byte
identity after regeneration.

## Correction to the earlier open roof patch

The earlier `layers=roof` patch incorrectly used (64,96) as the middle roof point
of inward slot 16. Its actual upper point is **(64,32)**, also confirmed by original
grass overlay frame 12. The old patch included one extra diamond behind the wall;
opaque artwork hid the overfill. The corrected open patch has **23**, not 24,
roof diamonds. This correction is restricted to the diagnostic roof assembly;
production roof rendering, source art and fitted cliff PNGs are unchanged.

## What this proves, and what it does not

Compatible original components exist for all four straight orientations and all
four inward/outward turns in this family. A complete closed visual assembly is now
available, rather than just an open front contour. The original artist's examples
remain comparison references, not pixel-identical reproductions of these fixtures.

The maps are still flat, non-occupying sprite diagnostics. This does not certify
height-four fitting, low-foot reservations, navigation, ramps, editable terrain,
shaded production rendering or arbitrary narrow/pinched contours. In particular,
the production native/fallback junction at 87,-23 is **not repaired by this lab**.
The next step is adapting the consistent source family to the shared height-four
renderer/planner while preserving or explicitly validating every reserved foot,
route, resource and editor transaction. No production terrain was replaced here. The subsequent
[height-four integration test](rubberduck-closed-height-four.md) now exercises real
roof/foot terrain, vehicles and editor transactions, with remaining visual defects
explicitly preventing global activation.
