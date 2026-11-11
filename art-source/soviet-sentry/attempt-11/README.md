# Sentry V11 — one-pixel black finishes on plate edges

User suggestion: edges should end in a black pixel, as in the original artwork.

Open `comparison-3x.png`: reference / V10 / V11.
`base-assembly-preview-3x.png` uses the unchanged supplied stationary base.
`rotation-check-2x.png` contains four diagnostic directions.

## Implementation

This is a final-sprite edge treatment, NOT extra geometry or painted source textures.
Blender renders diagnostic part-ID masks from the unchanged V10 model. These masks
identify roof plates, shoulder armor, side plates, covers and bearing boundaries.
The finishing script puts a ONE native-output-pixel stroke on selected boundaries,
using the original ink pixel at (75,115): RGBA (3,2,1,255).

- Only one part owns a shared boundary, avoiding doubled two-sided strokes.
- Existing black lines are retained rather than thickened.
- Thin gun tubes/collars are excluded: stroking them would swallow the metal detail.
- Existing bright gray edge pixels can move one pixel inward, where there is space,
  so the new black closure does not simply erase the metal highlight.
- Transparent pixels are never filled. Geometry, silhouette, canvas and pivot stay
  identical to V10. No expansion into the already tight canvas margin.

This implements an actual one-SCREEN-pixel finish rather than assuming that a one-
TEXEL border will stay one pixel wide after projection/minification.

The saved `sentry-v11-edge-study.blend` has the normal V10 materials restored; a
plain Blender render alone does NOT contain this final edge treatment. Run both
scripts to reproduce the finished sprites. The red part-ID PNGs and white edge-mask
PNGs are diagnostics only, never final artwork or texture content.

```text
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v11_edges.py
python tools/art/finish_sentry_v11_edges.py
```

## Checks

`edge-mask-report.json`: unchanged mesh topology/vertices, transforms, visibility,
camera and pivot. `edge-finish-report.json`: all four alpha channels byte-identical
to V10, no clipping, zero colors outside the original palette. First view adds 546
ink pixels and relocates 15 existing highlight pixels inward. The source artwork,
V10 lighting/textures and all prior attempts remain unchanged.

Still a local art study; no gameplay replacement, full 32-facing export, commit or push.
