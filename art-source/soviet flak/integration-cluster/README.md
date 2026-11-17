# Soviet sprite flak — muzzle flash and four-airburst weapon

Implemented locally following the user's request. Approved pass15 building PNGs,
mount243, muzzle1337,0,1603, footprint and existing SAM are unchanged. The sprite
flak remains the separate test/editor actor, not a replacement production SAM.

## Flash

WithMuzzleOverlay restored with `muzzle-cluster`: stock gunfire2.shp, five frames,
Scale1, centered on the calibrated barrel mouth. It is an omnidirectional cannon
flash, not an incorrectly oriented SAM launch animation. The engine loads actual
flash frames and retains0.189px maximum muzzle-registration error across32 facings.
No separate sprite barrel recoil was added.

## Weapon and balance

AGUN resolves to one armament with ZSU-23, Burst1, ReloadDelay8, Damage4000.
Its two barrel positions alternate, not two simultaneous rounds per reload.

SOVIET-CLUSTER-AA has Burst1, ReloadDelay32: exactly one-quarter firing frequency.
The parent shell does NO damage, but now creates a central visible explosion.
Four ticks later (0.16s at normal speed) it dispatches exactly four
SOVIET-CLUSTER-PAYLOAD explosions, Damage4000 each, offsets(+/-384,0,0) and
(0,+/-384,0). Total16000 per32 ticks matches4000 per8 ticks before armor/modifiers.
At25 ticks/s this is one shell per1.28s instead of0.32s.

Payload inherits the Allied armor multipliers and Air-only damage filter. Existing
Soviet10c512 range and flak reports remain. The inner full-damage plateau is0c512,
covering all four0c384 offsets plus shell inaccuracy at the aim point. This is
intentionally wider inner splash than Allied flak, not identical overall balance:
cluster area coverage and burst damage differ, moving targets can evade, and
actual DPS depends on impacts/armor. No hidden parent hit doubles the budget.

New AirburstClusterWarhead uses the projectile's ACTUAL impact position, never
terrain-cell centers or the original aimed position. It preserves world Z,
shooter attribution, damage modifiers, weapon target and impact orientation.
Children invoke the normal weapon/SpreadDamage/CreateEffect pipeline directly;
there is no secondary projectile falling back to terrain. Child clusters are
forbidden to prevent recursive cluster chains. A central visual explosion is
followed by four spatially separated damaging puffs. There is no extra central
damage, preserving the existing nominal damage budget.

## Tests and launch paths

Both map generators now select SOVIET-CLUSTER-AA.Test for their disposable sprite
flak. This retains cadence and all four clusters and permits Air/Ground/Water for
Ctrl+click ground testing; default actor and actual AA payload remain Air-only.
The dedicated map was regenerated. The normal launcher regenerates its map on
startup. Original testmap1 SHA256 remains
`ec6d572076d907c54041cde75dcc42428d6bbd2f87ccf91361d909662c8f3bda`.

`--check-soviet-cluster-flak` executes the SAME dispatch loop used by DoImpact,
with test callbacks at four world heights (including0 and negative heights),
checking four distinct positions, identical altitude, preserved modifiers,
orientation/source/target and unmodified parent arguments. It also checks real
resolved cadence, AA filters, armor multipliers, payload budget and central overlap.
This is not a live combat simulation or an observed GPU firing test.

`--check-soviet-flak-sprite` loads muzzle/body/turret/construction states and checks
trait dependencies and all-facing registration. Python regression suites cover
the AA/test variants, source art preservation, both map paths and unchanged SAM.
Runtime CA build succeeded without stopping a process. Live visual/firing review
of the new flash and airbursts remains pending. No commit/push/release performed.
