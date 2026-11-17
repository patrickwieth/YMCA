# Chinese Gatling — native1×1 reconstruction and local integration

## Latest supplied-platform follow-up

The rejected red/tan plinth has now been replaced by a native128px cut/paste platform
from `mount_base_flat_plat...-1843695069-0.png`. Background/ground are removed; original
bearing and source material detail retained. See `platform-cut/README.md` and
`platform-cut/pass-03/all32-native.png`. Base uses Scale1; turret pixels, Scale0.65,
rotation, mount and muzzle registration remain unchanged. Earlier model files and
runtime base are preserved. This is installed locally, without commit/push/release.

## Historical native reconstruction

`chgatling` now uses `chgatling.native`: a fixed armored plinth on the existing Soviet
1×1 bib, with a separately rotating twin-gun carriage rendered in32 directions.
The source folder contained32 enlarged directional PNGs,3a.png and additional JPEG
references, not ready-to-use transparent runtime sprites. Supplied files are unchanged.
`reference-review/source-hashes.json` records the32 numbered PNGs.

Current scene: `pass-03/chinese-gatling.blend`.
Reviews: `pass-03/review/comparison.png`, `pass-03/review/all32.png` and
`integration/rotation-native.gif` / `integration/all32-native-1x1.png`.
The latter composes the actual Soviet bib at runtime scale. These are native model
renders/compositions, **not live screenshots**. Source comparison removes only the
border-connected black background in a derivative; source files are never rewritten.

## Source interpretation and iterations

The32 source angles establish the paired white receiver housings, six-tube bundles,
red muzzle sleeves, black mechanisms and tan/red platform. Small feed components,
fasteners, cooling slots, bore interiors and hydraulic details are reconstructed/inferred.
The stationary platform deliberately does not rotate with the upper guns.

1. Establish chamfered armored platform, rounded receivers, individual hollow barrel
   tubes, recessed bores, bundle clamps, carriage and trunnions. Review exposed an
   overly empty central platform, weak lower mechanisms and insufficient red-paint range.
2. Close the azimuth deck, expose/lower the trunnions, add transverse linkage,
   ammunition feeds, cassettes, hydraulics, vents and casing seams; broaden the rim.
3. Recover saturated reds from the supplied views (the initial reduced palette omitted
   bright red samples), add rear rotary-drive motors, dorsal fins, yoke returns and
   fasteners. Review the complete rotation, source cardinal angles and actual1×1 size.

Materials use source colors and normal-dependent tonal steps, not procedural RGB noise
or whole-image projection. Receiver housings are curved volumes, not boxes. Each bundle
has six separate tubes and real recessed muzzle geometry. **Barrel-spin animation is
not claimed**: the bundles are static poses within the32 yaw frames, as in the previous
sprite weapon. The turret rotates and the two gun groups fire alternately.

## Runtime and gameplay preservation

- Direct RGBA files: `mods/ca/bits/china/buildings/gatling-base.png` and
  `gatling-turret32.png`,320×320 frames, Scale0.65.
- Base offset0,-75; turret offset0,-34; mount0,0,604. The plinth is placed slightly
  toward the upper part of the retained1×1 bib, rather than covering its front apron.
- Body/turret quantization now32 instead of the old body8; classic perspective fudge
  disabled for native128×64-grid registration.
- Muzzles623,-257,834 /623,257,834 lie on the upper bore of each rotary bundle.
- All eight AA/ground weapon stages now use the existing SharedBarrelArmament and
  actor-local SharedBarrelSequence, retaining the original weapon/name/conditions.
  The left/right phase survives stage changes, cooldowns and AA/ground target switches.
  No shared engine trait implementation was changed; Soviet Sentry remains isolated.
- Existing ChinaMGatt weapons are unchanged:12/8/4/2 reload stages, original damage,
  ranges, spin-up thresholds and targeting. AA is retained, unlike the ground-only
  Plasma Cannon. Ammo/reload-speed modifier, power, mines, costs, health, prerequisites,
  turning and construction behavior are unchanged.
- The original directional minigun flash selects visible source frames0 and2 from
  each six-frame facing (explicit16-frame remap, Stride2, eight facings), Tick40,
  Scale1 instead of the old oversized Scale4. Every odd source frame is EMPTY: the
  initial consecutive-frame draft would have been invisible after its first tick.
  The strengthened pixel-content check caught this before completion. Real
  Animation.PlayThen/Tick now verifies the visible two-frame selection survives
  the first40ms update and ends at80ms; all selected frames contain visible pixels. Projectile and flash share the chosen gun.
- Original icon/construction and Soviet bib remain. Damaged states reuse native art;
  no separate damage reconstruction is claimed.

Rules: `rules/china/gatling-art.yaml`; sequences: `sequences/chinese-gatling.yaml`,
loaded after the historical China/Scrin2x visual layer. Gameplay footprint remains1×1.
Selection bounds1448×1448/height79, decoration1472×1472/height79.

## Checks / remaining live review

The saved Blender model was reopened read-only: all fixed vertices stay identical
across32 rotations, every gun part is yaw-parented and the fixed/moving envelopes have
positive clearance. Split sprites match the independently rendered full assemblies in
the four control views. Sprite frames are unclipped and selection encloses the art.

`--check-chinese-gatling-art` loads actual native/damaged/dead/flash/icon/construction
assets, validates the eight profiles, shared phase and flash lifetime, and checks512
actual-grid muzzle projections (maximum0.132px). Native/source/rig/gameplay/weapon/bib
regressions, historical China/Scrin tests and disposable test-map loading pass.
Normal local build succeeds. Sentry, Shard and Plasma engine checks still pass.

Live firing/GPU review remains pending. Existing China display slots use the new actor
on a fresh launch; authoritative testmap1.oramap is untouched. No game/Blender process
was stopped. No commit, push or release.

Tools under tools/art/: model_chinese_gatling.py, review_chinese_gatling.py,
export_chinese_gatling.py, bind_chinese_gatling.py, audit_chinese_gatling.py.
Tests: tools/test_chinese_gatling_art.py.
