# Sentry V4 — geometry refinement and source-cut per-part textures

Open `comparison-3x.png`: reference / V3 neutral / V4 neutral / V4 textured.
`comparison-native.png` retains native pixel sizes. Presentation crops do not
change the actual 192x192 render canvas. `textured-v4-4x.png` is nearest-neighbour
magnification. Editable, packed model: `sentry-v4.blend` (Blender 3.6).

Actual changes from V3:
- Housing depth reduced by 16% around the fixed gun-receiver front.
- Separate sloping gray shoulder rails frame the red frontal center.
- Individual planar UV assignments for roof plate, rear roof, frontal armor,
  side plate and rear equipment, plus repeated narrow strips on the bearing.
- Source emblem extracted as an alpha cutout and put on a separate mesh layer.
- Existing scratches, metal color clusters and plate edges copied from source
  cuts; no generative image model, procedural noise or painted texture content.
- Closest texture sampling, emission-only rendering, binary final alpha and
  nearest source-palette color cleanup without dithering.

`texture-provenance.json` records all extraction quads and the metal fill sample.
Preparation verifies extracted texture colors against the original. Final preview
also verifies all visible output colors occur in the source. The alpha mask for
the emblem is an edit, not an unchanged rectangular source crop. Perspective
rectification and UV projection necessarily change the arrangement of pixels.

Still approximate, not a finished replica: barrel/collar proportions, some panel
edge transitions and rear equipment need further refinement. Bearing wear repeats
between segments. The exact hidden barrel count/back geometry remains inferred.
The neutral companion shows geometry only, not the final material scheme. Texture
orientation/lighting across other rotations has not yet been reviewed; no full
rotation sheet or game integration for V4.

Reproduce from Game:

```text
python tools/art/prepare_sentry_v4.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v4.py
python tools/art/compare_sentry_v4.py
```

The V3 blend is the geometry input. The saved V4 blend contains packed textures;
the script additionally renders an unsaved neutral companion. Source PNGs and
all earlier attempts remain unchanged. No commit or push for this iteration.
