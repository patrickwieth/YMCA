# Soviet Chemical Tower — static native artwork

## Current: pass06, installed on TTUR (Iraq)

Final user placement follow-up: move the body12 native pixels down to the front
bib edges (offset0,8). The outer footing retains about1px clearance; a larger move
would overhang the side/front edges. PNG pixels, bib anchor and occupied cell are
unchanged. Aim mount follows the body at Z1651; selection decoration shifts down
by272 world units. Review: `pass-06/placement-front-edge-3x.png`.

The oversized unshaded triangular pass05 cap was rejected. Pass06 replaces only
rows above48 with a short rounded crown: source-color curvature steps, upper-left
highlight and continuation of the original diagonal panel seam. All pixels at/below
row48 are byte-identical to pass05. Reviewed enlarged crown comparison and native/3x
placement. Reproduce with `tools/art/round_chemical_tower_dome.py`.

The 1x1 bib was also offset half a cell by the retained old2x2 building center.
TTUR now uses LocalCenterOffset0,0,0: body and bib centered on occupied cell0,0.
The actual engine geometry export/grid audit confirms exact anchor0,0, not merely
the declared bib size. Original and all earlier passes preserved.

## Previous: pass05 lower contour revision

`pass-06/chemical-tower.png` is installed directly as
`mods/ca/bits/soviet/buildings/chemical-tower.png`. Original `chemtower.png` and
passes01–04 are preserved. Earlier claims that pass03/04 had fixed the edges were
premature; those lower closures were rejected by the user.

The supplied 128x192 canvas clips the art at y48 and y175. Pass05 retains the
original outer piers, side cylinders and mechanisms, reconstructs the clipped
central dome contour, and closes the lower wall with a continuous curved native
ring cut. The short inferred wall extension continues the existing panel layout
with tonal steps selected from original colors, not a repeated facade/vent strip.
Isolated one/two-pixel extraction debris is removed. This is **not** exclusively
cut-and-paste: the inferred dome and lower-wall closure use controlled source-color
painting. Original source unchanged; reconstruction recorded in
`pass-05/reconstruction.json`.

Visual review: source/previous/current comparisons on dark and light ground,
then native/3x placement on the actual 1x1 bib. Intermediate flat pier caps and
floating stud cap marks were rejected and removed before installation. This is
one fixed source view, not a rotating model. Hidden surfaces are not claimed recovered.

Reproduction: `python tools/art/rebuild_chemical_tower_edges.py`.
Reviews: `pass-05/comparison-ground-3x.png`, `comparison-dark-3x.png`,
`placement-native.png`, `placement-3x.png`.

## Binding and verification

- `rules/soviet/chemical-tower-art.yaml`, `sequences/soviet-chemical-tower.yaml`.
- Scale1, Bottom anchor, offset0,8 (including the final12px downward placement).
- Existing invisible aiming mount follows body position:0,0,1651.
- User-requested 1x1 solid footprint and Soviet1x1 bib retained.
- Existing weapon, cost, health, prerequisites and static-body behavior retained.
- Resolved engine binding, source-color provenance, preserved main body pixels
  and exact installed bytes checked by `tools/test_current_building_followups.py`.
- Actual full default sprite set loaded with the current isolated engine utility.
- Live GPU/firing review remains unverified; no running game/Blender stopped.

No release, push, or unrelated faction art changes.
