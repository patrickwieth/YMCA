# Cliff fire fairness and firing-position movement

Implemented locally in **Game**; not committed, pushed, released or deployed to the tournament server. Original map packages and supplied artwork are untouched. `CliffFireControl` on the production World enables the policy; mods without that policy do not acquire terrain collision or firing-position searches from the engine hooks.

## Behavior

- Direct damaging energy weapons and conventional shots use the same logical terrain geometry. The beam/effect endpoint and impact position are clipped together; damage does not continue at the original target behind the obstruction.
- Swept segment tests prevent fast bullets/missiles skipping a cliff between ticks. Existing actor blockers remain separately controlled by their original `Blockable` settings.
- Ramps use `Map.DistanceAboveTerrain` and its interpolated surfaces, not rectangular cliff blockers or decorative rock sprites.
- The initial vertical tolerance is **362 world units** (half a native height step). The upper lip additionally has a narrow combat-only bevel: at most one height step at a cliff boundary, tapering to zero half a cell inland. Ramp cells are not beveled. Neither map elevations, movement terrain nor artwork is changed.
- Direct unit-versus-unit actor attacks also require a reciprocal body-center corridor. An overhanging barrel must not grant automatic one-way fire from a protected hull. Actual muzzle trajectory clearance is still checked. Hitboxes, damage, range and unit footprints were not enlarged or rebalanced.
- Cruise-height reference safety is now separate from obstacle planning: `CruiseRelativeToTerrain` prevents missiles diving into their own plateau without bypassing firing-position/stance checks. See `missile-height-policy.md`.
- High arcs are checked as arcs, not rejected by a straight ray or exempted by weapon name. Explicitly terrain-aware/steep-launch guided missiles retain their flight planning; ordinary low-launch missiles use a conservative direct firing corridor. Their actual flight still collides with terrain.
- Damaging ground-following sonic pulses now follow logical surface elevation and stop at steep faces, rather than traveling at absolute Z=0 underneath elevated terrain. Their visual height above the ground is retained.
- Non-damaging utility/healing effects and zero-range impact powers are not globally converted into terrain-blocked weapons. This is not a blanket change to every effect or support ability.

## Stances and orders

Terrain repositioning uses the attack activity's existing movement permission. No new stance or global chase rule was added.

- **AttackAnything:** searches for a nearby reachable position within weapon range with a clear firing corridor.
- **Defend / ReturnFire:** no automatic terrain chase; blocked targets are skipped rather than endlessly fired upon.
- **HoldFire:** no automatic attack or repositioning.
- Switching an ongoing *automatic terrain reposition* to Defend or a more restrictive stance cancels that movement. Explicit player attacks retain their usual permission to move.
- Explicit near and distant actor attacks can approach/reposition. Ordinary out-of-range movement runs before the local terrain-position search; a distant target behind a hill must not be mistaken for a failed local search.
- Force-ground fire remains an explicit trajectory shot: it may fire into the rock, physically impacts there and does not automatically relocate just because the corridor is blocked. It does not acquire actor-targeting body-corridor semantics.
- Search is bounded to a local 12-cell radius and failed attempts are rate-limited. No reachable local firing position means no blind march or endless shot loop. This is not an unbounded search around an entire mountain.

## Implementation

`CliffFireControl` supplies the optional `ITerrainFireControl` world policy. The engine patch `tools/patches/cliff-fire-control.patch` adds shared hooks in armament/auto-target and frontal/turreted attack activities, plus common Bullet/Missile/LaserZap/AreaBeam/Railgun/InstantHit and Cnc TeslaZap / AS ElectricBolt and RadBeam projectiles. CA BulletCA, MissileCA, LaserZapCA, PlasmaBeam and LinearPulse use the same service.

Beam clipping preserves the unblocked intended endpoint for later tracking updates. PlasmaBeam recomputes every segment from the clipped direction, not only its final endpoint. Railgun handles a zero-length terrain impact safely. None of this activates silhouette rendering on effects, mines or wrecks.

## Native evidence and regressions

Evidence: `art-source/cliff-fire/validation/`.

- Production RiftCannon: the old implementation damaged a low-ground target through a plateau; fixed force-ground shots were actually emitted but caused zero damage behind the obstruction.
- Native before/after pairs for LeecherBeam (PlasmaBeam), BeamCannon (AreaBeam), IonZap (Railgun), ThorBlitz (TeslaZap), StormColumnZap (ElectricBolt), SonicPulse (LinearPulse), RadBeamWeapon (RadBeam), and PrisLaserAim (LaserZapCA). Every baseline exercised real target damage; every fixed run blocked that damage.
- Native firing-position scenarios cover Channeler, medium tank, infantry and a low-launch missile weapon. Defensive/return-fire/hold-fire actors stay put; aggressive actors reposition and damage their targets. Explicit attacks override the automatic stance as before. A sealed, unreachable case neither moves nor damages the target.
- An actual stance switch during an already queued terrain move stops the unit; restoring aggression lets it resume. Distant explicit attacks are tested separately from the in-range cases.
- Close-range medium-tank exchange caught a genuine remaining asymmetry in the first tolerance-only version: the upper tank took no damage while killing the lower one. The narrow lip and body-corridor correction let **both tanks damage each other**, while deeply recessed positions remain protected.
- `--test-cliff-fire-geometry` covers opposite directions, eight cliff approach directions, edge versus deeply recessed endpoints, a genuine high arc and four planar ramp orientations.

Reproduce with `tools/test-cliff-fire.ps1 -EngineDirectory ... -OutputDirectory FRESH_PATH -Mode MODE`. Modes: `fixed`, `cannon`, `infantry`, `missile`, `plasma`, `area`, `rail`, `tesla`, `electric`, `pulse`; `radiation` and `laserca` cover the remaining direct energy families. Prefix a projectile mode with `baseline-` for the old policy. The baseline explicitly removes the production world trait. Fixed fixtures require the production trait and do not inject it. Diagnostic maps require `YMCA_CLIFF_FIRE_PROBE=1`; the runner scopes this to its own process. Never install these fixtures as selectable maps.

The final production binding and installed binaries passed native near/far attack, stance-switch and projectile checks. The 148 engine tests pass. `--audit-cliff-weapons` records all resolved damaging ranged projectile families, including the remaining gravity/parachute bombs (their own falling trajectories) and instant mine impacts (no traversing ray), rather than assuming one laser implementation covers every energy weapon.

## Limits

This policy uses real logical terrain height, not decorative cliff PNGs on otherwise flat terrain. Bounded local repositioning is not a guarantee of finding a remote ramp on the other side of a large mountain. Existing special/non-damaging support effects remain outside the damaging-projectile policy. These are explicit scope limits, not claims of universal weapon/ability coverage.
