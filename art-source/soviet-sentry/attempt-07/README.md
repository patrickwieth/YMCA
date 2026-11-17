# Sentry V7 — cleaner source-cut textures, unchanged model

Open `comparison-3x.png` (reference / V6 / V7), or
`base-assembly-preview-3x.png` with the original stationary base.
`rotation-check-2x.png` shows four diagnostic angles, not a finished rotation sheet.
Packed Blender model: `sentry-v7-textures.blend`.

This pass changes textures and the visible front-cover material/UV only:
- Corrected the red-slope source quadrilateral. V6 enlarged a partial scratch
  cluster instead of using the full painted plate.
- Replaced densely copied shoulder/front-edge fragments with quiet source-metal
  backing and a few small, hand-selected source wear clusters pasted at 1:1.
- Refined the rear-roof cut to avoid copying the neighboring hatch contour.
- Found that the existing solid frame occluded the recessed metal-cover mesh.
  Put the source metal texture onto the visible frame surface instead; no mesh
  was removed or changed to obtain the result.
- Kept the emblem, roof, barrel and bearing work from preceding passes.

All texture content is copied from `vorlage.png`, with nearest sampling, masks
and source-pixel backing only. `texture-provenance.json` records exact cuts and
placements. No image-generation tool, procedural noise or painted wear used.

Checks:
- Before/after hashes of mesh vertices, face topology, object transforms,
  visibility and camera match.
- Four final alpha silhouettes match the corresponding V5 images exactly.
- Four views: binary alpha, no clipping, and every visible color from the source.
- Bearing center stays on its rotation axis.

Visual fidelity is still a work in progress. These checks establish preservation
and provenance, not artistic equivalence. There are still differences in material
balance and pixel-cluster placement relative to the reference.

Reproduce from Game:

```text
python tools/art/prepare_sentry_v7.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v7.py
python tools/art/compare_sentry_v7.py
```

V6 is the model input. Source assets, prior versions and gameplay bindings remain
unchanged. The base composition is a preview, not a calibrated game attachment.
No commit or push.
