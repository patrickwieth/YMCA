# Alternating twin Gatling muzzles

Requested: two muzzle flashes, alternating between the two Sentry Gatling clusters.
Implemented locally in Game/modular; no art changes, commit, push or release.

- Read-only Blender projection of each six-bore cluster from the frozen native model.
  LocalOffsets547,86,655 and547,-86,655; original turret attachment unchanged.
- SharedBarrelArmament inherits normal Armament and substitutes only the barrel at
  FireBarrel. SharedBarrelSequence keeps a synchronized actor-wide phase. All four
  ground Gatling stages use it, so switching stage/target or cooling down does not
  restart separate barrel counters. Projectile and flash use the same chosen barrel.
- Chinese weapon definitions, spin-up controller, damage, reload and ground-only
  targeting remain unchanged. Exactly one shot, not two simultaneous weapons.
- Follow-up fix after live feedback: two frames at40ms (80ms total). The previous
  two20ms frames expired inside one engine40ms update, hiding the flash before
  rendering. The real Animation.PlayThen/Tick is now tested headlessly: flash must
  survive the first tick and expire at the second (fastest shot interval80ms).
- New trait restricts configuration to two offsets, Burst1 and FireDelay0.

Normal build succeeded and installed the DLL; no OpenRA process was running at
that time. Engine checker verifies shared phase, four ground stages, loaded flashes
and both muzzle points in all32 directions (max0.371px integer-engine error).
Sentry, Soviet SAM isolation and Nod SAM regressions pass. Original turret/base
pixels are preserved. all32-dual-muzzles.png markers were visually inspected;
this is a composed placement diagnostic, not live firing footage. Live review pending.

Scripts: tools/art/calibrate_sentry_dual_muzzles.py (background Blender, read-only),
tools/art/review_sentry_dual_muzzles.py. Earlier central muzzle documentation is
historical; current binding is the twin-barrel version above.
