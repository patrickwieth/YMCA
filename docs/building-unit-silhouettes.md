# Unit silhouettes behind buildings

Built and installed locally in Game after checking no user game was running; the installed build passed the native probe and screenshot regression. Not a release. Reuses `WithRockOcclusion` and its 35% white tint for units hidden by building artwork. No building PNG, foundation, collision, visibility or gameplay rule is changed.

The occlusion union now includes visible building sprites sorted in front of each unit renderable. Masks sample actual opaque pixels, including scale, offset and rotation. Transparent PNG margins, translucent shadows, decoration sprites and units sorted in front are excluded. Indexed sprites use palette alpha; RGBA sprites use their alpha channel. Building sources are sampled afresh so animation/damage changes and removal cannot retain stale masks. Existing unit cloak and viewer checks still apply at creation and final draw.

`engine/OpenRA.Game/Graphics/SpriteRenderable.cs` exposes read-only sprite/projected-corner information through `tools/patches/sprite-occlusion-geometry.patch`. The patch is registered for fresh engine preparation. Building sprites and voxel building bodies/turrets are occluders; voxel unit bodies are also supported silhouette targets.

## Native verification

`tools/test-rock-occlusion.ps1 -Buildings [-Gdi] [-Baseline] -EngineDirectory ... -OutputDirectory FRESH_PATH` creates isolated diagnostic packages. Never add these packages to normal maps. The probe requires the existing explicit diagnostic environment opt-in; the runner sets it only for its own process.

Verified Allied and GDI refinery fixtures with sprite/voxel/infantry renderables, an unoutlined front unit, an actual fog-hidden enemy, cloak in the previously occluded tank position, movement into clear view, two zooms and building removal. The fixtures use different rear positions because their building depth offsets differ. Native screenshots were compared directly: the GDI tank is genuinely hidden before the change and whitens the covering building pixels afterwards, not just empty model bounding-box margins.

`tools/test_building_occlusion_pixels.py BASELINE.png FIXED.png` additionally asserts substantive visible brightening of the occluded tank and unchanged front-unit/grass pixels. Use the second, 1x-zoom screenshot from the GDI fixtures. Evidence is in `art-source/building-occlusion/validation/`.

## Voxel building occluders

`ModelOcclusionGeometry` rasterizes the actual textured voxel slice mesh, using the renderer's camera, scale, limb/HVA frame, rotation and attachment offsets. Transparent texels and holes are preserved; neither the rectangular model bounds nor its projected shadow becomes an occluder. `voxel-occlusion-mesh.patch` exposes the existing CPU vertex arrays and the current projected transforms without copying model geometry or reading back the rendered model framebuffer each frame.

A per-building weak cache stores a full pixel mask for the current pose; all units share it. Rotation, animation, offsets, scale, position or palette alpha changes invalidate it. A moving unit only extracts the intersecting region instead of rerasterizing the turret. Cache dimensions are bounded.

Native fixture: `test-rock-occlusion.ps1 -Buildings -Voxel [-Baseline]`. It uses the real ATWR missile tower, holds four turret orientations, verifies changed geometry masks and then exercises movement, cloak at the occluded position, zoom and removal. Unlike other baselines, its baseline keeps sprite occlusion enabled and disables only `VoxelBuildings`, so a working sprite base cannot disguise a missing voxel silhouette. `test_voxel_occlusion_pixels.py BASELINE_DIR FIXED_DIR` compares actual native pixels in all four orientations and checks unobstructed controls. Native captures were visually inspected. Evidence: `validation/voxel/`.

## China bulldozer coverage correction

The initial tests missed `CHNMCV`: it inherits `^ConstructionVehicle` / `^Vehicle`, not the combat/support/economic selection templates where the effect was attached. The user's bulldozer/China command-center case reproduced with `white=False`; this was a real missing production binding, not a zoom or installation assumption.

`^Vehicle` now supplies the render trait for construction and other non-combat vehicles as well. The resolved bulldozer rules differ only by this added trait. `test_vehicle_occlusion_rules.py` checks construction vehicles, the China Nukular variant and the tank control for exactly one resolved component. No selection priorities, health, footprints or movement properties changed.

Run the native fixture with `-Buildings -China`. Fixed fixtures do not inject an occlusion trait into the construction-vehicle template. The probe enumerates Mobile actors independently of whether they already have the effect, preventing missing bindings from disappearing from the test. The installed build passes actual bulldozer movement behind/out from the CC, removal and visibility controls. The before/after pixel test (`test_building_occlusion_pixels.py BEFORE.png AFTER.png china`) verifies the white bulldozer on the roof rather than just counting clip rectangles. Chinese evidence is under `validation/china/`. GDI and rock native controls were rerun successfully.

The existing rock and slope native probes also pass; the 148 engine tests pass. The protected Nod construction-yard image and all supplied art remain untouched.
