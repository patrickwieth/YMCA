# AA Gun and Sonic Turret — rejected studies, rework pending

**Both pass-05 studies were explicitly rejected by the user. Do not integrate them.**
Pass-06 reconstruction studies exist; pass-07 refinements are written but unrendered.
The following comparisons/checks document the historical pass-05, not an approval.

[Both at actual native size on 1x1 bibs](gdi-small-turrets-native-preview.png).

| Study | Model | Rotation |
| --- | --- | --- |
| [AA Gun](gdi%20aa%20gun/README.md) | `gdi aa gun/pass-05/aa-source-model.blend` | [32 directions](gdi%20aa%20gun/pass-05/review/rotation-with-1x1-bib.gif) |
| [Sonic Turret](gdi%20sonic%20turret/README.md) | `gdi sonic turret/pass-05/sonic-source-model.blend` | [32 directions](gdi%20sonic%20turret/pass-05/review/rotation-with-1x1-bib.gif) |

Both now have actual separate stationary/rotating meshes, source textures, five
render/review passes, 32 directly rendered native views and independently validated
exports. Neither is installed as a gameplay actor yet. No existing defense, map or
source art was replaced. Preview bibs are the original native GDI 1x1 image.

Review: not just reference-facing views. Eight-view comparisons were checked during
iteration, then all 32 final native directions, including side/rear/end-on views.
Remaining limitations (inferred rear details, local texture stretching and tiny
features) are documented per model; automated tests are not an artistic approval.

Validation includes actual saved `.blend` geometry, stable base matrices/control
renders, foundation bounds inside one ground cell, source hashes, source-only
palette, binary alpha, no final clipping and engine-convention frame ordering.
Full 3D controls at 0/45/135/225/315 degrees match base+turret raw composites exactly
for both models. A base holdout in the turret pass handles contact occlusion.
The real OpenRA PNG loaders read 1+32 frames per study. No live GPU/firing review.

From Game (independent background Blender processes, never the user's open scene):

```sh
blender.exe --background --python tools/art/model_gdi_small_turrets.py -- --kind aa --pass 5 --all
blender.exe --background --python tools/art/model_gdi_small_turrets.py -- --kind sonic --pass 5 --all
python tools/art/review_gdi_small_turrets.py --kind aa --pass 5
python tools/art/review_gdi_small_turrets.py --kind sonic --pass 5
blender.exe --background --python tools/art/check_gdi_small_turret_geometry.py
python tools/art/review_gdi_small_turrets_overview.py
python -B -m unittest discover -s tools -p test_gdi_small_turrets.py
```

Utility: `dotnet engine/bin/OpenRA.Utility.dll mods/ca --check-gdi-small-turret-studies`
(run from Game with the normal ENGINE_DIR/MOD_SEARCH_PATHS environment).

Future integration needs explicit actor/weapon/balance choices, 1x1 occupied cells,
visual-only bib overlay, source-geometry muzzle calibration and live testing. Source
palette studies can then use the existing non-destructive GDI yellow harmonization
layer; the two approved faction-color reference buildings remain unchanged.
