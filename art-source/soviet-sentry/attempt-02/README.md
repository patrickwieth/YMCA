# Pixel-style experiment 02 — one view only

Review `comparison-3x.png`: source crop / previous render / new experiment.
`comparison-native.png` is unscaled; `turret-pixel-v2.png` is the transparent
160x160 candidate. `turret-pixel-v2-4x.png` uses nearest-neighbour enlargement.
The V1 canvas was 128x128; the comparison intentionally labels each version and
does not resample either native sprite. The source crop still contains background.

Changes: emission-only materials, no AO/bloom/gloss/light gradients, discrete
per-face shades sampled from the supplied reference, cleaned source-emblem cutout,
flat replacement for baked roof texture, hard alpha and one-pixel silhouette ink.
Final colors are locked without dithering to 13 source colors. Every visible RGB
color was verified against the reference image; the emblem is copied, not redrawn.
This is a rendering/material experiment, not a finished hand-pixelled sprite.
The body remains simplified relative to the reference; panel seams and gun detail
still need visual review. Geometry/model silhouette has not been fully rebuilt.

Reproduce from Game:

```text
python tools/art/preview_sentry_pixel_v2.py --prepare
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_pixel_v2.py
python tools/art/preview_sentry_pixel_v2.py
```

The packed `sentry-pixel-v2.blend` produces `render-raw.png`; the final palette/alpha
and outline steps are in the preview script. Original assets and V1 are unchanged.
No game integration, commit, push or full rotation export for this experiment.
