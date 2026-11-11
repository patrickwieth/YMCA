# V10 — GDI gold armor and brighter straight roof conductor

**Now installed at native160:** [active binding and engine validation](integration/README.md).
The 192px version remains a preview. Details below describe the color-finishing step.

[Before/after](gdi-gold-glow-comparison-2x.png) ·
[Rotation](rotation-gold-glow-2x.gif) ·
[GDI color context](gdi-color-context.png) ·
[32 native directions](all-32-native160.png).

## GDI armor color

Beige/tan armor and its brown shading are remapped to actual frequently occurring
yellow/gold pixels sampled from the supplied **GDI** conyard, weapons factory and
barracks PNGs. Matching is by luminance within those sampled gold ramps, preserving
texture placement and shading distinctions. No arbitrary new RGB colors are used.
The original buildings are read-only and their SHA256 values are recorded.
Red copper, cool steel, dark contour pixels and existing blue equipment colors are
not globally yellowed. A few warm foundation details use the same mapping; the base
remains a separate stationary image.

## Roof emission

The previously flat cyan core now has a bright `(173,251,251)` center, the same
highlight color visible in the muzzle emitter, and cyan edges. Existing channel
and narrow rail pixels immediately beside it receive a restrained blue/cyan light
accent using source colors. All V9 contour-mask pixels remain exact.

The core coordinate is projected from the actual V7 mesh in each direction, not a
screen-horizontal gradient. This keeps the highlight aligned as the turret turns.
Narrow projected cores retain at least one bright native pixel. This is controlled
pixel-art emission, **not runtime bloom, real dynamic illumination or a blurred
halo**. Alpha, geometry, sprite size, muzzle position and footprint are unchanged.

Both native160 and native192 candidates have 32 directions. No renders or final
sprites were resized. Only the comparison/GIF display is nearest enlarged 2x.
The original V7 Blender file still contains the original color materials; run the
finisher after rendering/contours to reproduce these colors.

The finishing script itself does not install art; the subsequently approved
integration installs V10 native160. Historical V5 exports remain intact.
No game restart, commit, push or deployment. The Guard Tower and protected Nod
conyard remain untouched. Source hashes, color mapping, glow colors and per-frame
bright-pixel counts are in `validation.json`.

Tests check source color membership, original hashes, exact alpha/contours,
changes confined to selected armor colors or the stored glow mask, a bright core
in every direction and unchanged active game assets. 32 native160 views were
visually inspected; this is not live GPU/in-game validation.

From Game:

```sh
blender.exe --background --python tools/art/project_heavy_blaster_conduit.py
python tools/art/finish_heavy_blaster_v10.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v10.py
```
