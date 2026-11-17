# Fixed GDI EMP launcher — implemented locally

User supplied loaded/empty PNGs and requested a fixed launcher: no rotating turret,
missile disappears on use, launches along the pictured rail and subsequently homes
toward the target. This was initially only discussed; it is now implemented.

## Art and fixed mounting

PATR directly references `emp launcher.png` / `emp launcher empty.png`, native256,
Scale1, Bottom offset0,16. The2×2 gameplay footprint is unchanged. Following user
feedback, the decorative GDI bib is now3×3, offset0,32: the building occupies its
rear2×2 area, leaving both front edge strips (five cells) nonblocking. This is only
an idle overlay, not WithBuildingBib or a3×3 building reservation. Selection
geometry remains2534×2534/height165.
`calibration.json` records original hashes and source geometry. No supplied pixels
were overwritten. `loaded-empty-3x3-passable.png` shows current placement and free
cells; the older `loaded-empty-placement.png` retains the initial2×2-bib review.
Both are offline composites, not screenshots.

The first projectile was extracted from the loaded/empty image difference and
rotated as a2D sprite, not modeled in3D. That study and its extraction regressions
remain archived. The user confirmed that flight works, but preferred the old
projectile's authored rotations: runtime now selects `thawk` / `thawk.shp`, its32
existing directions and original player palette. No voxel missile was created.
The fixed start angle, muzzle, homing delay and flight parameters are unchanged.

## Ready / empty cycle

PATR uses one fixed WithSpriteBody, default sequence `empty`. Its loaded image
is enabled by `loaded-rocket && !build-incomplete`, keeping construction intact.
AmmoPool starts empty and holds one rocket. New
`ReloadAmmoOnSupportPowerCharged` refills it on the real support-power Charged event,
not an independent timer. AmmoPool consumes it on actual firing; the loaded idle
animation immediately cancels back to the empty default. The rocket returns when
the ability is ready again. Original4500-tick ability charge interval is retained.
The building has only this one support power; the reload trait is used only here.

## Flight

Removed Turreted / embedded rotating body; AttackOmni fires without turning.
New `THLauncher.Fixed` inherits every EMP warhead and weapon range/reload/sound
fromTHLauncher, replacing its ballistic Bullet with MissileCA. Old THLauncher is
unchanged for any other users.

- Fixed armament yaw256 (engine west), muzzle0,-931,3249 matches visible rocket
  center86.866,32.393 relative to building ground128,176 (0.016px projection error).
- Fixed launch pitch72 (~25.31 degrees), initial speed180. Engine projected velocity
  aligns with source missile axis with dot product0.999892.
- Six initial flight ticks, then homing; max speed300, cruise altitude4c0.
- Point inaccuracy removed for the requested guided arrival; damage, EMP duration,
  effect radii, power cost, build price, charge interval and footprint remain intact.
- MissileCA gets an opt-in first-frame velocity-facing flag, enabled only for this
  launcher, so the initial sprite does not flash the default north-facing frame.

## Validation and limits

Actual runtime CA assembly built successfully into engine/bin (no process stopped).
`--check-emp-launcher` loads native loaded/empty/projectile/construction sequences,
resolves actor dependencies and validates fixed launch/muzzle math. Python tests
check original hashes, clean missile extraction, no rotating turret, readiness
wiring, all unchanged warheads and retained gameplay fields. `rules-before/after`
and `weapon-before/after` retain the resolved records.

User confirmed initial centering and guided flight in-game. New3×3-bib / restored
legacy projectile visuals and actual driving across the free strips still need
live review; no claim of exhaustive near/far/behind, capture or selling checks.
Restart the game for the new assembly and YAML. Local only, not committed/pushed.

Soviet flak muzzle flash and the slower four-airburst weapon are now implemented
separately; see `art-source/soviet flak/integration-cluster/README.md`.
