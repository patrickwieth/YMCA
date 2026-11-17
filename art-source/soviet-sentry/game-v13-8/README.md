# V13 in-game prototype (eight facings)

Archived iteration. The active test-map binding now uses
[32 native-size renders](../game-v13-32-native/README.md) at Scale 1.
This directory and its original export remain available for comparison.

This is an actual export from `attempt-13/sentry-v13.blend`, not the old 3x3
rotation sheet. All original studies, PNGs and source textures remain untouched.

- Eight complete directions, matching the existing actor's eight-facing quantization.
- Native pixel density unchanged. The canvas grows from 200 to 240 with 20px
  padding per side so previously unrendered directions do not clip.
- Same V13 geometry/materials. Raw lighting renders contain intermediate colors;
  finals are palette-mapped to source colors and receive the established one-pixel
  ink/paired-highlight pass. Plain Blender renders do NOT include that finish.
- V12 control lighting/albedo/edge frames were regenerated for all eight angles,
  rather than incorrectly reusing masks from another angle or geometry.
- Every final frame has binary alpha and zero visible colors outside the reference
  palette. Previous four V13 silhouettes match exactly after removing padding;
  a small number of shaded/finished pixels differ in the wider-canvas rerender.

## Binding and rotation

`experimental/sentry-v13.png` is a 4x2 sheet of 240x240 frames. Its embedded PNG
text chunks declare `FrameSize: 240,240` and `FrameAmount: 8`. The YAML sidecar is
only a metadata reference: PngSheetLoader does NOT read it automatically.
The initial export omitted these embedded chunks and crashed at sprite loading;
the packer now includes them without changing any pixels. The regression suite
also calls `--check-sentry-v13-frames`, using the actual engine sprite loaders.
After the in-game left/right report, frame order is corrected to N/NW/W/SW/S/SE/E/NE:
Blender angles 225/270/315/0/45/90/135/180. OpenRA WVec.Yaw counts counterclockwise
(north 0, west 256, east 768), unlike the initial clockwise assumption. No pixels
are mirrored; existing complete frames are reordered.

The original base PNG is unchanged. Its existing Center-anchor Offset `16,-10`
and sampled socket `(73,16)` are retained. The existing gameplay turret world
offset `0,0,112` is also retained. For the requested 1x1 version, both the base and
turret sequences use Scale 0.5; native PNG pixels remain intact. The turret sequence
Offset is now `8,-53`: sequence offsets scale with the sprite, whereas the world
height does not. This keeps the bearing attached to the scaled base.
`game-binding.json` records the projection, source-base hash and attachment error.

`ftur.soviet.v13` inherits all old base, construction and icon sequences. Only its
healthy/damaged turret sequences use the new sheet. The disposable building map
adds a local FTUR image override, the tooltip `Sentry V13 (art test)` and an initial
SW facing (384), plus a visual-only 112x56 selection diamond / height 49.
The test-map bib is `sovietbib1x1` (128x64), replacing its 256x128 2x2 bib.
The turret may extend upward; 64px is ground depth, not a total-height limit.
Occupied cells, hit shapes and mouse bounds are unchanged (the actor was already
1x1 mechanically). Global FTUR and TTUR retain their old art.
Actor27 remains at `140,-64`, above the Soviet helipad. Start `test-gebaeude.cmd`
to rebuild the disposable map; an already running game is not modified/restarted.

## Reproduce

From Game with Blender 3.6 and Pillow/NumPy installed:

```sh
bash tools/art/export_sentry_v13_game.sh
python tools/art/pack_sentry_v13_game.py
python -B -m unittest discover -s tools -p 'test_sentry_v13_game.py'
```

Preview: `game-assembly-preview-2x.png` (source composition, not an in-game capture).

## Explicit prototype limitations

This tests the appearance of V13, including its known muzzle-readability problems;
it does not claim a new bore-geometry fix. Eight facings can still look stepped.
The actor retains Flame Tower gameplay, including its original weapon and muzzle
spawn offset. No Gatling firing channels or combat rebalance were introduced.
Live appearance, rotation, layering and firing-origin alignment need in-game review.
No commit, push or deployment is included.
