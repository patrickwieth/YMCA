# V13: native-size 32-facing game export

Replaces the test-map-only 8-facing / ingame Scale 0.5 trial. Earlier exports and
all original assets remain intact. No original blend file or supplied PNG is overwritten.

## Native render, not a downsized turret

Both V12 control and V13 are rendered directly in Blender at **120x120**, 100%
resolution percentage, with the same camera framing as the 240px export. This
halves the world-to-pixel density in the actual render, not in the game renderer.
All 32 lighting, albedo, geometry-ID and weapon-ID passes use that same resolution.
The source-palette mapping and one-native-pixel edge/highlight finish are applied
at 120px too. Turret pixels are never resized by the packer. All four bound body
sequences use **Scale 1**.

The stationary base is supplied pixel art, not part of the Blender model. It is
nearest-sampled once into a separate 74x43 asset, then also drawn at Scale 1. Its
original bytes remain unchanged. The bib stays `sovietbib1x1` (128x64); 64px is
only ground depth, not a height cap.

## Rotation and binding

- 32 distinct actual renders, every 11.25 degrees. No image mirroring or synthetic
  2D rotation between the eight original views.
- Render filenames use integer **indices 000..031**, meaning index * 11.25 degrees.
  Generic review JSON fields called `angle` likewise contain these indices.
- Engine facing order increases counterclockwise: N=0, W=256, S=512, E=768.
  Engine frame i uses render index `(20+i)%32` (Blender north = 225 degrees).
- Sheet: `experimental/sentry-v13-native32.png`, 8x4 frames, 960x480 overall.
  **Embedded** PNG `FrameSize: 120,120` / `FrameAmount: 32`, not just a sidecar.
- Turret sequence Offset `-4,-24`; base sequence Offset `0,-3`.
  Both move together 8px left and 2px down relative to the first native export.
  Base ground-contact pixel centers `(1.5,24.5)` and `(72.5,24.5)` now average
  exactly to the bib origin. Preview images include the actual 128x64 bib.
  Existing gameplay world offset `0,0,112` is retained; its screen displacement
  is included in attachment calculations. Errors are recorded in `game-binding.json`.
- The separate actor **SENTRY.SOVIET** now owns this binding and its
  BodyOrientation.QuantizedFacings=32 / TurnSpeed=32 settings (half-turn: 16 ticks).
  It inherits FTUR as a temporary gameplay template but does NOT override FTUR.
  `-Buildable` keeps this art prototype out of normal production menus.
- Actor27 on the disposable map is now `sentry.soviet`, at the same position.
  Initial facing remains SW=384; inherited weapon, muzzle spawn, occupied cells,
  hit shapes and interaction bounds are retained. Decoration height 49 encloses
  all 32 frames. FTUR/TTUR are unchanged both inside and outside this map.
- This directory remains the sprite export. A separate real VXL/HVA alternative
  is now available in [voxel-01](../voxel-01/README.md), placed beside it on the test
  map as `SENTRY.SOVIET.VOXEL`. Neither version replaces FTUR.

## Reproduce (from Game)

```sh
bash tools/art/export_sentry_v13_native32.sh
python tools/art/pack_sentry_v13_native32.py
python -B -m unittest discover -s tools -p 'test_sentry_v13_game.py'
```

The tests call the real engine sprite loaders, validate cardinal directions,
frame-to-render pixel identity, native input/output sizes, source palette, binary
alpha, all-frame containment, base derivation, prototype trait dependencies and
independent-actor isolation (including exact FTUR rule equality in the test map).

`game-assembly-preview-2x.png` and `rotation-preview.gif` are compositions, not
in-game captures. The GIF uses 80ms per frame for inspection, not game turn timing.
This export does not fix the known V13 bore geometry or calibrate new firing
origins; the Flame Tower weapon remains. Live appearance/turning review is pending.
No game session was started or stopped, and no commit/push/deployment is included.
