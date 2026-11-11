# Heavy Blaster V4 — multi-view mechanical correction

Historical checkpoint, now superseded in-game by [V5](../attempt-05/README.md).
The dimensions and binding descriptions below refer to V4.

Current model: `pass-05/heavy-blaster-v4.blend`.
Current separate native assets: `export/heavy-blaster-base.png` and
`export/heavy-blaster-turret32.png`. Now bound to the existing **Ion Defense**:
[active game binding and documented muzzle-attachment changes](integration/README.md).

## Review the difficult views, not only the source-facing pose

- [V3 / V4 / native contour finish, six angles](export/multiview-comparison-2x.png)
- [All 32 directions](export/rotation-contact-sheet.png)
- [Rotation preview](export/rotation-preview.gif)
- [Separate base and turret](export/separate-parts-3x.png)
- [Reference-facing study comparison](export/main-comparison-2x.png)

## Work performed and rejected experiments

1. Replaced the image-derived polygon trunnions with actual coaxial cylinders,
   a common shaft, paired structural arms, front/rear root braces and anchor pads.
   The old side-profile caps were not really round and the supports read poorly
   when rotated. New parts share the same physical Y/Z axis, not merely the same
   apparent screen location. This is a mechanical change, not cosmetic UV masking.
2. Removed redundant thin barrel/casing overlays that competed at grazing angles.
   Tried 128px nearest source-texture LOD on all turret parts. Rejected as final:
   fine monitor/arm/roof detail suffered.
3. Added distinct bearing seals/rims/copper caps and restricted source LOD to the
   main hull. Corrected vertically reversed brace UVs. Kept this comparison in
   `export-pass-03`, rather than silently discarding it.
4. Tested an explicit top-strip UV reassignment. Rejected: it did not adequately
   improve the end-on view and disturbed the source-facing roof detail.
5. Kept the mechanical/overlay corrections, restored the authoritative texture
   everywhere, retained the better roof UVs and rendered all 32 native directions.
   Applied a restrained one-native-pixel exterior source-ink contour. This gives
   the silhouette a continuous boundary without blur, noise or runtime scaling.

All source colors are sampled from the supplied reference. The bearing facets use
source metal tones, not synthetic weathering. The 128px texture experiments are
retained, but the selected export does **not** use them. Rendered sprites were never
resized: all game-size turret frames were rendered directly at 160x160.

## Separation and checks

The fixed base is **pixel-identical to V3**, including its separate PNG. Rotating
the turret to 123.75 degrees leaves the base render unchanged. The shared canvas
and pivot/attachment metadata also remain unchanged.

Blender assertions check nine coaxial mechanical parts and two anchor pads. Their
bottoms are 0.01 world units above the fixed socket top (less than 0.2 native pixel),
not floating several pixels above it. The arms overlap their pads and the shaft
spans between the arms through the gun housing.

The source PNG hash is unchanged. All prior attempts and all five V4 passes are
retained. Nine combined Heavy Blaster tests pass: source preservation, mechanism
checks, stationary base, real native frame dimensions, 32 distinct directions,
embedded PNG frame metadata, source-only palette, binary alpha and no clipping.
These are technical checks, **not an automatic artistic-quality approval**.

## Still limited

The reference shows only one side. Rear/opposite-side detail is still inferred,
partly reused from observed panels. The source shading is baked into the textures,
not physically relit per view. Some end-on roof/detail transitions still read less
naturally than the reference-facing pose. The new round bearings slightly change
that pose in exchange for more coherent side/rear geometry.

The subsequent Ion Defense binding now installs these assets and visual bounds;
see [integration](integration/README.md). Engine loaders/trait dependencies and
muzzle projection are checked, but live GPU/firing appearance remains unreviewed.
Open Blender/unsaved scenes were untouched; separate background processes only.
No game restart, commit, push or deployment.

## Reproduce from Game

```sh
python tools/art/prepare_heavy_blaster_v4.py
blender.exe --background --python tools/art/refine_heavy_blaster_v4.py -- --pass 5 --all
python tools/art/pack_heavy_blaster_v4.py
python -B -m unittest discover -s tools -p 'test_heavy_blaster*.py'
```
