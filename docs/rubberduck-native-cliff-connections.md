# Native cliff corner and end connections

## Verified scope

The original high-cliff sheet now has four composed, **unwarped** connection fixtures:

| Fixture | Source pieces (`grassland_1x1.png`, 128x256 slots) |
|---|---|
| South-facing convex corner | SW 0/1/2 + SE 5/6/7 + broad corner 24 |
| North-facing concave corner | SW 0/1/2 + SE 5/6/7 + wedge 16 |
| Open southwest-facing wall | rear closure 30 + SW 0/1/2 + front closure 24 |
| Open southeast-facing wall | rear closure 26 + SE 5/6/7 + front closure 24 |

All four were viewed in OpenRA, not just as extracted sprites. The convex corner has a
continuous spreading foot; the concave join does not leave two overlapping wall faces.
The two open runs terminate with actual native silhouettes rather than square cut-offs.

**This fixture is connection calibration, not gameplay certification.** The isolated map
has flat height-zero terrain and nonoccupying decorative actors. It proves neither
walkable roofs nor collision, and its source-rectangle anchors are not terrain anchors.
Height-fitted versions are now integrated on supported generator contours; see
[native cliff production](rubberduck-native-cliff-production.md).

The native walls lean and have spreading feet. Placing them unchanged on current
vertical four-level cliffs would extend art into potentially walkable lower terrain.
The production integration fits their height, reserves low footing and clips against
nearer roofs; it retains fallback rendering where a safe placement is unavailable. Do not silently block paths or flatten existing plateaus
to accommodate them. Other side/rear corners and grass transitions remain uncalibrated.

## Connection rule

`MapGeneration/RubberduckNativeCliffComposition.cs` contains the source geometry and
sample assemblies. Each connection matches **both** a roof point and a foot point.
A single common roof point is insufficient, especially at convex corners.

Source placement offsets use CPos increments: `(dx,dy)` projects to
`(64*(dx-dy),32*(dx+dy))` pixels. These positions refer to source-rectangle origins.

For the convex sample, a SW face at `(0,0)` and SE face at `(1,-1)` have coincident
roof endpoints but separated feet. Slot 24 at `(1,0)` bridges those feet.

For the concave sample, slot 16 at `(0,0)` connects to a SW face at `(1,0)` and an SE
face at `(0,1)`. **The wedge replaces the two central faces.** Overlaying it on both
central faces produces the wrong overlap and silhouette.

For an open SW run, slot 30 precedes the first face at `(-1,0)` and slot 24 follows the
last face at `(length,0)`. The SE equivalents are slot 26 at `(0,-1)` and slot 24 at
`(0,length)`. Slot 24 is not universally an isolated decorative pillar: here it is a
functional part of the native silhouette.

## Reproduction and evidence

After building CA, with the normal utility environment:

```
OpenRA.Utility ca --rubberduck-native-corner-lab SOURCE OUTPUT
```

Only the original `grassland/grassland_1x1.png` is read; `ruin` is excluded. Output inside
the source directory is rejected. Each original crop is retained as `original-SLOT.png`.
The map versions preserve source RGB, size and shape; only near-black matte alpha is
keyed out (maximum RGB <= 8). `pieces.tsv` records source coordinates and keyed counts.
No stretching, repeated texture sampling or artificial corner filling is performed.
Map PNGs embed `FrameSize: 128,256`, `FrameAmount: 1`, `Offset: 64,128`.

The command writes:

- Four checkerboard assembly images and `assemblies.tsv` with every placement.
- `connection-results.txt`: 32 socket-chain checks (four assemblies, lengths 1..8),
  plus rejection of a displaced corner, a wrong corner and a missing wedge.
- `rubberduck-native-corners.oramap`, with embedded pieces and map-local rules/sequences.
  Export is reloaded and custom rules checked. Install only temporarily for review.

Current evidence: `%TEMP%/ymca-native-corners/ingame.png`. In the screenshot, the convex
join is upper left, concave join upper right, and the two open runs below. The installed
4-player gameplay map, its heights, resources, ramps and production artwork were not
changed during this connection-calibration step. Movement tests were not rerun because
this step does not alter production movement; socket tests are not a substitute for them.
