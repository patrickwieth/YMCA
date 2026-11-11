# Sentry V13 — real voxel alternative, first experiment

This is an actual Westwood **VXL/HVA**, not another pre-rendered sprite sheet.
The original sprite Sentry and all earlier study files remain intact.

## Contents / binding

- Source: saved `game-v13-32-native/render.blend` (V13 geometry). The user's open
  Blender session and unsaved changes were not read or modified.
- 23,840 surface voxels on a 74x40x40 grid, 18 voxels per model unit.
- Geometry is sampled from evaluated mesh surfaces, not reconstructed from PNGs.
- VXL coordinates: forward X = -Blender Y, file Y = Blender X, Z unchanged,
  relative to the bearing pivot. OpenRA flips the file Y axis internally.
- One static limb and one identity HVA frame. The engine rotates the actual volume
  continuously; it is not limited to 32 pre-rendered facings.
- Colors come from the corresponding 32 source-albedo renders, projected onto
  nearest mesh surfaces and mapped to the reference palette. Hidden surfaces are
  approximate. There is no procedural wear/noise or generated texture painting.
- An indexed PNG provides exact 8-bit source palette colors. The VXL header also
  has the conventional 6-bit palette for format compatibility, but the game uses
  `sentry-voxel-source`. RA2 surface normals provide restrained runtime lighting.

Independent actor: **SENTRY.SOVIET.VOXEL**, inheriting the existing independent
sprite prototype. `WithSpriteTurret` is removed; `RenderVoxels` + `WithVoxelTurret`
render only the new turret. The small stationary PNG base and 1x1 bib remain.

The disposable test map places it at **142,-66**, directly screen-right of the
sprite Sentry at **140,-64**. Tooltip: `Sentry V13 (voxel test)`. Neither prototype
is in normal production menus; FTUR and its build entry are unchanged.

CameraPitch 85 approximates the 30-degree sprite-study camera. RenderVoxels.Scale
0.98765432 converts the voxel lattice to the same physical image size: this is
voxel geometry scale, not downsampling a rendered PNG. A model-space Z translation
seats the bearing on the PNG base while preserving the gameplay turret offset.
The voxel variant uses unquantized body orientation and the prototype's TurnSpeed
32. Old Flame Tower weapon/muzzle settings remain placeholders.

## Validation and limitations

`--check-sentry-voxel` uses the engine's real VxlReader/HvaReader/Png readers and
checks limb/frame counts, voxel colors/normals, bounds, actor trait dependencies,
model-sequence registration and engine forward/camera orientation. Python tests
also compare every encoded voxel to the export data and verify separate map slots.

**Live GPU appearance is not yet verified**, especially depth ordering against the
sprite base and exact attachment/lighting. Runtime normal lighting produces colors
different from the source palette; the finished sprite's screen-space ink/highlight
pass is not baked into this voxel model. Sub-voxel details may thicken/disappear,
and projection-based colors can look grainier. V13 bore geometry is unchanged.

`voxel-study.blend`, `voxel-preview-*.png` and `comparison-2x.png` show the actual
voxel cells in Blender with unlit palette colors, NOT OpenRA GPU output.

## Reproduce from Game

```sh
blender.exe --background --python tools/art/voxelize_sentry_v13.py
python tools/art/pack_sentry_voxel.py
blender.exe --background --python tools/art/preview_sentry_voxel.py
python -B -m unittest discover -s tools -p 'test_sentry_voxel.py'
```

Start `test-gebaeude.cmd` for the in-game comparison. No game was started/stopped,
and no commit, push or deployment was performed.
