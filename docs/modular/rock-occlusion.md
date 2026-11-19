# Rock occlusion: units and resources

Implemented in Game / isometric, approved after the ramp/occlusion correction,
merged to master and published in v0.97.5. See `../release-0.97.5-validation.md`.

## Reproduced defect

With the isometric depth buffer intentionally disabled, the resource overlay was
painted after all terrain. A crystal rooted on lower ground could therefore appear
on the projected roof/rim of a higher plateau, even though that upper cell had no
resource. The isolated baseline fixture reproduces this; simply deleting resources
near cliffs would hide the symptom by changing the playable map.

`OccludedResourceRenderer` retains the engine's resource state, fog memory, variants,
density updates, radar and tooltips. Unoccluded cells still use the original batched
terrain-sprite renderer and lighting. Only sprites intersecting a nearer elevated
surface use clipped geometry. No crystals are added, removed or moved by this fix.
Resource depletion clears both normal and clipped drawing; terrain revisions rebuild
cached projections. Editor rendering uses the same implementation.

## White unit silhouettes

`WithRockOcclusion` applies to combat, support and economic unit templates. It clips
normal unit pixels hidden by higher ground and supplies a white silhouette at 35%
opacity only in occluded regions. Front-facing/roof-top visible units keep normal
rendering. Actual clipped cliff sprites provide their opaque-pixel masks, including
holes and corners; a rectangular bounding box is not treated as solid rock.

The normal and white unit passes use complementary masks of the union of roof and
foreground rock coverage. Each source silhouette is drawn once, after the last
intersecting foreground rock rather than after each face separately or above the
whole world. They cannot reveal units hidden from the render player and
are suppressed for cloaked units, including detected/own cloaked units. Visibility
is checked both when constructing the silhouette and immediately before drawing,
including death/disposal and render-player changes. Existing actor visibility/render
modifiers remain in force. A valid player view is required;
this deliberately fails closed for a null render-player context.

The normal shadow is not part of a voxel silhouette. A small engine extension adds
`ModelRenderable.WithoutShadow`, preserving this setting across renderable copies.
Voxel replacement tints are premultiplied correctly, matching sprite translucency.
Pixel clips are unioned at framebuffer resolution when zoomed out, avoiding repeated
alpha blending along adjacent strips. Moving units recalculate their masks; stationary
unit ground geometry is cached until position/bounds/terrain change.

The engine patch also exposes the existing resource-layer drawing method to subclasses,
so ordinary resource rendering remains on its previous batching path. Both extensions
are in `tools/patches/rock-silhouette-model.patch`, included in the shared Windows/shell
patch list. Engine and mod must be rebuilt together; the normal Windows build does so.

## Correction after player feedback: hidden units and ramps

The first implementation was not correct in the reported gameplay cases despite
passing its original fixture. A later face could paint over a silhouette attached
to an earlier face, while normal and white coverage did not share the complete rock
mask. On ramps, the roof projection reused the current cell's projected center
(including its ramp center height) and consequently shifted other surface masks.

Corrected locally:
- Union foreground coverage once per source renderable and use complementary
  normal/white masks. Draw the white pass after all intersecting foreground faces.
- Project each roof from its own zero-height world center and actual corner heights,
  rather than inheriting the observer cell's ramp offset.
- Add a real four-direction ramp fixture with a fully covered tank, a tank stopped
  midway across the rim, unobstructed lower-ground and roof references, and tanks
  on two levels of each ramp.

Validation now compares actual native unit pixels, not merely the existence/count
of clipping rectangles (model bounding boxes include transparent margins). The
baseline-colored fully hidden body becomes visibly translucent white; the partial
body retains normal exposed pixels and white covered pixels. All eight ramp cases
retain their normal body color without white tint. Native pictures were inspected.

Evidence: `art-source/rock-occlusion-review/correction/` from
`.tmp/rock-slopes-baseline04` and `.tmp/rock-slopes04`. The separate movement, cloak,
fog and resource-depletion fixture also passes at `.tmp/rock-correction-regression`.
Normal Release build and 365 Modular tests pass. This correction was approved by
the player and is included in v0.97.5; the downloaded Windows package also passed
the native full/partial/ramp pixel comparisons.

```
powershell -File tools/test-rock-occlusion.ps1 -EngineDirectory ENGINE -OutputDirectory BASELINE -Slopes -Baseline
powershell -File tools/test-rock-occlusion.ps1 -EngineDirectory ENGINE -OutputDirectory FIXED -Slopes
python tools/test_rock_occlusion_pixels.py BASELINE FIXED
```

The earlier evidence below is retained as history, not proof that the reported
regression did not exist.

## Original native validation

The fixture contains a real raised plateau, lower green/blue resource cells, sprite
infantry and voxel vehicles behind it, normal units in front and on top, a non-owned
cloaked stealth tank, and a unit hidden in fog. Hold-fire actor initializers keep this
visual/privacy fixture from turning into a battle.

Final native run (`.tmp/rock-final09`), with matching baseline capture:
- Occluded resource drawing exercised without relocating the resource layer;
  an explicit resource-layer assertion confirms no real resources on the upper roof.
- Sprite and voxel silhouettes inspected in actual screenshots; translucent white,
  no white model shadows, visible front/roof units unchanged.
- Enemy cloak and fog cases have no silhouette output.
- Moving a previously occluded tank into clear view removes its silhouette.
- Two zoom levels rendered; native pixel-run partition checks pass without overlaps.
- Clearing resources in the isolated fixture leaves no stale clipped crystals.

The initial visual iteration exposed overly bright voxel replacement tints; corrected
before the final review. An intermediate fixture was rejected for invalid map-local
inheritance overrides; another exposed unintended combat in the test scene. The final
fixture validates its rules before launch and uses hold-fire initializers instead.
A later cloak-transition assertion initially read the previous frame's diagnostic
counter (`TickRender` runs before `PrepareRender`). It now inspects freshly generated
renderables for both cloak and movement transitions, with an additional draw-time
visibility safeguard. The owned fixture pins its camera to prevent desktop mouse
edge-scrolling from invalidating screenshots. These failed harness
runs are not counted as passing validation.

365 Modular tests and Windows engine-patch tests pass. Normal Game Release build
succeeds. Evidence is retained in `art-source/rock-occlusion-review/`.

## Reproduce

After building Game normally:

```
powershell -File tools/test-rock-occlusion.ps1 -EngineDirectory ENGINE -OutputDirectory FRESH_PATH
powershell -File tools/test-rock-occlusion.ps1 -EngineDirectory ENGINE -OutputDirectory OTHER_FRESH_PATH -Baseline
```

The opt-in diagnostic requires `YMCA_ROCK_OCCLUSION_PROBE=1`, set/restored by its
launcher. It runs with isolated support/settings/maps and stops only its own process.
The fixture's explicit depletion step is diagnostic only, not production behavior.
