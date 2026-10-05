# Rubberduck shroud: disconnected map-edge strips

## Subsequent correction

The later [height/fog acceptance pass](rubberduck-height-combat-and-soak.md)
found that preserving full fog alpha 255 was incorrect for RGBA: it bypasses the
indexed fog palette's alpha 128 and blacked out explored terrain. The installed
fog derivative now uses 128; full unexplored shroud stays 255. Actual normal/shaded
captures and source-removal checks cover the fix. The layout/edge-field work below
remains, but its original fog-opacity claim is historical, not current.

## Diagnosis and accepted change

The thin water/grass strips above the black map boundary were not terrain cracks.
Removing ShroudRenderer on a diagnostic copy removed the disconnected strips.
The original partial masks include opaque rectangular corners outside their
isometric diamond. Their overlapping coverage cuts off visible portions of adjacent
cells, leaving small isolated strips. Replacing only the full-shroud tile did not
fix the partial-mask problem.

Rubberduck now uses two **procedural**, cell-aligned shroud/fog atlases. These are
not native Rubberduck artwork and are not claimed as unwarped artist assets.
Original shroud/fog PNGs remain unchanged; other tilesets retain their original
filenames. Only the four shroud/fog sequence filenames are overridden for the two
Rubberduck tilesets in `mods/ca/sequences/misc.yaml`.

For each corner bitmask, bilinear black-corner coverage in CPos-aligned u/v is
smoothed over the interval 0.25..0.75. Outside the diamond, u/v are clamped to its
edge field rather than filled with arbitrary opaque rectangle corners. This avoids
both detached visible islands and hard seams from simply clipping the old masks.

The original 48-frame layout, sequence index mapping, full-shroud opacity and
full-fog opacity were retained in this original stage (fog opacity is corrected
by the follow-up above). This original stage changed no visibility calculation,
reveal radius, actor visibility trait, terrain, resource, collision, projection bounds or camera clipping
was changed. The visual feathering is intentionally different from the old art.

## Reproduction

From the configured `Game/engine` utility environment:

```
dotnet bin/OpenRA.Utility.dll ca --rubberduck-shroud-art OUTPUT
dotnet bin/OpenRA.Utility.dll ca --rubberduck-shroud-art OUTPUT fog
```

The command generates `shroud-seams.png`, `fog-seams.png` and individual provenance
files. Copy approved output to `mods/ca/bits/terrain/rubberduck/derived/`.
It validates all 16 masks for range, quarter-turn symmetry, monotone hiding,
fully visible/hidden coverage and matching shared edges, plus exact PNG roundtrip.
Source hashes and the precise construction are recorded beside the installed PNGs.
No runtime generation or additional render layer is required.

## Actual evidence

Root: `%TEMP%/ymca-map-edge-diagnosis`.

- `baseline.png`, `no-shroud.png`: layer isolation on otherwise identical fixture
  terrain. Probe activities removed from these diagnostic copies; these screenshots
  are not cargo movement tests.
- `repaired`, `clipped`, `clipped-both`, `opaque-diagnostic.png`: rejected trials.
  Repairing alpha valleys alone retained the strips; diamond clipping produced
  a dark zigzag; opaque rectangles were only a diagnostic control.
- `continuous/ingame.png`: accepted continuous-field candidate. In the same
  screen rectangle (x=0..1099, y=0..69), scanning each column for visible -> black
  -> visible pixels found **893 disconnected columns before, zero after**.
  Visible means R+G+B > 6. This metric is specific to that fixture/viewport, not
  a universal image-quality score.
- `edges/{north,south,west,east}.png`: actual views centered on all four flat map
  edges, inspected without detached strips. Staggered-grid waviness is retained;
  no map area was cropped or terrain painted black.
- `water-basins`, `archipelago`: normal/shaded generated 4P42 maps with unexplored
  shroud and real cargo. Six runtime assertions each; inspected both screenshots.
- `mountain-fog`: Hill Fortress 4P42, infantry/tanks moving around height-four
  barriers under shroud. **16 completed legs, eight blocked-entry assertions**;
  screenshot reviewed. Reproduce with rock-coast-playtest mode `ground-routes-fog`.
- `editor-shaded`: **93 live editor assertions**, including transactions,
  material-cache checks and save/reload; screenshot inspected.
- `final-art`: deterministic utility output equals the installed files.

The diagnostic map binaries retain their source terrain. Existing height-four
raster teeth and native/fallback cliff joins are not fixed by changing shroud art.
The broader faction, projectile, turret and long-match visibility/performance
matrix remains open in [terrain acceptance](rubberduck-terrain-acceptance.md).
