# Current building feedback — local modular worktree

No release/push, no running game/Blender stopped. Original supplied art and earlier passes preserved.

## Footprints and bibs

Authoritative last occupancy layer: `mods/ca/rules/building-ground-masks.yaml`.
Generator: `tools/art/complete_building_ground_masks.py`; centers are compensated
when footprint dimensions change so existing visuals/exits do not move accidentally.

- Soviet MSLO: exactly 2x2, `xx xx`, all four cells blocked.
- Soviet TTUR chemical tower: exactly 1x1, `x`.
- Soviet radar: `xx= xxx xx=` at (-1,0); center (0,1) blocked, far-right (1,0) free.
- Chinese propaganda center: 4x4 `sovietbib4x4`, existing body remains 3x3.
- Chinese war factory: solid 4x2 `xxxx x=xx`; courtyard (1,1) remains free.
  Its existing 4x4 bib reservation remains, with two additional fully passable rows.
- GDI WEAP.TD: exactly 3x3 `xxx xxx xxx`, all nine cells blocked.
  Existing production exit (0,2) remains outside the body.

## Chemical tower art

`art-source/soviet chem tower/pass-04/chemical-tower.png` installed directly at
`mods/ca/bits/soviet/buildings/chemical-tower.png`. The repeated bottom facade,
duplicate vent and discontinuous pier copies were removed. A bounded curved source
ledge closes the clipped lower wall; tiny inferred closure uses native source colors.
Source pixels above the repaired lower closure remain unchanged (except inherited
cap reconstruction). Static image, no new rotating mechanism. Reviewed enlarged
native output after correcting repeated donor marks and an excessive plain skirt.

Sequence offset is now (0,2), compensating the shorter closure while retaining the
ground registration. Invisible aiming origin lowered correspondingly to Z1787.
Reproduction: `tools/art/repair_chemical_tower_seams.py`.

## GDI missile turret

Existing local pass14 is actually installed (PNG/VXL/HVA/palette byte comparison).
External paving removed, render scale 0.85, corrected voxel-grid scale and centered
bearing at local (0,11,1527). Reviewed full 32-direction native engine-transform
rotation including rear/end-on views. Actual engine Voxel.TransformationMatrix
check passes; maximum muzzle deviation 0.240 pixels.

Textures genuinely originate from bounded source rectangles, but are repeated UV
swatches, not individually pasted whole source components. This distinction was
clarified in the art README. No claim of one-to-one reconstruction of hidden faces.

## Tesla

All four charge-level armaments now emit at Z3270, corresponding to the upper
sphere center in the final 142x204 sprite with Bottom anchor and offset (2,-3).

## Verification

`python tools/test_current_building_followups.py`: 7 tests pass using resolved
engine actor/sequence rules and actual asset comparison. Missile transform check
uses the already-built isolated utility recorded in
`.art-validation/current-building-followup/utility-path.txt` (the running game's
ordinary engine/bin utility is older). No running process replaced or stopped.

Live GPU presentation, Tesla firing and unit movement in a newly started match
remain unverified. Existing matches do not automatically reload changed rules.
