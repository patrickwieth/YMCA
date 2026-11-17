# Approved AA pass11 — installed locally on CRAM

The user explicitly approved pass11 and authorized integration. No newer AA art
was substituted. Canonical runtime files:

- `mods/ca/bits/gdi/buildings/aa-gun-base.png`: approved base pixels, explicit 192x192/1-frame metadata.
- `mods/ca/bits/gdi/buildings/aa-gun-turret32.png`: byte-identical approved sheet, 192x192/32 frames.

Only **CRAM** (GDI AA Gun) uses `cram.native`. Allied **AGUN** remains identical to
the captured resolved baseline. Fixed body and rotating turret are separate traits;
base and damaged-base use the same approved static image. The original 1x1 GDI bib
is a decorative overlay. No occupied cells or hit shapes change.

Registration: base Center offset `0,-40`; turret Center offset `0,-2`; attachment
`0,0,862`; CameraPitch85. Runtime sheet frame i corresponds to model yaw `(4+i)%32`.
InitialFacing224 and TurnSpeed60 remain unchanged. Visual selection bounds1448²,
height84 encompass the all-facing artwork without modifying mouse/physical bounds.

**Physical muzzle adjustment:** all four existing GAT armaments change LocalOffset
from `520,0,450` to `1303,0,750` relative to the new attachment. This places emission
at the modeled six-barrel cluster center (model `0,2.8798233,4.1131529`). It is an
intentional change to projectile/muzzle origin, not a weapon-stat change. Gatt.0..3,
firing conditions, accumulation/revocation, damage/range/rate, cost, health, power,
prerequisites and detection remain unchanged. No weapon definitions were edited.

Existing construction/sell animation and icon remain inherited from CRAM; the
approved source supplied no replacement construction animation. Damaged states
currently reuse the approved PNG, not invented damage art.

Validation:
- CA project compiled successfully (existing warnings only).
- `--check-gdi-aa-art`: actual engine PNG loader reads 1+32 native frames; trait
  construction succeeds; all four muzzles across32 facings deviate at most0.164px.
- `test_gdi_aa_art.py`: approved pixels/paths, gameplay regression, Allied isolation
  and fresh engine validation pass.
- `before/` retains original resolved CRAM/AGUN rules; `after.yaml` and
  `sequences.yaml` record the installed binding.

No live gameplay/firing capture yet. No process termination, original map edit,
commit, push or release. Nod/Sonic/Allies flak are NOT covered by this art approval.
