# Authored cliff-foot variants, including both end pieces

The original `grassland/grassland_1x1.png` contains complete grass-foot versions of
most of the high cliff pieces further down the same sheet. Using only its first
three rows omitted these authored details. The matched source variants now feed
the shared plateau export:

| Logical connector / installed asset | Original source slot |
| --- | --- |
| 0, 1, 2 | 36, 37, 38 |
| 5, 6, 7 | 41, 42, 43 |
| 24, outer corner | 44 |
| 26, end piece | 46 |
| 30, opposite end piece | 48 |
| 16, inward connector | 16, unchanged |

Each original rectangle is 128x256 at `(slot % 12 * 128, slot / 12 * 256)`.
These are complete artist-made cliff variants, not loose plants pasted onto the
blocked ground. Source files remain read-only. The original end-piece roof/foot
socket offsets were already matched; **no anchor or collision change** was needed.

## What is and is not preserved

- Logical actor IDs, footprints, reserved dirt cells, terrain, resources, ramps,
  socket coordinates and sequence offsets are unchanged. Editor and generator
  continue to share the same installed images.
- Existing height-four fitting, outer-corner canvas padding, authored grass roof
  layers and matte treatment remain. Thus production sprites are still fitted
  derivatives, not unwarped originals.
- The new source variants include baked-light differences and additional foot
  detail. Do not describe their rock bodies as pixel-identical to the bare versions.
- Extraction verifies the source roof/socket contour in rows 0..95, allowing one
  alpha level of antialias variation. The initial stricter check through row 159
  rejected source 38: it has 42 alpha differences greater than one in that interval,
  beginning at row 124. This is an artist-variant mid-wall difference, not a shifted
  roof socket. The revised assertion deliberately checks the socket region, and
  runtime views cover the resulting body/foot composition separately.
- Outer source 44 has no opaque black matte. Its translucent nearly black foot
  pixels start at row 219 and reach alpha 157. These are retained; the bare outer
  source 24 still uses its separately verified alpha-91 limit. Opaque black matte
  in other source pieces is not indiscriminately restored.
- Inward connector 16 remains byte-identical to the preceding installed asset.

`--rubberduck-native-corner-lab SOURCE OUTPUT feet=grass` exports the selected
source pieces without height fitting, with the existing matte treatment. Its
`pieces.tsv` records logical and source slots separately. The plateau laboratory
uses this mode and writes `authored-foot-provenance.txt`; the installed copy is
`derived/authored-foot-provenance.txt`. Per-roof-layer provenance also records the
actual source slot instead of incorrectly naming the logical connector as source.

## Evidence

Root: `%TEMP%/ymca-end-junctions`.

- `before-26`, `before-30`, `after-26.png`, `after-30.png`: matching actual-client
  closeups; both end directions and their neighboring complete wall runs inspected.
- `shaded-26`, `shaded-30`: actual-client shaded-tileset closeups, both inspected.
- `backup`: previous ten installed images, including the earlier restored outer
  contact shadow; kept outside the source folder.
- `authored-feet`: selected originals, source audit, fitting and PNG pixel/anchor
  roundtrip validation. `final-art` is the deterministic regeneration check.
- `editor`: nine planner preflights and 14 live shape/rollback/Undo/Redo/save checks;
  screenshot inspected.
- `production-final`: 32 movement legs, eight foot and eight height-edge checks,
  home MCV placement/deployment under shroud; screenshot inspected. Production
  map binary and YAML compared byte-for-byte with the preceding foot-shadow export.
- The earlier `production` capture was interrupted by the enclosing command's
  timeout. It is not the final runtime evidence and must not be double-counted.

The grass tufts improve the direct rock/ground contact. The large bare brown apron
at some native/fallback junctions is **still visible**. This change does not conceal
it with green terrain or silently reopen blocked cells. Full junction/terrain-section
acceptance, nonuniform material-overlay lighting and performance reviews remain open.
