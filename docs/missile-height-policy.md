# Shared missile cruise-height safety

Local Game development only; no release/push/deployment. This extends the initial
MLRS-AA fix in `mlrs-aa-flight.md`. A plateau did not break every shot: launch
height, target height, distance and the cruise/hitting phase determine exposure.

## Audit and reproduction

`--audit-missile-height` examines resolved MissileCA/Missile definitions and actor
armament bindings, including inherited and upgraded weapons, rather than grepping
individual YAML entries. The prior MLRS-AA correction protected four variants;
many other missiles still interpreted cruise altitude as height above world zero.

A uniform-plane native capture isolates this defect from genuine cliffs. With
four terrain levels (Z=2896), examples that hit on flat terrain but struck their
own plateau far short of the target included HQ-7 ground rockets, upgraded IFV AA,
Nod Rocket Hail bike rockets and Scrin Atomizer bolts. Higher terrain also exposes
weapons with a larger nominal cruise altitude. Aircraft-only weapons tested from
a synthetic ground launch are diagnostic geometry cases, not claims about their
normal unit deployment.

## Final implementation — separate height reference from obstacle permission

- `MissileCAInfo.CruiseRelativeToTerrain` defaults to true in the CA-specific class.
  It supplies local physical terrain altitude during launch/cruise calculations.
  New MissileCA weapons inherit protection without needing a per-weapon repair.
- **TerrainHeightAware still defaults to false.** It remains the separate explicit
  forward-lookahead/incline-avoidance option. Making it universally true would also
  exempt ordinary missiles from the existing conservative firing-position policy;
  that broader behavior was not retained.
- The common engine `MissileInfo` gets the same cruise-reference option, default
  **false** for other mods. CA enables it on the engine-Missile roots for AP bombs,
  CyCannon, Scrin aircraft/bike/stealth-tank torpedoes, Atomizer bolts, Scrin depth
  charges and Overlord plasma. Their variants inherit it.
- Engine Missile lookahead also uses `position.Z - DistanceAboveTerrain(position)`
  instead of assuming each logical height step is 512 world units. CA's isometric
  steps are 724. Initial terrain sampling no longer invents a zero-height ledge.
  The registered `missile-physical-height.patch` carries these engine changes.
- Existing water-bound torpedoes keep their old flags and sea-level behavior.
- Damage, reload, weapon range, fuel, speed, accuracy, point defense, physical cliff
  collision and ordinary ground-target firing/stance rules were not changed.
  The earlier explicit MLRS-AA terrain-lookahead flag remains in place.

This fixes an invalid cruise reference, not all possible misses. A missile may
still hit a real obstacle, lose/intercept its target, scatter, or exhaust fuel.
Existing flat-ground range/fuel behavior is deliberately not rebalanced here.

## Validation and guard

`--audit-missile-height check` fails if a non-water missile has neither relative
cruise height nor explicit terrain awareness. This also guards future definitions
using the common engine Missile implementation. `CheckMissileCruiseHeight` is also
automatically discovered by normal rules lint; the audit command verifies discovery
and negative controls, not just the current catalog. All current default definitions
pass. Arbitrary external map-local weapon overrides are outside the default catalog
and require their own lint/test run.

`--create-missile-height-probe OUTPUT HEIGHT [water]` creates disposable uniform
plane fixtures; HEIGHT is 0..12. `MissileHeightProbe` requires the scoped environment
variable `YMCA_MISSILE_HEIGHT_PROBE=1`. Run the isolated native harness with
`-WaitForResultFile missile-height.json`. Its Passed flag means capture completed,
not that every projectile must hit; comparisons are made by the regression tests.
It instantiates real projectiles from copied resolved infos, disabling only random
inaccuracy and lock-on failure in those diagnostic copies. Normal speed, range,
fuel, steering and terrain policy are retained. Ground and airborne launch/target
geometries are exercised where appropriate. No production weapon data is mutated.

Evidence: `art-source/missile-height-policy/`.

- Before/after flat-plane results are identical, including termination positions
  and ticks. Existing flat-ground misses remain visible rather than being hidden.
- At levels 4, 8 and 12, no captured shot terminates far from its target below its
  own plane. Every flat-ground success remains a near-target termination at height.
- Four water-bound torpedoes reach their surface targets at altitude zero.
- A separate map-local Atomizer override exercises explicit engine lookahead at
  height8; it is not installed into production rules.
- Actual MLRS.AA versus stationary/moving Orcas still passes after the shared fix.
- The native low-launch missile cliff/stance fixture passes: blocked shots, hold,
  defend, return fire, aggressive repositioning, explicit attacks and unreachable
  positions retain their established behavior.
- The isolated build, resolved-rule guard, Python evidence regressions and Windows
  engine patch application/idempotence tests pass. No user's running game was stopped.

The resolved audit comparison preserves every previously captured field (including
TerrainHeightAware); only the new relative-height field is added. This is focused
regression coverage, not a claim that every weapon or future map is bug-free.
