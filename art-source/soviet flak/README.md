# Soviet flak — native 1x1 reconstruction in progress

The user supplied this artwork and explicitly requested a **1x1** construction.
The supplied cutouts are `flak base.png` (161x90) and `flak.png` (163x147), not256px
wide. `Create_a_new_pixel_a...-475793982-0.png` is the separate1024px full reference.
All originals remain untouched. Both cutouts have fully opaque near-white backgrounds.
The existing runtime `mods/ca/bits/soviet/buildings/flak base.png` is a distinct
alpha-cleaned source and has also been left untouched.

## Muzzle correction after user feedback

The sprite flak's muzzle was projected using a false64px horizontal tile width.
The actual grid is128×64. `FLAK.SOVIET.SPRITE` now disables classic perspective
fudge and uses LocalOffset669,0,1603 (attachment0,0,243 unchanged). Main bore tip,
not either auxiliary hydraulic tube, is the reference. Weapon stats/recoil, sprites,
faction gates and the retained voxel prototype are unchanged. Checker now reads
actual engine TileSize/TileScale; all32 directions pass with0.080px maximum error.
`tools/art/review_soviet_flak_muzzle.py` produces
`muzzle-registration/all32-muzzle-markers.png`: red=old misplaced origin,
green=corrected muzzle. All32 marker views were inspected. Not a live screenshot;
live firing confirmation remains pending. No running game was stopped.

## Current runtime: rendered sprites, not voxels

User rejected the voxel presentation. Pass14 now exports32 rendered turret facings
plus a fixed pedestal. `FLAK.SOVIET.SPRITE` is bound in both building test launch
paths. Native engine sprite/sequence/muzzle checks pass; live review remains open.
No separate barrel recoil in this version. Old voxel work is retained for reference,
not selected by the updated test maps. See `integration-sprite14/README.md`.

## Size contract

- Native20px/model-unit, Scale1. No runtime scaling or source PNG resizing.
- The fixed cross-foot base is constrained to the actual128x64 isometric ground
  diamond, not simply a128px rectangular image width.
- The builder verifies every fixed mesh vertex in world coordinates, including
  transformed outrigger beams: `max(abs(x),abs(y))*20*sqrt(2) < 64`.
- Frames are192x192 for transparent rotation/height padding. Frame width is NOT the
  footprint. The elevated cannon may overhang the ground square.
- Separate stationary base and rotating turret; ground origin96,136; pivot0,0,.62.
- Source points forward-Y; engine-convention sheet starts at model facing20.

## Current: pass13 geometry and real voxel integration test

The two parallel auxiliary tubes now sit farther outside the main receiver and
extend farther forward, with their clamps/rods following them. Pass13 adds actual
small bores and red terminal bands to those tubes. Real VXL/HVA parts
for base, turret and barrel are now installed on the isolated
`FLAK.SOVIET.VOXEL` actor, with axial runtime recoil and one pose per component.
See [voxel implementation, checks and test-map instructions](voxel-01/README.md).
Existing SAM is not replaced. Live appearance/firing remains unobserved.

## Historical study: pass11 — separate recoil group

The barrel, its jackets/red bands and the two sliding upper rods now belong to
`barrel_recoil`, a child of the rotating turret. Receiver, sleeves, yokes and base
remain still during firing. A quick0.24-unit kick travels backward along the actual
42-degree barrel axis, followed by a slower return. No mesh shape or rest placement
changed: background-Blender regression compares every mesh with pass10 and verifies
only the named barrel parts move, across all32 turret directions.

A small amount of authored coating wear now breaks up painted steel on the yokes,
shield, receiver and base legs. Original source palette remains enforced. No RGB
noise or copied mechanical details; no BRDF-roughness claim for the emission-based
pixel rendering.

Animation preview: `pass-11/recoil/recoil-four-views.gif` (four raw-render views).
`recoil.json` documents membership/axis/travel; `recoil-check.json` contains the
geometry/motion regression. Five current Soviet study tests pass.

**Not installed in game.** Stock `WithSpriteBarrel` applies recoil along horizontal
local X. This inclined barrel needs an angle-aware runtime binding or rendered
recoil states, including correct changing overlap. Merely setting Armament.Recoil
would not reproduce this animation. Existing SAM rules are untouched.

## Previous internal study: pass10

Further implementation after the pass03 blockout:

- Pass04 replaces bolt-like dots with connected solid tooth/root-ring gears.
- Pass05 exposes bolts previously buried under outrigger beams, adds leg identifiers,
  seams and collar fasteners, and protects small red marks in contour finishing.
- Pass06 adds real receiver clamps/bolts, rear locking ring and articulated linkage.
- Pass07 corrects the main cannon elevation from34 to42 degrees after comparison
  with the supplied barrel slope. No actor/weapon has been bound.
- Pass08 gives the lower auxiliary cylinder its own near-horizontal axis instead of
  wrongly making it parallel to the main cannon; adds its red terminal and mounts.
  Its mechanical interpretation is inferred, not a decision about game weapons.
- Pass09 seats the buried rear locking ring on the actual closure surface, adds the
  source's rear red badge and fixes the opposite-side badge winding.
- Pass10 gives the feet/legs8% more XY spread to better balance the source proportions,
  without widening the bearing socket or touching the rotating pivot. Independent
  world-vertex audit confirms60.789px versus the64px ground-diamond limit.

All32 native facings rendered and visually inspected. Geometry audit verifies
outward closed meshes, fixed/moving parentage and the actual hollow main bore.
Latest source/palette/alpha/frame-order/occlusion regression tests pass. These checks
and reaching10 passes are not user approval or runtime integration. Further material
fidelity remains possible; do not solicit feedback on the old pass03 again.

## Earlier internal working studies

Pass01 builds a real hollow single cannon, paired recoil sleeves, yokes and gears
above a stationary four-foot foundation. Pass02 exposes the previously buried brass
collars, aligns the rear closure with the elevated receiver, adds lower drive gears,
service plates, reinforcement and return struts. Pass03 closes the missing lower
front shield, adds its red star/fasteners and introduces restrained tonal shading.
All32 directions are rendered. Side/front/reverse rotation views were inspected.

This is an early reconstruction, **not a finished-art presentation or approval**.
Surface detail and source-like material treatment still need substantial work;
technical size checks do not replace that work. Hidden mechanics remain inferred.
The source palette combines both supplied cutouts, excluding their white background.
No background pixels are projected onto the model.

Scripts: `tools/art/model_soviet_flak.py` and the shared
`review_gdi_small_turrets.py --kind soviet`. Current geometry/source hashes are in
`pass-11/geometry.json`. `test_soviet_flak_study.py` checks source preservation,
1x1 size contract, native palette/alpha/frame order, stationarity and five exact raw
base+turret/full-3D composites. No engine-loader or live gameplay test is claimed.

No SAM actor/rule/footprint/weapon change, runtime art promotion, map edit, commit,
push or release. Runtime integration remains separate from this sizing/model work.
