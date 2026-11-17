# V8 preview — native contour pixels on visible part/crease edges

[Before/after](contour-comparison-2x.png) ·
[Rotation comparison](rotation-contours-before-after-2x.gif) ·
[32 directions at native 160px](all-32-native160.png).

Both 160px and 192px candidates have 32 directions. The V7 wide geometry and
straight roof conductor remain unchanged. **Preview only, not installed.**

## Method

A separate native surface-ID render locates visible component boundaries and
physical creases, including interior edges. IDs are diagnostic masks, never color
art/textures. The finished pixels still come from the original source palette.
No generic color-contrast filter traces every painted highlight or adds noise.

- Exactly one side of each visible boundary supplies the contour pixels, without
  dilation, sprite resizing or growing the silhouette. Existing alpha is exact.
- Contour ink is source anthracite `(24,30,38)`. Existing darker pixels stay darker.
- Smooth cylinder facets are grouped (25-degree threshold), retaining cap borders
  without drawing a wireframe around every polygon. Other creases use 10 degrees.
- Cyan core, narrow roof metal rails and bearing rims are protected to avoid
  filling these tiny details completely with contour ink.
- Fixed base keeps its source-authored internal edges/details; only its outer
  perimeter is reinforced. No incorrect modeled-base edges are imposed on the
  source-cut base composite.

Thus the pass outlines visible structural edges, not hidden edges, tessellation
seams or every texture-color transition. At native size tiny overlapping contours
cannot each be represented independently without losing details.

## Review and preservation

First-pass comparisons in `pass-01/` show why pure black and unprotected fine rims
were rejected: small bearings became blobs. The selected root previews use the
more restrained source ink and protected narrow parts. All 32 native160 views were
visually inspected. Larger comparison/GIF displays are nearest 2x inspection only.

No new geometry blend is needed: the color model remains
`../attempt-07-preview/heavy-blaster-v7.blend`. The ID script loads it in an isolated
background process and does not save its diagnostic materials over that model.
Final outlines are deterministic native-pixel finishing, not painted source edits.

Tests check exact alpha preservation, changes confined to contour masks, source
palette, protected details, base-interior identity, original source hash and active
V5 game asset identity. No game/rule/muzzle/size choice was installed. No restart,
commit, push or deployment. The Guard Tower and protected Nod asset are untouched.

From Game:

```sh
blender.exe --background --python tools/art/render_heavy_blaster_v8_edges.py
python tools/art/pack_heavy_blaster_v8.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v8.py
```
