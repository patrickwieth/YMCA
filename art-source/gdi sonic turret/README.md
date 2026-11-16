# GDI Sonic Turret — modeled/textured native 32-direction study

**User rejected pass-05 visually. It is NOT approved or selected for integration.**

## Current requested hardware recovery: pass22

Pass21 added rear-to-side paired pipes/couplings, receiver cable loops, two fixed
supply leads on the pedestal, and an actual concave small radar dish with a feed.
Pass22 seats the lower pedestal leads in visible glands, rather than leaving their
ends outside the shell. Back-side pipe routing is a documented mirror inference.

The big emitters, housing, yoke and existing pedestal geometry remain identical to20;
only the three old small radar placeholders were replaced. Fixed supply leads enter
below the rotating bearing and remain stationary. `hardware-geometry-check.json`
records the scoped comparison.32 native views rendered and visually inspected.

Contour finishing now uses an object-local front mask: upper/rear geometry keeps
its dark contour, while forward-facing regions receive the brighter single-pixel
edge. This follows the gun as it rotates instead of globally outlining all armor
with bright pixels. IDs use two channels; the third carries the front mask.

The user subsequently approved installation. Pass22 is now bound to STWR with
independent fixed base/cables and rotating turret, using byte-identical approved
PNGs. See `integration-pass22/README.md` for placement, muzzle registration and
engine checks. Existing unlock conditions/gameplay remain unchanged. Live review
is pending. Preview: `pass-22/review/eight-views-2x.png` and `all-32-native.png`.

## Previous material follow-up: pass20

User requested more mottled coating and brighter single-pixel contours like the
pedestal. Pass20 adds irregular small paint islands and light chip lips, plus a
single native highlight pixel just inside upper/left geometry boundaries. Dark
silhouettes remain; cyan/core and tiny details are protected. Original source
palette only. This finish affects the turret, not the pedestal: base PNG bytes
match19, and all32 alpha silhouettes match19. All mesh geometry still matches15.
See `pass-20/review/eight-views-2x.png`; not runtime-integrated.

## Previous material follow-up: pass19

After the requested Allied-flak installation, pass19 further broadens the coating
range, introduces restrained warm worn-paint tones, extends bounded original-paint
patches and adds sparse authored edge chips/short streaks. No random RGB noise.
All214 mesh geometries/transforms still match15 exactly; the nested dishes and
improved body shape are preserved.32 views rendered; native study checks pass.
No Sonic runtime integration or overall artistic approval claimed.

## Previous material follow-up: pass18

User positively evaluated the improved form but again found the broad textures
monotonous. Pass16-18 add coherent coating-tone regions and bounded paint variation
sampled from the original gold shoulder, at20 texels/model unit. No repeated sensor,
bearing or mechanism fragments and no procedural RGB noise. Pass17's overly pale
marks were corrected to restrained darker coating wear in18. Roof and shoulder
panels now vary without replacing their shape. Hidden-side paint remains inferred.

`texture-only-geometry-check.json` verifies all214 mesh geometries and transforms
match pass15 exactly, including the nested emitter dishes.32 native views rendered;
working-study regressions pass. No runtime integration; missing original mechanical
surface details are a separate open item, not declared solved by coating changes.

## Previous detail work: pass15

The user's latest specific request is the multiple nested dishes INSIDE the front
emitter around the cyan core. Pass15 implements two independent profiled annular
dishes plus recessed separators, not painted rings. Their rear sections overlap the
existing front housing/socket; the core remains recessed and visible. All32 facings
were rendered; reference, side, reverse and end-on views inspected. This addresses
that concrete omission, not every outstanding detail or overall art approval.
See `pass-15/review/eight-views-2x.png`. No game integration.

## Earlier rejected stand and subsequent work

The user explicitly says pass10's turret is still far from the source. The base is
partly acceptable but lacks details and has a notch through its vent slats. No Sonic
integration is authorized. Pass count is not acceptance. Pass11 replaces the old
photographed vent patches with continuous physical louvers/frames; internally
rendered and inspected. It does NOT resolve the rejected turret proportions,
mechanical forms or source detail density. Pass12 broadens the swept shoulders,
lowers the forward armor skirts, deepens the actual dish profiles and adds a
substantial reinforced gray receiver neck. Rear service covers/status fittings
were repositioned rather than left buried inside the changed housing. Native
reference/reverse review still shows insufficient source detail and overly simple
housing treatment. This is an internal iteration, not a presentation for approval.
Pass13 replaces the contaminated broad roof projection with bounded independent
armor plates, adds correctly sized diagnostic panels, protective rails, lower yoke
inspection covers and reverse-side stiffeners. A signed-volume inspection found
inward yoke normals: pass14 corrects them before beveling, and seats the previously
buried rear stiffeners on the actual dish backs. Geometry/material regressions pass,
but the source comparison still needs more housing/arm detail and material fidelity.
Substantial turret rework remains.

## Pass10 reconstruction history

Pass08 replaced the previous receiver/yoke/emitter geometry: unequal lofted housing
sections, broad structural yokes, a continuous spine, and thick profiled dishes with
beveled lips and recessed apertures. Internal review caught an overlong spine
occluding the lens; pass09 fixes this and replaces the rectangular saddle with a
circular race. Pass10 enlarges the bearings, adds neck/intermediate collars and
bounded source status panels. Roof UVs span the actual roof instead of repeating
one photographed cut on every section. Rear mechanics remain inferred.

[Current 32-view inspection](pass-10/review/all-32-native.png). The automatic padded
canvas is now 224x224; density remains 20px/unit and Scale1, not resized artwork.
Source/palette/alpha/clipping/frame-order/stationarity/raw-composite tests pass.
These are local reconstruction candidates, not user approval or game integration.
The broad metal surfaces remain an area for further source-material refinement.

## Rejected earlier direction

Pass-07 was rendered and inspected in eight directions. It remains rejected:
the receiver is too boxy, the emitter faces too flat, and the bounded side texture
cut is stretched. Do not integrate it or call it finished. Pass06 replaced the old
extruded-photo receiver; pass07 raised the housing and separated the ring borders,
but neither meets the source's visual standard. Further geometry and UV work is
required. Technical checks do not establish visual approval.

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
