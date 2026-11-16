# Scrin Plasma Cannon — native1×1 ground turret

## Installed locally

PTUR now uses `ptur.native`: a stationary rooted foundation and independently rotating
32-facing upper organism reconstructed from `photon_cannon-1201896784-0.png`.
The supplied source is unchanged (SHA256
`5a73a2d00809ea3f06d5cd057d131f436bface291efa76750c579df3c11eea7f`).

Current scene: `pass-05/plasma-cannon.blend`. Earlier passes remain intact.
Review: `pass-05/review/comparison.png`, `pass-05/review/all32.png` and
`integration-pass05/rotation-native.gif`. These are native model renders/compositions,
**not live game screenshots**. Hidden/rear anatomy is inferred from the single source view.

## Geometry and visual review

- Rooted chitin legs, layered shoulders, exposed inter-leg mechanisms, bounded source
  shin cuts, blue knee nodes and tapered ground tendrils are entirely stationary.
- The curved cervical supports, cyan conduits, closed vascular returns, receiver,
  crescent housings, upper reservoir, plasma capsule and jaws all belong to yaw.
- The emitter is a convex volume inside a recessed collar/cage, not a pasted disk.
  The upper reservoir is domed and held by curved crescent housings.
- Source-color directional shading and bounded source armor cuts preserve the material
  family. No whole-reference projection, random RGB noise or altered original image.

Iterations addressed actual findings:
1. Separate base/yaw anatomy, swept claws, receiver, collar and spherical emitters.
   Review exposed inward orb/collar normals, overly long receiver and blank regions.
2. Correct normals, overlapping receiver petals, gills, cervical ribs and energy nodes.
3. Shorten the upper receiver in depth, expose anterior cervical returns, recover source
   shin detail and improve knee-node visibility rather than burying it inside the socket.
4. Layer the foundation shoulders, add vascular seams, open the emitter aperture and
   strengthen source-derived chitin tonal detail.
5. Replace the closed forward body with a longer exposed plasma capsule/cage, add
   serpentine conductors/control nodes, and apply a bounded original receiver plate cut.
   Canonical, side, rear, end-on, full rotation and runtime-sized views reviewed.

The source-facing pose is model yaw16; the engine compass still packs `(4+i)%32`.
The saved model was reopened read-only: every fixed vertex remains unchanged through
32 rotations and every upper mesh is parented to turret_pivot. Vertical separation
is0.160 model units, sufficient for every yaw because rotation preserves Z.
Split base+turret renders match independently rendered complete assemblies exactly
in the four control views. Frames are not clipped and selection encloses both parts.

## Runtime

- `mods/ca/bits/scrin/buildings/plasma-cannon-base.png` / `plasma-cannon-turret32.png`.
- Native320×320 RGBA frames; Scale0.58. No indexed source-palette mismatch.
- Base offset0,-72; turret offset0,-20; mount0,0,681.
- Two existing burst slots now originate on the same plasma aperture:
  LocalOffset362,-22,726 and362,22,726. Classic perspective fudge disabled.
-1×1 occupancy retained; mouse bounds1448×1448/height72, decoration1392×1392/height72.
- The old bib is removed; the native rooted foundation provides the ground attachment.
- Existing PlasmaTurretGun retained: ground/surface targets (Ground, Water), **no AA**,
  two-shot burst,4-tick spacing,15-tick reload and unchanged damage/range.
- Cost, health, power, prerequisites, turning, targeting conditions and death behavior
  remain unchanged. The original flash, its Scrin palette, icon and shared construction
  are inherited. Damaged states reuse this art; no separate damaged model is claimed.

Rules: `rules/scrin/plasma-cannon-art.yaml`; sequences: `sequences/scrin-plasma-cannon.yaml`.
Both load after the historical China/Scrin2x registration; the latter is superseded
only for PTUR's visuals. The disposable building test already has PTUR and uses it
without changing the authoritative testmap1.oramap.

## Verification and remaining review

`--check-scrin-plasma-art` loads native/damaged/base/flash/icon/construction sprites,
checks targeting and tests64 actual-grid muzzle projections (max0.177px).
Python preservation/rig/compass/occlusion/selection/gameplay/weapon tests pass.
China/Scrin visual regressions and disposable faction-map engine loading pass.
Normal local build and direct engine checks pass. Live firing/GPU confirmation is
still pending. No running game was stopped; no commit, push or release.

Tools: `model_scrin_plasma.py`, `review_scrin_plasma.py`, `export_scrin_plasma.py`,
`audit_scrin_plasma.py` under tools/art/; `tools/test_scrin_plasma_art.py`.
