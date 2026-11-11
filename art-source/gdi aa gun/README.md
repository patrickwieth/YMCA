# GDI AA Gun — modeled/textured native 32-direction study

**User rejected pass-05 visually. It is NOT approved or selected for integration.**

Rework started: pass-06 replaces projected moving armor with explicit panels and
separate mechanical geometry, but is still too plain. Further pass-07 texture and
shape corrections are written in `tools/art/small_turret_rework.py` but have NOT yet
been rendered/reviewed. Work was interrupted by the new power-plant inputs and Nod
laser study request. Do not present the old pass-05 technical checks as approval.

The links and implementation record below describe the **rejected historical pass-05**.

- [Rotation on native 1x1 bib](pass-05/review/rotation-with-1x1-bib.gif)
- [Source / model / opposite view](pass-05/review/source-comparison-2x.png)
- [Eight directions](pass-05/review/eight-views-2x.png)
- [All 32 native views](pass-05/review/all-32-native.png)
- [Separate actual parts](pass-05/review/separate-parts-2x.png)
- Model: `pass-05/aa-source-model.blend`.

Original `phalanx_ciws_turret_...-204705973-0.png` is unchanged, SHA256
`3c07334edab877fb11a06f425ce1a8459d06477e72facecb2956cda7c55449b7`.

## Actual construction

Seven stationary meshes form the plate, armored pedestal, bearing and vents.
Twenty-one rotating meshes form the open armored helmet, cradle/mantlet, six real
hollow barrels with collars, optical sensor, antenna and paired radar reflectors.
The saved model has 1,160 vertices / 949 faces, UV layers on every mesh. This is
geometry, not a rotating image plane or voxel approximation.

Sources use nearest sampling, source-cut UV patches and sampled palette materials.
The firing flash in the supplied image is excluded from the idle texture; no flash
geometry is baked into the turret. Hidden surfaces reuse observed armor patches;
their detail cannot be claimed as recovered from the single reference view.

## Iterations

1. Solid model, source calibration, independent base/turret, eight-angle review.
   Rejected merged grey barrel appearance and overly broad dark gun opening.
2. Separated narrower hollow barrels, protruding muzzles, facet contrast, smaller
   aperture, physical mantlet and clean source-gold patches on inferred sides.
3. Paired tracking reflector, all 32 views and geometric framing checks.
4. Darker source-metal barrel shading, retaining visible tips instead of a light
   grey stick. Source-facing, opposite and end-on directions reviewed again.
5. Base holdout during turret exports. This removes the contact-occlusion error
   when layers are composited; five full 3D control views now match separate raw
   layers **pixel-for-pixel**. All 32 final directions inspected.

All earlier passes and their blends/renders remain available. Some inferred helmet
panels are simpler than the supplied side, and tiny bores lose readability at some
native angles. This is a reviewable study, not a claim of perfect source fidelity.

## Native export and 1x1 target

`review/base.png`: one 192x192 frame; `review/turret32.png`: 1536x768, 32 frames.
The 192px canvas is transparent padding at 20px/model-unit, **not runtime scaling**.
Both meshes/exports remain separate. The foundation fits inside a 128x64 one-cell
ground diamond; measured fixed XY extent 1.91 vs limit 2.26274. The preview uses the
existing original GDI 1x1 bib. Source angle0 points NE; engine frame i uses render
index `(4+i)%32`, following N/W/S/E engine ordering without mirrored pixels.

Actual engine PNG loaders read both native exports and embedded frame metadata.
Geometry inspector verifies separate parents, stationary matrices and 1x1 fit.
Tests verify original hashes, palette, binary alpha, clipping, all facings and five
exact full-3D/separate-layer comparisons. Background Blender only; user's open
scene untouched. No gameplay actor, weapon, production slot or test-map placement
has been installed. No live game review, commit, push or deployment.

See [shared workflow and selected previews](../gdi-small-turrets.md).
