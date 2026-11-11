# Sentry V12 — all seven reference-review corrections

Main review: `comparison-3x.png` = reference / V11 / final V12.
`base-assembly-preview-3x.png` uses the unchanged supplied stationary base.
`rotation-check-2x.png` contains four diagnostic directions.
Final source-palette + ink images are `final-turret-000.png`, `045`, `180`, `315`.

## Actual changes

1. **Front cover:** raised the existing metal-cover mesh above the solid frame
   that previously hid it; slightly widened it and applied the source metal cut.
   This is now a protruding component, not just a gray patch painted onto a slot.
2. **Guns:** tube radii increased 20%; wider/thicker shared collars and common
   muzzle surrounds; tiny individual muzzle rims made less bright/noisy. The bright
   tube-face cut no longer includes baked black silhouettes of neighboring tubes.
   Metal highlight/shadow contributions retuned, retaining the actual dark bores.
3. **Roof:** main hatch widened 12.8% and extended 0.22 units rearward. The useful
   V10 rear extension remains, but its exposed roof strip is no longer dominant.
4. **Side armor:** added a narrow 0.035-unit one-segment bevel; upper facets catch
   light, lower returns stay dark. Existing source weathering and rivets retained.
5. **Wear:** replaced the enlarged orange front cluster with a directed 16x3
   source scratch and small copied corner chip. Other established wear preserved.
6. **Bearing:** source paint remains brighter under the overhanging body rather
   than being shaded twice; changed light direction from (-3,-4,8) to (-2,-5,8)
   and tuned cylinder ambient/diffuse to .88/.65. Separate metal rings remain.
7. **Edges:** reapplied V11's one-native-pixel original-black finish, retaining
   neighboring metal highlights. Thin weapon parts still excluded from ink strokes.

`review-corrections.json` records changes. `texture-provenance.json` records exact
source cuts, masks and backing pixels. No generated texture artwork or procedural
noise. Final output uses exact original colors; physical raw lighting is intermediate.

## Canvas/pivot

A wider muzzle touched the old right edge in one test view. The canvas was expanded
from 192 to 200 pixels, with orthographic extent increased proportionally: FOUR
pixels added on every side, with NO shrink/resampling of the turret's native scale.
Camera pose, world-pixel density and bearing pivot are unchanged. `attachment.json`
contains the new projected pivot for the preview; the old 192px pivot is not reused.
This does not constitute final OpenRA facing/muzzle calibration.

## Checks

Four final directions: zero non-source colors, binary alpha, no clipping (at least
4px canvas clearance); constant bearing axis verified. Ink pass preserves each
new silhouette exactly. V11 edge scripts were generalized through optional CLI
arguments; their original default inputs/output directories are unchanged.

Remaining caveat: this is a closer reconstruction, not a verified 1:1 replica.
Hidden gun/rear construction is still inferred. No 32-facing export or game binding.

Reproduce from Game:

```text
python tools/art/prepare_sentry_v12.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v12.py
python tools/art/compare_sentry_v12.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v11_edges.py -- --input-attempt attempt-12 --output-attempt attempt-12/edges --blend-file sentry-v12.blend --output-blend sentry-v12-edge-study.blend
python tools/art/finish_sentry_v11_edges.py --input-attempt attempt-12 --output-attempt attempt-12/edges --before-label "V12 before ink" --after-label "V12 1px ink"
python tools/art/compare_sentry_v12.py --final
```

Editable lighting model: `sentry-v12.blend`. Final single-pixel ink remains a sprite
finishing step, not something a plain Blender render reproduces on its own.
Original source art, supplied base and previous attempts remain unchanged.
No commit, push or deployment.
