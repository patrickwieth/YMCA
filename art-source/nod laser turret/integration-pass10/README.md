# Approved Nod laser pass10 installed on LTUR

## Current size: 50%

User requested another reduction after reviewing65%. All five body/turret bindings
now useScale0.5, attachment0,0,79; both muzzles953,0,784; selection724×724/height59.
Sequence offsets and original PNG bytes remain unchanged. Engine muzzle error
0.144px maximum; runtime scale and gameplay regression tests pass.
`rules-after.yaml` is current; `rules-scale65.yaml` preserves the intermediate size.

## Previous size: 65% (historical)

All five body/turret sequence bindings now use Scale0.65. Original PNGs are
unchanged. Sequence offsets remain0,-58 /0,-51 because the renderer scales sprite
offsets together with the image; scaling those twice would misplace the parts.
Attachment is now0,0,102; both muzzle offsets1238,0,1019. Decoration bounds941×941,
height77. Gameplay footprint, hit shape, bib, weapons and stats are unchanged.
Engine muzzle check passes at0.168px max; resolved-rule/sequence regressions pass.
`rules-fullsize.yaml` preserves the previous integration; `rules-after.yaml` is
current. Live appearance/firing review still pending.

## Original full-size registration (historical)

User approved installation without further geometry/material redesign.
`laser-base.png` and `laser-turret32.png` under the authoritative Nod building
folder are byte-identical to pass10 exports. Original supplied artwork and
`conyard.png` were not modified.

- Native256 frame; fixed base Center offset0,-58.
- Independent32-facing turret, Center offset0,-51; facing start20 as exported.
- Turret attachment0,0,157; CameraPitch85; no sprite recoil.
- Both existing armaments use physical muzzle1905,0,1568, corresponding to model
  emitter center(0,-4.21,4.4). Engine maximum projection error0.249px over32 facings.
- Existing LaserTur/LaserTur.Adv, conditions, turn speed, cost, HP, power and1×1
  gameplay footprint unchanged. Existing Nod bib retained; selection art height118.
- Turret hidden during generic China construction; obsolete embedded body removed.

`rules-before.yaml` / `rules-after.yaml` and `test_nod_laser_installation.py` check
that only visual fields and explicitly documented muzzle registration changed.
`--check-nod-laser-art` loaded the actual PNGs and actor dependencies. No live firing
or GPU appearance check claimed. Restart to load the new bindings.
