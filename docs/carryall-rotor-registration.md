# Carryall rotor registration

Local fix, 2026-11-25. No release or deployment.

OCAR previously used terrain-projected `WithIdleOverlay` offsets on the fixed-pixel V22 voxel. On the 128×64 map grid this stretched the rotor separation horizontally and made attachment errors change with facing.

OCAR now uses the existing `WithVoxelRotorOverlay` (already used for Chinook). Both airborne and grounded pairs are registered to the V22 nacelle surfaces: model coordinates X=9, Y=±54, Z=10, model scale 0.5; calibrated offsets are `102,±611,81`. The inherited OCAR.Eagle and OCAR.Reinforce variants receive the same correction. Voxel artwork, rotor sprites, aircraft scale, speed, cargo offset and pickup/drop behavior are unchanged.

Validation:
- Actual native screenshots inspected at eight facings, both airborne and grounded; original misplaced rotors reproduced first.
- `--check-carryall-rotors` reads V22 voxel surface heights, resolves all three production variants, rejects duplicate legacy overlays and checks 32 orientations. Maximum attachment projection error: 0.122 pixels.
- Isolated fixture generator: `tools/prepare_carryall_rotor_probe.py INPUT.oramap OUTPUT.oramap`; run with `tools/test-handcrafted-map-runtime.ps1`. Fixtures disable autopilot, move starting bases out of view and clear resources only in the review package; never install them as gameplay maps.
- Evidence: `art-source/carryall-rotor-registration/`.

Only runtime YAML changed for the fix; the renderer already exists in the locally installed build. Restart the game to reload rules. Normal generated Battle Nexus packages do not need regeneration for this actor-rendering fix.
