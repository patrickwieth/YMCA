# V7 preview — straight central blue roof conductor

[Before/after comparison](straight-roof-comparison.png) ·
[Side-by-side rotation](rotation-before-after-2x.gif) ·
[All 32 native 160px directions](all-32-native160.png).

The old wave was baked into the source-projected roof texture. It was not an
animated effect. Its zigzag became particularly conspicuous in end-on views.
This revision removes that projected zigzag and builds a straight, centered blue
conductor with metal edges as actual separate solids attached to the turret.

Changes are limited to the short roof transition between barrel and receiver:

- Six roof vertices now lie on a continuous ramp rather than the stepped profile.
- Four roof UV faces use a clean tan armor cut from the original artwork.
- The conductor follows one straight 3D line at X=-0.125, along the gun axis.
- Channel shadows, metal edges and cyan core use sampled source palette colors.
- Side armor, barrel length, muzzle, wide cross section and receiver remain intact.
- Fixed bases are byte-identical to V6; both native sizes (160 and 192) now have
  all 32 directions rendered directly in Blender. There is no sprite upscaling.

The first pass produced an overly bright pale roof patch and was rejected. Its
model/renders/comparisons are preserved under `pass-01/`. The selected root files
use tan source armor instead. This is a focused roof correction, not a claim that
all inferred rear details and texture transitions are now finished.

**Preview only:** active Ion Defense game assets remain V5. No rule, footprint,
weapon, muzzle attachment or game image was changed by this revision. The guard
tower integration from the previous step remains untouched. Originals and the
user's open Blender scene are untouched.

Validation: 32 native frames per size, binary alpha, source palette, no clipping,
straight roof profile, exact source hash, fixed-base identity and unchanged active
V5 assets. The displayed GIF/comparison is enlarged 2x for inspection, never used
as a game sprite. No live game review, commit, push or deployment.

Reproduce from Game:

```sh
blender.exe --background --python tools/art/refine_heavy_blaster_v7.py
python tools/art/pack_heavy_blaster_v7.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v7.py
```
