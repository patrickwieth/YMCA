# Soviet sentry: Blender prototype v1

Unreviewed prototype, NOT bound to FTUR/TTUR in-game. The supplied stationary base,
reference and old rotation sheet are not changed. No image-generation model was used.

## Outputs

- `sentry-prototype.blend`: editable mesh model; all textures packed into the file.
- `textures/`: nearest-neighbour source extractions and single-pixel source-color swatches.
- `texture-provenance.json`: exact source sample coordinates and rectification quads.
- `renders/turret-00.png` ... `turret-31.png`: fixed-canvas transparent 128x128 renders.
- `sentry-turret-32.png`: 8 columns x 4 rows, 1024x512 RGBA sprite sheet.
- `review-32.png`: labeled contact sheet.
- `review-facing-00.png`: nearest-neighbour enlarged preview (not game art).
- `render-layout.json`: angle/order/frame metadata.

## Construction and limits

A separate rotating bearing, chamfered armored housing, roof panels, side armor,
two three-tube rotary guns, collars, muzzle bores and rear vent fins. Geometry is
an approximation of `mods/ca/bits/soviet/buildings/rotational/vorlage.png`.
Unseen surfaces are inferred, not recovered. Visible side-emblem/roof/bearing
textures come from perspective-rectified crops; uniform materials sample source
pixels. Texture creation verifies that EVERY output texture RGBA value exists in
the supplied reference. Lighting naturally changes colors in the final renders.
Nearest texture sampling and native 128px rendering avoid interpolated textures.
There is no generated/repainted texture artwork. The model still needs artistic
review, especially housing silhouette, gun detail, material contrast and scale.

Only the turret hierarchy rotates. Camera/light are fixed. Camera elevation is
30 degrees with an orthographic projection; final engine camera matching, facing
zero, handedness, pivot and muzzle offsets still need in-game calibration.
No independent frame trimming is performed, preserving a consistent rotation pivot.

## Reproduction (Blender 3.6)

From the Game repository root:

```text
python tools/art/build_sentry_textures.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_blender.py
python tools/art/pack_sentry_renders.py
```

These are standalone scripts, not an installed or modified copy of the linked
EoralMilk add-on. Its inspected revision renders separate directions using its
specific template nodes; it does not implement sheet packing or VXL/HVA export.
Our scripts implement the same rotation-render workflow plus explicit sheet packing
without depending on that template. A voxel exporter remains a separate future task;
this prototype exports PNGs only.

No commit/push was performed for this prototype.
