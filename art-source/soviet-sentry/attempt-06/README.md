# Sentry V6 — texture-only work on the accepted V5 model

The user's current concern is the TEXTURE, not further geometry detail.
No mesh, transform, camera, object visibility or silhouette was changed here.
`texture-assignment-report.json` records matching before/after geometry hashes.
The preview additionally asserts an exactly identical alpha silhouette to V5.

Open `comparison-3x.png`: reference / V5 / V6 on the same model and camera.
`base-assembly-preview-3x.png` uses the unchanged supplied stationary base.
`source-cuts-2x.png` shows the actual patches; `sentry-v6-textures.blend` packs them.

Texture work:
- Corrected roof UV orientation: source U follows rearward Y, V follows negative X.
- Separated the red slope cut from the adjacent gray cover instead of copying
  neighboring details into both materials.
- Replaced the black-screen treatment with a source metal-cover patch.
- Assigned worn-metal cuts to shoulders and front-facing side-plate edges.
- Copied a narrow receiver paint edge and scuff rather than source gun sockets.
- Copied longitudinal wear to the brightest barrel face, leaving grooves dark.
- Reworked continuous bearing texture coverage at roughly native texel density.

The first local render of this pass copied too many neighboring black contours
onto the shoulders. It was reviewed and revised: those pixels are now cut out,
with a verbatim source metal/paint pixel as the backing. No painted synthetic
scratches, random grain or image-generation output. Mask/filter choices and all
source quads are recorded in `texture-provenance.json`.

This is still a texture study, NOT a claim that source fidelity is solved.
Some source perspective/lighting remains baked into copied pixels, and patch
transitions/texel density still need artistic review. Matching source colors is
only a mechanical provenance check, not a visual-quality score.

Reproduce from Game:

```text
python tools/art/prepare_sentry_v6.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v6.py
python tools/art/compare_sentry_v6.py
```

Checks: unchanged geometry/camera, exact V5 silhouette, binary alpha, no clipping,
all visible final colors and texture pixels from the source. Previous versions,
source artwork and gameplay bindings remain unchanged. No commit/push.
