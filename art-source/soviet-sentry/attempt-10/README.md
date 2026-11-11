# Sentry V10 — reconstruct the previously truncated rear

User feedback: V9 is improving, but the rear ends too early and differs from the
reference; the original's material/depth treatment is still stronger.

Review:
- `comparison-3x.png`: reference / V9 / V10 at native relative scale.
- `rear-change-fixed-pivot-3x.png`: V9/V10 on their original fixed canvases, without
  recentering display crops; useful for seeing exactly where the rear grew.
- `base-assembly-preview-3x.png`: original stationary base, unchanged.
- `rotation-check-2x.png`: four diagnostic angles.
- `sentry-v10-rear.blend`: packed editable Blender 3.6 model.

## Rear geometry

- Extended rear body vertices by 0.38 world units; did not elongate the weapon/front.
- Revealed and extended the separate rear roof step, overlapping the main hatch
  rather than leaving a broad red gap between two apparently separate boxes.
- Extended the rear equipment backing while retaining its forward attachment.
- Replaced the generic evenly spaced five-rib stack with an upper grille and a
  separate lower framed cover, copied from the visible source rear assembly.
- Added narrow return edges/gaskets; moved inferred rear-facing service doors
  with the rear wall.
- The fence and pole around source x184 are scenery, NOT turret components.

The side/back detail hidden in the reference is still inferred, not a claim of
an exact blueprint. The new silhouette is an actual model change, not a 2D stretch.

## Texture/depth work

V9 lighting remains the basis. New rear surfaces use original roof/grille/cover
cuts, including a narrow copied worn roof-return strip. A contaminating neighboring
hatch silhouette inside the roof cut is masked onto a verbatim source-metal pixel;
coordinates and operations are recorded in `texture-provenance.json`.

Contact shading was kept more local, and bearing diffuse/ambient response adjusted
to read more clearly as curved metal rather than globally darkening all artwork.
This does not yet reproduce every hand-authored highlight/shadow cluster in the
original. Source fidelity is still being refined.

## Checks and rendering

`model-change-report.json` verifies 164 existing objects outside the allowed rear
edit set remain unchanged, including the front, gun assemblies, side plates,
camera and bearing pivot. New parts are separate `v10_*` meshes.

Because geometry changed, palette-family masks now come from a dedicated unlit
albedo render of EACH NEW view, not V8's obsolete screen-space pixels. Raw physical
lighting can contain intermediate RGB values; final sprites are quantized without
dithering to exact original colors. Gray materials retain the neutral source subset.

Four angles: 0 source-color errors, binary alpha, matching lit/albedo silhouettes,
no clipping. Minimum canvas clearance is only 1 pixel in one diagnostic view;
a complete rotation/export still needs a full clearance/pivot check. No game binding
or final 32-frame sheet has been produced.

```text
python tools/art/prepare_sentry_v10.py
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v10.py
python tools/art/compare_sentry_v10.py
```

Previous versions, supplied artwork and unrelated user replacements remain intact.
No commit, push or deployment.
