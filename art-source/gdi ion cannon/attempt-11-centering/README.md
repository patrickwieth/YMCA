# Active Ion Defense — centered bearing/turret and 2x2 bib

[Before/after and pivot markers](centering-and-bib.png) ·
[Rotation composite](rotation-centered.gif) · [Multi-view](rotation-contact-sheet.png).
These are source composites, not live game captures.

## Correction

The turret was aligned to its modeled socket, but the **socket itself was off-center
on the armored platform**. The platform polygon centroid is `(87.1054,93.9930)` in
the native frame at deck height .65. Moving the fixed bearing and turret together
six native pixels right and four down centers the pivot at the original .7 model
height to within 0.46px. No width, size, texture or glow change was needed.

The new saved background model moves both parts in world XY with unchanged model
Z. Only the fixed-base render is recomposited with the preserved original-source
perimeter and the approved native finishing/colors. The 32-facing turret PNG is
**byte-identical to V10**. Game attachment offsets supply its six-right/four-down
placement; no sprite resampling/repainting or extra rotation is introduced.

The changed base silhouette includes removal of the old offset bearing's overhang.
The stationary-base control renders at two turret angles are pixel-identical.

## Active binding

- Existing `Ion_Defense`, Scale1, native160, 32 directions, original 2x2 occupancy.
- Base offset `-7,-24`; turret sequence offset `-1,-10` remain unchanged.
- `Turreted.Offset` becomes **`0,0,226`**, previously `0,-136,317`.
- Local muzzle `1631,0,675`, CameraPitch85 unchanged. Moving the attachment also
  moves the physical projectile origin; weapon stats/range/turn speed stay intact.
- `WithIdleOverlay@GDIBIB` uses existing **gdibib2x2**, original 256x128 PNG,
  Scale1, Bottom anchor, ZOffset -4096. Legacy WithBuildingBib remains removed.
- The new bib is decoration only, visible after construction. It does not change
  occupied cells, hit shapes, mouse bounds, pathfinding or costs/power/HP.
- Visual selection stays `1901,1901,0,0`, height66 and contains all shifted views.

Release build and actual loader pass, including the 2x2 bib. Engine centering check:
**0.464 native pixel** from the platform centroid at pivot height. All 32 model vs
engine muzzle checks remain within **0.609 pixel**. Gameplay baseline, exact turret
preservation, stationary base, selection envelope and prior art tests pass.
Live appearance/firing still needs review. No running game was stopped/restarted.
No commit, push or deployment. Original source PNGs and protected Nod conyard stay
untouched. The test-map actor remains at `169,-35`, beside the GDI Naval Yard.

The subsequent [GDI-yellow harmonization](../../gdi-color-match/README.md) is now
installed. Active sequence filenames point to color-only derivatives under
`bits/gdi/buildings/color-matched/`; this directory's V11 exports remain the
unaltered pre-color geometry baseline. Centering, alpha and bib are unchanged.
Rerun the harmonizer after regenerating the underlying art.

Reproduce from Game:

```sh
blender.exe --background --python tools/art/center_ion_platform_v11.py
python tools/art/integrate_ion_defense_v11.py
python -B -m unittest discover -s tools -p test_heavy_blaster_v11.py
python -B -m unittest discover -s tools -p test_ion_defense_art.py
```
