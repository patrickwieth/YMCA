# Original template catalog and connected reference contour

Commands (output must be outside the immutable source directory):

```
--rubberduck-template-catalog SOURCE OUTPUT
--rubberduck-native-corner-lab SOURCE OUTPUT layers=section
```

Evidence: `%TEMP%/ymca-template-catalog`.

## What has actually been established

The catalog includes all **33 PNG sheets in grassland/ and grassland/small/**,
including the `(2)` exports, all cloudy/shaded wall sheets and both water patches.
Ruin and unrelated props are excluded. `index.html` links numbered sheet views and
source crops. `sheets.tsv` records file and decoded-RGBA hashes, dimensions, candidate
grid size and unassigned rows. `frames.tsv` records 3842 candidate rectangles,
alpha occupancy, opaque/translucent nearly-black counts and exact RGBA hashes.
Empty rectangles are listed but not exported as PNGs.

Crops are unmodified source pixels, checked by PNG decode roundtrip. Numbered grids
are separately marked diagnostic images, not replacement artwork. Existing matching
outputs are decoded and verified instead of recompressed. Original file hashes are
checked again at the end. The initial full export exceeded a 160-second command
limit; the subsequent complete run succeeded. A partial export is not evidence of
a completed catalog.

### Verified packing, not guessed transition absence

For each material variant, `*_tiles_w_trans.png` contains:

1. **48 ground frames**, exactly matching the corresponding block in `*_tiles.png`.
2. **32 cliff-transition frames**, exactly matching the corresponding block in
   `*_cliff_trans.png`.

Grass and dirt each have four variants; sand has two. All **800 frame correspondences**
are decoded-RGBA identical, including any empty placeholders. They are recorded in
`verified-material-packing.tsv` and checked by the utility on every run. These are
not 800 distinct semantic transitions: many are material variants or empty slots.

For zero-based variant `v` and local frame `f`:

| Separate sheet | Separate frame | Combined frame |
| --- | --- | --- |
| `*_tiles.png` | `48*v + f`, f=0..47 | `80*v + f` |
| `*_cliff_trans.png` | `32*v + f`, f=0..31 | `80*v + 48 + f` |

This confirms that the combined sheets must not be treated as extra unrelated
transition families, nor should their latter rows be overlooked.

### Families and remaining interpretation work

| Sources | Catalog grid | Interpretation / limits |
| --- | --- | --- |
| `grassland_1x1.png` | 128x256 | Chunky high cliffs, later grass/rubble variants and props; existing calibrated source slots retained. |
| `grassland_2x2.png` | 256x512 | Larger chunky pieces, corners, caves and props; not automatically twice-sized interchangeable 1x1 connectors. |
| `rock_cliffs.png` | 128x256 | Shorter chunky rock family, including rear faces; slot meanings differ from the high sheet. |
| `sheet_walls_1_*` | 128x256 | Visually different, finer-textured wall family; straight, corner, end and surface pieces. |
| `sheet_walls_2_*` | 256x256 | Wider straight sections and their orientation/detail variants. |
| `sheet_walls_3_*` | 384x320 | Larger corners, endings, caves and surface wedges. |
| `sheet_ground_*`, material/transition sheets, `ground_grasses` | 128x64 | Candidate ground grid; decorative and empty entries are not inferred to be collision tiles. |
| `small/sheet_*` | half of the corresponding candidate grid | Separate supplied lower-resolution family. No implicit upscaling or mixing. |
| `water_v01/v02` | whole 768x384 patch | Kept whole here; spatial diamond reconstruction belongs to the existing water audit, not rectangular animation slicing. |

These grids organize inspection. They do **not** establish every piece's sockets,
orientation or physical height. Cloudy/shaded variants are cataloged separately.
Neither `(2)` sheet is an exact decoded duplicate of its unsuffixed counterpart.
`grassland_1x1 (2).png` is 3844 pixels high instead of 3840: its final four rows are
exported separately as `unassigned-tail.png`, not silently discarded or assumed to
be a proven global four-pixel anchor adjustment.

## Connected original reference

`reference-section` uses one **14-piece connected contour**, with all six calibrated
straight variants, both end directions, two convex turns and one concave turn.
Both roof and foot socket connectivity are validated. The recipe is recorded in
`assemblies.tsv`; `pieces.tsv` distinguishes logical slots from actual source slots.

It uses the complete authored grass-foot variants (36/37/38, 41/42/43, 44, 46, 48),
the original inward connector 16, and the already matched authored grass roof
layers. No fallback wall, fitting, recoloring or near-black matte deletion is used.
The original source hashes are recorded in `section-scope.txt`. The outer-corner
four-row canvas padding preserves its authored roof antialias pixels without
moving either source layer.

`ingame.png` is the first actual-client view; `ingame-close.png` is the explicit
zoom closeup. The entire front contour is connected in the inspected view; this
provides a source-art baseline independent of the rejected native/vertical taper.

**This is a wall-contour reference, not a completed terrain section or playable
plateau.** Ground remains a flat diagnostic map. Full roof fill, rear contours,
height mapping, collision, ramps and editor transactions are not certified by this
fixture. In particular, the catalog has not proved that an original transition to
our independently generated vertical fallback exists. Remaining pieces must be
matched within their own source family before adding more replacement geometry.

The subsequent [original roof reference](rubberduck-original-roof.md) adds the
matching full grass diamonds behind this front contour, with a contrasting floor
for inspection. The [closed reference](rubberduck-original-closed-reference.md) subsequently adds
rear/side closure and four inward-turn variants. Playable elevation and production
integration remain separate tasks.

No installed cliff artwork, generator topology or production map was changed.
