# V9 — cleaner, consistent contour ownership (preview only)

[Before/after V8 vs V9](contour-comparison-2x.png) ·
[Rotation comparison](rotation-contours-before-after-2x.gif) ·
[32 native views](all-32-native160.png).

V8 selected screen-left/top pixels at each surface boundary. At a diagonal, the
horizontal and vertical tests could therefore draw on opposite sides, producing
doubled stair steps and inconsistent placement as the turret rotated.

V9 assigns each boundary to the same surface ID, with explicit exceptions for
protected cyan/metal details. It no longer chooses the side according to screen
direction. Unit tests verify single-sided diagonals and rotation/mirror equivariance
of the raster operator. This does not claim subpixel animation is flicker-free:
newly visible geometry still changes with each native view.

Interior strokes use a uniform near-black `(14,13,17)` already in the source
palette. They no longer alternate between old black texture fragments and lighter
anthracite. Existing V7 outer silhouette pixels remain exact. Thin roof rails,
cyan conductor and bearing rims stay protected; no blur or dilation is used.
Base source details are preserved; only its outer edge is reinforced.

The first stable-ownership comparison is archived in `pass-01/`; selected root
files also use the more consistent dark interior stroke. This is a targeted line
quality refinement, not a reconstruction of all remaining busy source textures.

Both 160px and 192px candidates have 32 views. V7 native color renders and V8 native
surface-ID masks are reused without resizing or changing geometry. The open Blender
scene, original source, Guard Tower, map and active V5 game files are untouched.
No game installation, restart, commit, push or deployment.

Reproduce from Game (V7/V8 prerequisites retained):

```sh
python tools/art/pack_heavy_blaster_v9.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v9.py
```

Validation covers all 64 output frames: exact alpha, palette, changes confined to
stored contour masks, original outer silhouette and unchanged active game PNGs.
The packing step also verifies protected details byte-for-byte. All 32 native160
views were inspected; preview enlargement is 2x nearest only.
