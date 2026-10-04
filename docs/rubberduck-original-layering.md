# Original sprite layers: first matched cliff-cap assembly

Later update: [authored cliff-foot variants](rubberduck-authored-cliff-feet.md)
now replace the bare straight/outer/end source pieces. The sections below record
how the roof layers and initial contact-shadow correction were established.

The artist's `example1.jpg` / `example2.jpg` and `grass_cliff_trans.png` were
revisited. The earlier color-replacement attempts did not use the authored grass
transition. They remain rejected; they are not the basis of this change.

## Verified pairing

| Layer | Original source rectangle | Placement in the cliff rectangle |
| --- | --- | --- |
| Concave rock piece | `grassland_1x1.png`, slot 16: 512,256,128,256 | 0,0 |
| Grass wedge | `grass_cliff_trans.png`, frame 12: 512,64,128,64 | 0,32 |

The wedge's tip at 64,0 matches the cap's tip at 64,32. Composition is ordinary
straight-alpha source-over, with no RGB replacement, tint, blur, procedural edge
mask or scaling of the grass. Its native irregular edge replaces the exposed
geometric brown triangle at the top of the inward connector.

This layer intentionally changes the **roof-cap** appearance/alpha. It is not an
alpha-identical recoloring. The isolated original pairing has 1530 source-covered
pixels, including 306 over previously transparent pixels near the cap. All pixels
outside destination rows 32..95 are asserted unchanged. In particular, wall body
and low foot artwork are not painted green.

## Separate reference and production paths

```
--rubberduck-native-corner-lab SOURCE OUTPUT layers=bare
--rubberduck-native-corner-lab SOURCE OUTPUT layers=grass
```

These modes construct a complete concave wall chain with two end pieces using
unwarped original crops. Unlike the historical default corner laboratory, these
reference modes do not key out dark source pixels. The flat diagnostic map is
**not** a traversable elevated plateau and is not collision/height certification.
The comparison establishes a sprite pairing, not a reconstruction of the entire
artist example scene.

The production path retains the existing height-four **fitted cliff body**. The
same original grass frame is placed on that fitted cap afterward, unscaled. It is
precomposed into `derived/plateau-native-16.png`; runtime and editor therefore use
the same artwork and existing clipping/actor placement. It would be misleading to
call the entire production cliff an unwarped original. No other installed native
piece, terrain entry, actor footprint or pathfinding rule is changed by this step.

`--rubberduck-native-plateau-lab` reproduces this asset and checks PNG pixel/anchor
roundtrips. Source files remain read-only. Provenance is installed beside the asset
as `plateau-native-16-layers-provenance.txt`.

## Evidence

Root: `%TEMP%/ymca-original-layering`.

- `grass-contact.png`: original transition-frame contact sheet used to identify
  the wedge; not production art.
- `bare`, `grass`: original-piece assemblies, socket validation and actual client
  captures. The historical 32 chain checks and three rejection cases still run;
  the new closed-end concave assembly is also validated.
- `cliff-review`, `cliff-review-shaded`: actual Mountain Valleys 4P42 before/after
  captures with identical cameras. Both inspected: the flat brown cap triangles
  now have the authored grass edge. `vergleich.png` provides a crop comparison.
- `editor`: nine planner preflights and **14 live editor checks**, including native
  cliff replanning, rollback, Undo/Redo and save/reload. Screenshot inspected.
- `production`: **32 movement legs, eight foot and eight height-edge checks**,
  home MCV placement/deployment under shroud. Screenshot inspected. Exported
  terrain and YAML remain byte-identical to the previous production export.

## Follow-up: straight roofs and the outer corner

The same source-over compositor now also matches:

| Cliff slots | Original grass frame | Original-space placement |
| --- | --- | --- |
| 0, 1, 2 | 0 | 0,32 |
| 5, 6, 7 | 2 | 0,32 |
| 24 (outer) | 16 | 0,-32 |

All six straight variants retain their rock pixels outside the authored roof layer.
The outer frame has nonzero alpha in rows 29..31, projecting three pixels above the
old image rectangle. The initial fixed-canvas export correctly rejected this;
those pixels were **not clipped** and the wedge was not shifted to hide the issue.
Instead, slot 24 now has four transparent padding rows and a 128x260 canvas.

Its PNG offset and `NativeCliffBody` source-roof/depth calculations compensate the
padding. Thus the original rock/foot remains at the same screen coordinates.
The renderer derives padding from the actual frame height and also supports the
previous 128x256 unpadded sprite. The same logic is used in editor previews.
The original-piece laboratory compensates padding in both its map sprites and
its offline compositions. Source-over composition does not rescale either input.

Evidence: `%TEMP%/ymca-original-edges`:

- `candidate`: rejected fixed-canvas attempt; not an accepted export.
- `straight`: six straight edge layers; normal/shaded client closeups inspected.
- `complete`: the padded outer corner, unchanged concave layer and straight layers;
  PNG pixel/anchor roundtrips checked. Installed provenance is per cliff slot.
- `complete-normal.png`, `complete-shaded.png`, `unwarped/ingame.png`: actual-client
  views inspected, with the original-layering captures as the preceding baseline.
- `editor`: nine preflights and **14 live editor checks**, native replan, atomic
  rollback, Undo/Redo and save/reload; screenshot inspected.
- `production`: **32 movement legs, eight foot and eight height-edge checks**, plus
  home MCV placement/deployment under shroud; screenshot inspected. Production
  terrain/YAML remain unchanged.

## Follow-up: restore the artist's outer-corner contact shadow

The default corner extraction keyed every nearly black source pixel to transparent.
That was too broad for slot 24: its 1234 nearly black source pixels all lie in
rows 192..255 and have alpha 1..91, with **no opaque black matte**. They form the
original translucent contact shadow. Extraction now retains them, with a guard
against unexpected opaque/darker or upper-body pixels. This is not a new painted
shadow, blur or procedural fade.

The height-four fitting and four-pixel canvas padding remain unchanged. Compared
with the preceding fitted asset, 1066 previously transparent pixels in destination
rows 192..222 regain source shadow alpha. No previously visible pixel changes.
The other nine fitted pieces remain byte-identical. Original sheets are untouched.
The production shadow still undergoes the existing cliff fitting; it is not an
unscaled raw sprite. The installed slot-24 provenance records this distinction.

Evidence: `%TEMP%/ymca-original-foot-shadows`:

- `before-24.png`, `final-art`: baseline, deterministic regeneration and PNG
  roundtrips; decoded-pixel comparison checks that only absent shadow pixels return.
- `cliff-review.png`, `cliff-review-shaded.png`: actual production closeups, both
  inspected against the preceding material-clipping captures. The contact change
  is subtle; the broad brown collision apron is visibly still present.
- `editor`: nine preflights, 14 live shape/Undo/Redo/save checks, screenshot inspected.
- `production`: 32 movement legs, eight blocked-foot and eight height-edge checks,
  home MCV placement/deployment, screenshot inspected. Terrain/YAML byte-identical
  to the preceding material-clipping production export.

Do not apply blanket shadow restoration to end pieces 26/30: those contain 4815
and 4772 opaque nearly black source pixels respectively. Their matte/shadow
separation requires its own evidence. No replacement end piece was installed in
that shadow-restoration step; the later authored-foot update selects complete
source variants 46/48 without indiscriminately restoring their opaque matte.

End pieces 26/30, broad low-foot composition and complete original-example sections
remain open. General material occlusion now has a
[separate correction and limitations](rubberduck-material-occlusion.md).
These local improvements are not full terrain acceptance. Bridges remain excluded.
