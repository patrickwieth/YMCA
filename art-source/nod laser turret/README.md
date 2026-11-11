# Nod laser turret — native reconstruction candidate

Current review candidate: **pass-04**, after four actual model/render/review cycles.
Not an installed gameplay actor or an artistically approved final asset.

- [Source / current model / opposite side](pass-04/review/source-comparison-2x.png)
- [32-direction rotation](pass-04/review/rotation.gif)
- [Eight enlarged directions](pass-04/review/eight-views-2x.png)
- [All 32 native directions](pass-04/review/all-32-native.png)
- Actual model: `pass-04/nod-laser-model.blend`
- Exports: `pass-04/review/base.png`, `pass-04/review/turret32.png`

## Source and scope

Original `laser3 turret.png`, 161x161 RGBA, untouched SHA256:
`4d77eba222321efb813251506bdfaf412eba9df77125a631970c8e4427abf10e`.

Background Blender 3.6 only, no interaction with the user's open scene. Source
textures are bounded UV islands on the receiver/yoke plates, not a single image
projected over the whole moving model. Other surfaces use source-palette materials,
consistent directional shading and actual mechanical geometry. No procedural RGB
noise, voxel substitute, billboard turret, resized render or baked firing beam.

Stationary: circular red foundation, steel plate and inset bearing. Rotating: paired
solid yokes, red armor, inner support ribs, lower bearings, upper trunnions with
individual teeth/hubs, closed receiver, cooling ribs, side/rear vents, top sight,
barrel sleeves/red bands and an actual recessed red emitter inside an octagonal
muzzle. Unseen rear mechanics and opposite-side repeated panels are inferred.

The original's precise hand-pixeled bevels/highlights are not fully reproduced;
the reconstruction remains more regular and less rounded in places. Review images
are honest source/model comparisons, not game screenshots or a claim of approval.

## Iteration record

1. Real separate geometry, explicit side-panel UV islands, native renders and
   geometry-ID contours. Review rejected undersized lower bearings, merged roof
   fins, bland yokes and a blank rear cover.
2. Larger lower bearings, bounded source yoke panels, separately delineated cooling
   fins, larger muzzle armor and a modeled rear louver panel. Protected small gear
   teeth from over-thick contour finishing; reviewed 32 native directions.
3. Applied real receiver/sight/fin bevels, added the lower receiver apron, preserved
   the copper emitter bezel instead of outlining it away.
4. Deeper beveled bearing rims and larger ten-tooth gears; moved the lower yoke feet
   and front maintenance brace toward the source's stance. Checked source, side,
   rear, end-on and every final native yaw view.

Earlier models/renders remain in their own directories. Interior ink lines come
from actual visible geometry surface IDs, with stable single-sided ownership and
protection for small mechanical teeth and luminous details. No silhouette dilation.

## Native rendering / validation

256x256 is the transparent **frame canvas**, not the foundation width. Density is
20px/model-unit throughout; Scale1. Ground origin `(128,186)`, pivot `(0,0,0.4)`.
Foundation XY extent 2.04 fits a candidate 128x64 one-cell ground area (limit 2.26274).
This is geometry validation only, not an installed 1x1 actor footprint/bib binding.

Separate PNG metadata: base FrameAmount1; turret 2048x1024 sheet, FrameAmount32,
FrameSize256,256. Forward is model -Y; engine frame i selects render `(20+i)%32`.
No mirrored facings. A base holdout during the turret pass preserves real occlusion.
Five full 3D controls at 0/45/135/225/315 degrees match raw base+turret composites
pixel-for-pixel. The fixed base control remains identical after rotating the turret.

Independent saved-blend inspection verifies three fixed meshes, 127 moving meshes,
5,594 vertices / 4,517 faces, separate parents, all UV layers, stable fixed matrices,
foundation bounds, and no projected-vertex clipping throughout all 32 rotations.
Tests check source/model hashes, all 32 distinct native frames, source palette,
binary alpha, clipping, ordering, metadata and the real OpenRA PNG loaders.

The full solution build compiled the new utility but could not copy its DLL because
the running game holds `engine/bin/OpenRA.Mods.CA.dll`. The game was NOT stopped.
The fresh compiled CA assembly was tested with a separate copy of the existing
utility/dependencies at workspace `.nod-laser-study-utility/bin`; all four study
tests passed there. This is not a claim that the normal output-copy/build completed.
No live gameplay/firing/selection tests, commit, push, deployment or source-map edit.

## Reproduce from Game

```sh
blender.exe --background --python tools/art/model_nod_laser_turret.py -- --pass 4 --all
python tools/art/review_nod_laser_turret.py --pass 4
blender.exe --background --python tools/art/check_nod_laser_geometry.py
python -B -m unittest discover -s tools -p test_nod_laser_turret_study.py
```

Engine utility: `--check-nod-laser-turret-study`. Tests use normal engine/bin unless
`NOD_STUDY_UTILITY_DLL` points to an isolated utility copy containing the fresh CA
assembly. Normal ENGINE_DIR/MOD_SEARCH_PATHS configuration is applied by the tests.
