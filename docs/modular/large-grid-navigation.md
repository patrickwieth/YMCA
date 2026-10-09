# 64px vehicle designer grid

This supersedes the earlier 28px and 96px display cells. Every inventory cell is now **64×64 UI pixels**,
including held items and protruding weapon cells. A 2×2 engine, generator or shell block occupies
128×128 UI pixels. OpenRA's UI scaling applies additionally: at 150%, a cell occupies 96×96
screen pixels. Inventory cell counts, placement rules, prices, damage and saved designs are unchanged.

The designer window uses the screen minus 16px margins. The left component sidebar and right
preview/property column retain their widths; additional width goes to the middle canvas. The
canvas and property panel grow with window height. Footer actions remain inside the window.

Large vehicles cannot fit into a typical 1080p canvas at this fixed scale. Instead of shrinking
cells or clipping away inaccessible modules, the canvas supports:

- **Wheel:** vertical scrolling.
- **Shift+wheel:** horizontal scrolling (horizontal wheel events also work).
- **Middle-button drag:** pan both axes, including while holding a part.
- **R / Esc / right click:** existing rotate/cancel/remove behavior remains.

Content and snapped held items are clipped to the canvas, so they cannot cover neighboring UI.
A held item outside the canvas still follows the cursor over the designer, as before. Hit testing
adds the same pan offset that drawing subtracts. Panning is clamped to include all grid cells,
external sockets, silhouettes and the chassis footer. Switching designs/chassis resets the view.
The turret and chassis keep separate origins and non-overlapping grids, including Titan's upright
layout. The 120mm still occupies one interior cell without enlarging the turret or external socket.

The old 28px coordinates remain only as **reference artwork coordinates**: chassis silhouettes
and procedural weapon drawings are scaled to the new cells. They do not introduce a second grid
cell size. Text and outlines remain UI-sized.

Engine/generator portraits use their full-size atlas for the enlarged grid and held items; small
menus keep prefiltered thumbnails. Both runtime atlases have power-of-two dimensions, even when
a source PNG is 192px. See [module icons](module-icons.md).

Tests verify fixed 64px dimensions, unchanged capacities, non-overlapping grids and reachability
of every cell across all silhouettes at 1280×720, 1920×1080, 2560×1440 and 3840×2160. Static UI tests
cover pan-aware hit testing, scissoring and responsive window expressions. Interactive review of
layout/artwork and input behavior remains necessary; these are not GPU acceptance tests.
