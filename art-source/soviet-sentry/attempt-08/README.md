# Sentry V8 — V6 weathering, exact original colors, copied fasteners

User feedback: V6 texture is preferable to V7; retain the good wear, use EXACT
source colors, and carry over rivets/fasteners for authenticity. Model unchanged.

Review:
- `comparison-3x.png`: reference / V6 / V7 / V8.
- `v6-v8-detail-4x.png`: V6 left, V8 right, same native scale and canvas.
- `original-rivet-cuts.png`: actual extracted source hardware, enlarged.
- `base-assembly-preview-3x.png`: preview with untouched supplied base.
- `rotation-check-2x.png`: four diagnostic orientations, not a final sheet.
- `sentry-v8-textures.blend`: editable packed model.

Implementation:
- Loads V6, not V7. Retains V6's shoulder, front and bearing weathering.
- Copies a 5x6 recessed rivet cluster from source (128,116)-(133,122) onto the
  front edge; no invented circle, new geometry or painted highlight.
- Copies source side fasteners onto existing fastener caps, and darkens the
  central head using the actual source center pixel instead of a bright dot.
- Existing roof fastener caps use the source hardware cut.
- Uses explicit source pixels for the side metal rim and bearing edge/shadow,
  instead of reusing the same generic material across different parts.

## Palette verification — not merely a claim of similar colors

`texture-provenance.json` records source hash, source rectangles, swatch coordinates,
source RGBA values and the full allowed original palette. All texture pixels are
checked against that palette. Every unlinked emission material is checked BEFORE
rendering (`texture-assignment-report.json`).

Observed `palette-audit.json` results:
- Facing 0: 39 visible colors; 0 outside the source.
- Facing 45: 36 colors; 0 outside the source.
- Facing 180: 38 colors; 0 outside the source.
- Facing 315: 38 colors; 0 outside the source.
- ALL FOUR RAW Blender images already use exact source colors. Maximum RGB
  correction in the final palette check is 0; no recoloring was needed.

Earlier final previews also used source colors. The remaining artistic issue is
where and how much of each color/detail appears, not just palette membership.
This iteration is still a study, not a declaration of a finished replica.

Geometry/camera/visibility hashes match before and after. All four alpha silhouettes
match V5 exactly; binary alpha and no clipping verified. Only material/UV work.

```text
python tools/art/prepare_sentry_v8.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v8.py
python tools/art/compare_sentry_v8.py
```

Source art, prior attempts, unrelated user replacements and gameplay bindings are
untouched. No generation, procedural noise, painting, commit or push.
