# Heavy Blaster V1 — geometry/material study

First actual Blender reconstruction: `heavy-blaster-v1.blend`.

Four views: 0/45/180/315 degrees. The base is stationary; the upper assembly rotates
around `turret_pivot`. The model includes a chamfered plinth, bearing, paired angled
supports and trunnions, split barrel rails, a recessed rectangular muzzle, rear
receiver, roof vents, side displays, cyan conductors and capacitor banks.

All texture cuts come from the supplied `heavy blaster 3.png` using projective
rectification and nearest sampling. Flat material colors are exact sampled source
colors. No procedural wear/noise or image-generation tools were used. The original
PNG is unchanged. `texture-provenance.json` records the source cuts.

Raw renders contain lighting colors. `review_heavy_blaster_v1.py` maps them back
to the source palette and applies a one-native-pixel exterior source-ink contour.
All four study images have binary alpha, source-only visible colors and unclipped
silhouettes. The 320px canvas adds padding without changing study pixel density.

## Not finished / do not treat as the game asset

The overall form is recognizable but still too simplified compared with the
reference. The barrel/shroud reads too open and straight, the sloped receiver
needs its curved cyan top channel, supports need more shaping, and the base lacks
the reference's layered armor/piping/rivets. Gear/panel details are currently flat
source-cut surfaces rather than fully reconstructed geometry. The unseen rear
is extrapolated, not source-verified.

Therefore this version is deliberately **not integrated into the game** and is
not yet exported as 32 native-size game facings. Review the main view against the
reference first: `comparison-2x.png`. `four-views.png` shows the rotation study.
Material passes 00/01 are retained as earlier intermediates, including their flaws.

Reproduce from Game:

```sh
python tools/art/prepare_heavy_blaster_v1.py
blender.exe --background --python tools/art/model_heavy_blaster_v1.py
python tools/art/review_heavy_blaster_v1.py
```

All Blender work was in separate background processes. The user's open scene and
unsaved edits were not modified. No existing GDI actor, original PNG or gameplay
setting was replaced.
