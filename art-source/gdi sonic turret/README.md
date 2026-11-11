# GDI Sonic Turret — modeled/textured native 32-direction study

**User rejected pass-05 visually. It is NOT approved or selected for integration.**

Rework started: pass-06 replaces the extruded-photo receiver and projected ring
textures with new solid geometry and bounded UV islands, but is still too plain.
Further pass-07 receiver/texture/ring corrections are written in
`tools/art/small_turret_rework.py` but have NOT yet been rendered/reviewed. Work was
interrupted by the power-plant inputs and Nod laser request. Technical checks do
not establish visual approval.

The links and implementation record below describe the **rejected historical pass-05**.

- [Rotation on native 1x1 bib](pass-05/review/rotation-with-1x1-bib.gif)
- [Source / model / opposite view](pass-05/review/source-comparison-2x.png)
- [Eight directions](pass-05/review/eight-views-2x.png)
- [All 32 native views](pass-05/review/all-32-native.png)
- [Separate actual parts](pass-05/review/separate-parts-2x.png)
- Model: `pass-05/sonic-source-model.blend`.

Original `sonic_turret-1863943949-0.png` is unchanged, SHA256
`98e7adabaa762627d1cb08d3c8d35fec818bf9835207b80596019e5a58483134`.
The user confirmed this source, not a second copy of the existing Ion Defense.

## Actual construction

Seven stationary meshes form the completed pedestal, vents and fixed bearing.
Thirty-two rotating meshes form the closed receiver, paired support arms, cross
brace, axle, central barrel, three actual annular emitter collars, rim geometry,
rear reinforcements and recessed two-tone cyan lens. The saved model contains
2,542 vertices / 2,241 faces, all meshes UV-mapped. This is actual solid geometry,
not three billboard discs or a voxel approximation.

The supplied image is cropped at the top/right/bottom. The study completes those
edges and unseen sides with geometry, source cuts and source-palette materials.
Rear radial braces and repeated source panels are explicitly inferred, not observed
reference details. No procedural noise or invented RGB textures are used.

## Iterations

1. Source-calibrated model, independent base/turret, eight views. Review exposed
   clipped rotations, plain back surfaces and source lens color bleeding onto
   other projected rings. Initial clipped previews are retained, not selected.
2. Broader receiver/support connection, source-front-ring UV atlas instead of
   projecting the foreground lens onto every ring. Outer pedestal geometry was
   reduced in XY (not image-scaled) to fit 1x1; the support bearing stayed intact.
3. Fixed a real geometry error: the central barrel cap was in front of the recessed
   lens. Shortened that barrel so the actual cyan lens becomes visible. Added
   concentric/radial rear reinforcement and fitted all 32 yaw angles in a larger
   transparent canvas at unchanged native density.
4. Corrected roof UV selection using the actual profile normals, replacing blank
   fallback gold with source armor detail. Made rear reinforcement edges readable.
5. Base holdout during turret export for proper separate-layer occlusion. Five
   independent full-3D renders match their raw base/turret composites exactly.
   All 32 final directions were inspected.

Earlier blends/renders remain. The inferred roof/backs still show some texture
stretching and simpler detail than the reference; this is not labeled a perfect
finished match. The native review demonstrates the actual size and overhang.

## Native export and 1x1 target

`review/base.png`: one 256x256 frame; `review/turret32.png`: 2048x1024, 32 frames.
**256px is canvas padding, not a 256px-wide foundation.** Density remains
20px/model-unit and the preview uses the original 128x64 one-cell bib. Measured
stationary XY extent 2.208 is below the one-cell limit 2.26274. Tall/overhanging
weapon art is not forced into a 128x64 total sprite-height restriction.
Source angle0 points SE; engine frame i uses render index `(12+i)%32`, preserving
counterclockwise N/W/S/E ordering without flipped pixels.

Engine PNG loaders pass with embedded FrameSize/FrameAmount metadata. Geometry,
stationarity, 1x1 fit, native size/palette/alpha, frame ordering and full-render
composition are tested. No gameplay actor or weapon/balance binding is installed;
no live game appearance/firing claim. Existing Ion Defense is untouched.
The user's open Blender scene, original source and protected Nod art were untouched.
No commit, push or deployment.

See [shared workflow and selected previews](../gdi-small-turrets.md).
