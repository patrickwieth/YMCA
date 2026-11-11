# Sentry V5 — mechanical/readability refinement

Review files:
- `comparison-3x.png`: reference / V4 / V5, nearest-neighbour enlarged.
- `comparison-native.png`: native pixel scale (display crop only).
- `rotation-check.png`: four diagnostic directions (0, 45, 180, 315 degrees).
- `base-assembly-preview-3x.png`: composition with the ORIGINAL unmodified base.
  The socket placement is approximate, not an OpenRA integration/offset approval.
- `sentry-v5.blend`: packed editable model, Blender 3.6.
- `attachment.json`: projected bearing pivot, frame size and named muzzle markers.

Changes made and visually reviewed in this iteration:
1. More distinct barrel tubes using discrete source-gray face colors and darker grooves.
2. Thin dark lips on open cluster collars and separate receiver sockets.
3. Common octagonal muzzle surrounds instead of exposed flower-like tube ends.
4. Fewer tiny collar fasteners, making the bands less noisy at native resolution.
5. Mechanical shoulder separation lines and darker plate recesses.
6. Continuous circumference UV for the drum, assembled from THREE different source
   strips, instead of repeating the same narrow scratch strip on every polygon.
7. Rear service doors, grilles, handles and source-cut lower plate sections.
   Rear topology is inferred, not claimed to be visible in the supplied reference.
8. Corrected the 0.12-unit discrepancy between the old model pivot and bearing
   center. All children retain their front-view world positions; the bearing now
   remains stationary while the turret rotates. Assertions check this at each view.

Still not a final match: side armor thickness, front shoulder contour, some UV
edge transitions and the exact concealed gun construction remain approximations.
Texture shading is partly baked into source cuts; these are pixel-art materials,
not physically correct relightable surfaces. No team-color mask or full 32-facing
export yet, and no in-game substitution. The original stationary base is never
resized, recolored or regenerated.

## Source-only texture policy / checks

`prepare_sentry_v5.py` copies previous source-cut patches, extracts three new drum
strips and records source coordinates plus SHA256 in `texture-provenance.json`.
All visible texture colors must occur in `vorlage.png`. No procedural noise,
painted scratches or image-generation tool is used. Nearest sampling only.

`compare_sentry_v5.py` snaps emission/color-management roundoff to the reference
palette without dithering, thresholds alpha, checks all four silhouettes for
clipping and verifies every visible final RGBA value against the source.
It also makes the isolated turret and stationary-base composition previews.

```text
python tools/art/prepare_sentry_v5.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v5.py
python tools/art/compare_sentry_v5.py
```

Input geometry: `attempt-04/sentry-v4.blend`. All previous attempts remain intact.
No commit or push for this iteration; gameplay files and supplied artwork untouched.
