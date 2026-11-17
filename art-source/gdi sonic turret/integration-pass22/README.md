# Approved Sonic pass22 installed on STWR

## Current size: 65%

User requested the Sonic smaller too. All seven body/turret/dead bindings now use
Scale0.65, with the original PNGs untouched. Sequence offsets remain0,-52 /0,-11
because sprite offsets are scaled by the renderer. Attachment0,0,599; muzzle957,0,677;
selection decoration941×941, height78. Footprint, bib and gameplay unchanged.
Engine loading/projection checks pass (0.132px max). `rules-fullsize.yaml` preserves
the previous integration; `rules-after.yaml` now records the reduced size.

## Original full-size registration (historical)

User explicitly approved installation. No further artwork changes were made.
`mods/ca/bits/gdi/buildings/sonic-{base,turret32}.png` are byte-identical to the
approved pass22 exports. Stationary supply cables remain in the base layer;
receiver pipes, hanging leads and small radar rotate with the turret.

- Native224 frame, Scale1, 32 facings; facing start12.
- Fixed base Center offset0,-52; turret Center offset0,-11.
- Attachment0,0,921 from model pivot Z2.35; CameraPitch85.
- Muzzle1472,0,1042 from luminous core center at model
  X3.2521789933, Y0, Z5.0083486389. Engine max projection error0.184px.
- Attachment rounding relative to the original composite is under0.3px.
- Old embedded turret/body and legacy minibib replaced by independent parts and
  existing1×1 GDI decorative bib. Footprint, hit shape and gameplay remain intact.
- China construction retained; turret and decorative bib hidden while incomplete.
- Idle, damaged, dead and turret bindings use the new assets, not the old SHP.

`--check-sonic-art` loads real native frames, all new sprite sequences, construction
and trait dependencies, and verifies muzzle projection over32 directions.
`rules-before.yaml` / `rules-after.yaml` and `test_sonic_installation.py` guard
unchanged weapons, stats, turn speed, power, detection and prerequisites.

The existing `~disabled` prerequisite remains: this is art integration, not a new
balance/unlock change. STWR already exists on both `testmap1.oramap` and the disposable
`Flak Voxel Recoil Test (local art)` map. Neither map was modified. Restart/reload
to view the new graphics there. Live GPU/firing review remains pending.
