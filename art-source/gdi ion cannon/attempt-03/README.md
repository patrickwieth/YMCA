# Heavy Blaster V3 — rebuilt and reviewed through five passes

V1/V2 were rejected for wrong proportions, sparse/distorted detail and the open
rail shape. V3 is a new camera-calibrated model, not another overlay on V1.

## Review

- [Reference / rejected V2 / current V3](export/comparison-2x.png)
- [Stationary base / separate turret / assembled result](export/separate-parts-3x.png)
- [32-view rotation](export/rotation-preview.gif)
- [All 32 views](export/rotation-contact-sheet.png)
- [Five reviewed modeling passes](iteration-contact-sheet.png)

Current model: `pass-05/heavy-blaster-v3.blend`.

## What changed in the loop

1. Rebuilt the closed barrel/receiver, paired arms, side armor, monitor, capacitors,
   base and socket from calibrated source outlines. Source details are preserved
   by nearest-sampled UVs on solid extruded profiles and horizontal plating.
2. Restored missing front-deck seams/rivets; corrected support/capacitor depth so
   the socket does not swallow their feet. Rebuilt the cross-brace on the correct
   physical plane instead of the wrongly sloped side-profile construction.
3. Inspected rotations, removed foreground objects baked into the housing texture,
   and added inferred opposite-side armor/monitor and mirrored physical supports.
   This pass regressed some main-view detail and was not accepted as final.
4. Masked only the actual source foreground occluders instead of blanking the
   entire housing face. Restored its real detail, removed alpha holes through solid
   geometry and stopped projecting unrelated image regions onto hidden edges.
5. Added an inferred rear service panel from existing source detail and an actual
   recessed muzzle face with collar/inner walls. Rendered all 32 native directions.

The front comparison is now substantially closer. It is **not proof of complete
multi-view reconstruction**: observed faces are camera-registered source UVs. The
rear, hidden faces and opposite details are inferred/reused from visible source
panels. Some supports/edges remain simpler than the drawn reference. No procedural
wear, noise or generated color texture was used; the generated occlusion mask is
only a binary material-selection mask, not RGB detail painting.

## Actual part separation

The blend has two collections:

- `STATIONARY BASE`: plinth, plating and fixed socket, with no turret parent.
- `ROTATING TURRET`: gun, receiver, arms, caps, monitor, capacitors and cross-brace,
  parented to `turret_pivot`.

Exports are separate assets, not merely layers in a comparison image:

- `export/heavy-blaster-base.png`: one 160x160 frame.
- `export/heavy-blaster-turret32.png`: 32 frames of 160x160, 1280x640 sheet.

Both have embedded PNG `FrameSize`/`FrameAmount` metadata. They use the same canvas
and documented pivot, so base + turret can be overlaid without guessing offsets.
A Blender control render at 0 and 123.75 degrees verifies the fixed base's pixels
and world matrices do not change. Two-layer compositing differs from the whole
3D scene at only seven pixels in the reference-facing native view (depth edges).

All game-sized images are rendered directly at **160px** and intended for Scale 1.
They are not reduced 320px renders. Only review pictures/GIFs use nearest display
enlargement. Final visible pixels belong to the original palette; alpha is binary.
The 32 frames are ordered using engine counterclockwise N/W/S/E, with the reference
SW pose at frame 12 / facing 384. The base never rotates.

## Validation / integration boundary

`tools/test_heavy_blaster_v3.py` checks original preservation, all five saved
passes, Blender part-separation controls, embedded metadata, all 32 distinct native
renders, source palette, alpha, clipping, facing convention and separate-layer
composition. Four V3 tests plus two retained V1 tests pass.

**Not installed into gameplay yet.** Actor/bib offsets, selection envelope, weapon
and live engine appearance still need integration and review. No source image,
existing building, original test map or previous attempt was replaced.
The protected Nod construction-yard blob remains unchanged.

All Blender processes were background processes; the user's open/unsaved scene
was not touched. No game restart, commit, push, release or deployment performed.

## Reproduce from Game

```sh
blender.exe --background --python tools/art/model_heavy_blaster_v3.py -- --pass 5 --rotation
blender.exe --background --python tools/art/check_heavy_blaster_separation.py
python tools/art/pack_heavy_blaster_v3.py
python -B -m unittest discover -s tools -p 'test_heavy_blaster*.py'
```

The earlier review passes are retained under `pass-01` through `pass-04`.
