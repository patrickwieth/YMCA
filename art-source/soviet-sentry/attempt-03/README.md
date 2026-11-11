# Sentry V3 — geometry review, not final art

Open `comparison-3x.png` (reference / rebuilt geometry / neutral geometry).
Display crops are aligned at native scale; original render frames are NOT trimmed.
`geometry-4x.png` is a nearest-neighbour enlargement of the full color study.
Editable model: `sentry-geometry-v3.blend` (Blender 3.6).

Rebuilt from scratch rather than filtering V1:
- Explicit sloped housing profile and separate vertical gun receiver plate.
- Sloping front armor with recessed rectangular sensor/window.
- Thick side plates, inset faces, perimeter seams and corner fasteners.
- Stepped roof, dark underlay, separate top plate and fasteners.
- Rear side equipment and vent ribs.
- Multi-tier bearing, red drum, narrow steel lips and segment seams.
- Separate gun tubes, central spindles, OPEN cluster collars, individual muzzle
  rims and recessed bores. Earlier solid collars obscured the barrel detail.

This is not yet an exact reconstruction: housing shoulder proportions, rear
extension depth and weapon dimensions require review. Six tubes per weapon and
hidden rear mechanics are inferred; the reference cannot establish their exact
construction. The emblem, weathering, texture detail and team-color masking are
intentionally absent while geometry is assessed. Color-study materials use flat
colors from the reference, not generated textures. No image generation was used.
No rotation sheet or in-game integration yet. V1/V2 and supplied source/base remain
unchanged. No commit or push.

Reproduce from Game:

```text
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/model_sentry_v3.py
python tools/art/compare_sentry_v3.py
```

The saved blend contains the color-study materials; the script additionally renders
an unsaved neutral-material variant for shape review. Camera elevation remains
30 degrees; view/geometry matching is still approximate, not calibrated tracing.
