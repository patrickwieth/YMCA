# Material overlays behind foreground terrain

## Reproduced fault

The `occlusion-water` fixture placed a real height-four roof in front of the
secondary-water gallery. The base terrain was correct, but the later water overlay
painted blue diamonds across the foreground grass roof. This was a rendering-order
fault, not missing water artwork or incorrect collision.

## Shared correction

`RubberduckMaterialLayer` wraps the ordinary terrain sprite layer. Unobstructed
cells still use that layer. For obstructed cells it projects the nearer terrain's
four corners, intersects each image column with the projected surface, and compares
physical depth before excluding covered pixels. Visible runs become sprite slices
referencing the existing atlas. Fully hidden cells submit no slices.

- Ground transitions, both spatial water families, legacy water 1070/1080 and the
  existing soil fringe use the same clipping path.
- No source image, PNG alpha, material mask, terrain ID, height, actor or collision
  is changed. There is no new texture atlas per clipping shape.
- All four supported ramp directions are included. Equal-height surfaces do not
  obscure each other. The search extends to the grid's maximum terrain height.
- Terrain revisions invalidate distant occlusion, including lowering and Undo/Redo.
  Ordinary neighbor updates still handle material selection independently.
- The soil fringe no longer drops an entire cell just because part of it is behind
  a roof. This restores visible portions without painting dirt across the roof.
  The historical conservative apron scan remains a diagnostic oracle only.
- Sprite offsets are applied once, by `DrawSprite`. An early direct-draw candidate
  added them twice; it was corrected before the final captures.

## Evidence

Root: `%TEMP%/ymca-material-clipping`.

- Baseline: `%TEMP%/ymca-material-occlusion/before/ingame.png`;
  `vergleich.png` shows an unscaled before/after crop.
- `final-occlusion-water/ingame.png` and `final-occlusion-water-shaded/ingame.png`:
  actual-client captures inspected. The roof is grass rather than blue water;
  water outside the covered area remains visible. These are isolated diagnostic
  maps, not claims that the entire gallery is visually finished.
- `final-editor-corners` and `final-editor-corners-shaded`: 98 checks per final
  editor session, including exact Undo/Redo and save/reload. New checks cover four
  water IDs, equal-height surfaces, height-four/eight occluders, partial coverage
  for all four ramps, disjoint slices and restoration after lowering/terrain edits.
  Logs append across reruns: do not count 196/294 accumulated lines as additional
  independent cases.
- `cliff-review.png` and `cliff-review-shaded.png`: production cliff-foot closeups
  after switching the existing fringe to partial clipping, both inspected.
- `production-final`: Mountain Valleys 4P42 under fog; 32 movement legs, eight
  blocked-foot and eight height-edge checks, plus home MCV placement/deployment.
  Final screenshot inspected. Exported terrain and YAML are byte-identical to
  the preceding original-edges export. The water diagnostic also retains its
  binary terrain; its YAML differs only in dedicated probe-log output paths.

A proposed independent indexed mesh was **not installed**: its index-buffer
allocation required an internal engine context API. The failed candidate is stored
outside the repository as `rejected-index-buffer-candidate.cs`; no engine patch or
reflection workaround was added.

## Still open

This does not repair the shapes of cliff end pieces or the broad blocked dirt
aprons. It also does not establish complete original-example reconstruction.
The clipped fragments currently use cell-center terrain lighting, like ordinary
sprite fragments, rather than the base terrain layer's four-corner interpolation;
nonuniform local lights need separate review. GPU/frame-pacing and large-map
worst-case clipping workloads are not certified by the geometry/editor tests.
The existing procedural material blends and soil fringe remain derivatives, not
newly discovered authored transition sprites. Overall visual acceptance is open.
