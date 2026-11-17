# GDI Heavy Blaster — reconstruction and separate native exports

Active art now also uses the [shared GDI yellow color layer](../gdi-color-match/README.md),
matching the unchanged Weapons Factory and Communication Center. Geometry and
pre-color exports below remain preserved; runtime filenames use `color-matched/`.

**Installed: V11 centering + 2x2 bib, retaining V10 native160 turret art.**
[Active binding / engine validation](attempt-11-centering/README.md) ·
[centering preview](attempt-11-centering/centering-and-bib.png).
[V10 GDI gold and brighter roof glow](attempt-10-preview/gdi-gold-glow-comparison-2x.png) · [rotation](attempt-10-preview/rotation-gold-glow-2x.gif).
[Source colors, glow method and validation](attempt-10-preview/README.md).

**Previous preview, not installed:** [V9 cleaner contour placement](attempt-09-preview/contour-comparison-2x.png) · [rotation](attempt-09-preview/rotation-contours-before-after-2x.gif).
[Stable diagonal ownership and validation](attempt-09-preview/README.md).

**Previous contour preview, not installed:** [V8 native edge contours](attempt-08-preview/contour-comparison-2x.png) · [rotation](attempt-08-preview/rotation-contours-before-after-2x.gif).
Uses V7's straight roof; [method and limitations](attempt-08-preview/README.md).

**Previous roof correction, not installed:** [V7 straight blue conductor](attempt-07-preview/straight-roof-comparison.png) · [rotation before/after](attempt-07-preview/rotation-before-after-2x.gif).
The wave is replaced by a straight centered solid; [details](attempt-07-preview/README.md).

**Smaller size comparison, not installed:** [V6 A / B size comparison](attempt-06-preview/start-size-comparison.png).
A is a direct 160px render, B a direct 192px render of the broad V5 geometry.
[Directions and rotation](attempt-06-preview/README.md). V10 now uses the smaller
160px size; the historical 192px alternative remains uninstalled.

Source: `heavy blaster 3.png` (user-supplied, untouched).

Current status: **V10 Ion Defense**, native **160px**, 32 directions, separate base
and turret, Scale 1. Broad cross sections, straight roof conductor, refined native
contours, source-sampled GDI gold and brighter muzzle-like roof emission.
Geometry blend: `attempt-07-preview/heavy-blaster-v7.blend`; native finishing is
applied by the V8/V9 contour and V10 color scripts. Installed export:
`attempt-11-centering/export/` (centered base, exact V10 turret sheet).
The corresponding centered model is `attempt-11-centering/heavy-blaster-v11-centered.blend`.
Muzzle attachment and selection visuals are recalibrated
for the smaller art; gameplay footprint/weapon stats remain unchanged. Live review
is pending. All earlier models/exports, including oversized V5, are preserved.

The 1024x1024 RGBA reference contains 112 colors including transparency and binary
alpha. It looks approximately like a 4x enlargement of 256x256 pixel art, but the
nearest-sampling round trip differs at 466 pixels. Therefore the original remains
authoritative; `attempt-01/reference-native-study.png` is only a working preview.

Reproduce the non-destructive inspection:

```sh
python tools/art/inspect_heavy_blaster_reference.py
```

Intended pipeline, as with the Sentry:

1. Reconstruct the stationary armored base, yaw bearing, paired trunnion supports,
   long split barrel shroud, recessed muzzle, rear receiver and capacitor bank.
2. Use source-cut tan/brown/steel/cyan textures with nearest sampling; no generated
   noise/weathering. Keep the cyan channels and display readable at native size.
3. Review the main reference view before extrapolating the unseen back/other side.
4. Export 32 actual directions directly at game resolution, Scale 1, with matching
   masks/control passes, native-pixel finish, palette and attachment checks.
5. At the user's later request, bind the existing Ion Defense rather than adding
   an independent actor. Preserve weapon/balance settings and document muzzle changes.

Unseen geometry cannot be recovered exactly from this single image. Initial
reconstruction must be labeled as a study, not as a finished reference match.
The user's open Blender session is not read or modified by this inspection.
