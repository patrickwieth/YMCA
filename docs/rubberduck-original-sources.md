# Original Rubberduck grassland sources

Source directory supplied by the user: `../rubberduck tileset` relative to Game.
`ruin` is a separate tileset and is deliberately excluded.

The [expanded template catalog](rubberduck-template-catalog.md) now covers all 33
grassland sheets, alternate exports and `small/`, verifies the ground/cliff packing
in all three combined material sheets, and provides a connected original contour.
Its remaining semantic/height/collision limitations are explicit.

A subsequent [walkable plateau and real-ramp fixture](rubberduck-plateau-ramps.md) now
validates derived engine ramps in all four orientations. This does not mean a native ramp
sprite was found in the original sheets or that the production generator was switched.

## What the examples establish

Both `example1.jpg` and `example2.jpg` were inspected. They show:

- high cliffs around plain, continuous plateau tops;
- low rock banks, detached rocks, open cliff ends and vegetation masking some joins;
- grass/dirt/sand transitions and broad, continuous water texture;
- complete concave corners and cave/overhang shapes, not arbitrary rectangular rock walls.

They are rendered pictures, not terrain topology or heightmap definitions. No unequivocal
walkable ramp was identified. Open cliff ends and flat grass between pieces must not be
silently interpreted as a height ramp. Engine ramp definitions and movement tests remain
necessary for actual elevated plateau access.

## Reproducible importer/audit

```cmd
utility.cmd ca --rubberduck-source-lab "D:\src\YMCA\rubberduck tileset" OUTPUT
```

`RubberduckSourceLabCommand.cs` reads the originals with the engine PNG decoder and
writes outside the source tree. Output includes:

- `source-inventory.tsv`: dimensions, PNG pixel type and SHA-256 of 25 grassland PNGs;
- `source-slots.tsv`: source coordinates, alpha bounds and edge-touch flags;
- annotated source grids and whole paired-corner crops;
- `grassland-plateau-fill.png`: the plain horizontal plateau-fill diamond from
  `grassland_1x1.png`, source slot 90, rectangle (768,1792,128,128);
- `water_v01-diamonds.png` and `water_v02-diamonds.png`;
- independent native-water and native-cliff-gallery `.oramap` fixtures.

Cliff grids are **source slots, not certified independent sprites**. The audited grids
are 128x256 for rock_cliffs/grassland_1x1 and 256x512 for grassland_2x2. Corners span
neighboring slots. In particular, the joined first-row columns 3+4 must not be treated
as unrelated single-cell pieces. Some alpha silhouettes touch grid boundaries; those
need explicit composition/anchor decisions rather than blind slicing.

The plateau-fill diamond is real supplied artwork. It is not a ramp, and it has not yet
changed the generator's collision/plateau topology.

## Water reconstruction

Each original water image is one **768x384, 6x6 isometric patch**, not a rectangular
sprite sheet. For logical tile (x,y), crop origin is:

```
sourceX = 320 + 64 * (x - y)
sourceY = 32 * (x + y)
```

The crop is 128x64, masked to the actual diamond. RGB samples remain native and are not
rescaled. Output frame index is `x + 6*y`. Production use must select by consistent CPos
phase, not random frame indices, to preserve the large texture.

Checks cover all 72 output tiles across both variants: each must contain substantial
visible coverage, and the 36 masks must reconstruct the entire source diamond exactly
once per pixel, without gaps or overlaps. The fixture was reloaded and rendered ingame.
Its outer boundary against the old water marker is intentionally visible as a control;
this is not yet a shoreline implementation or a replacement of the production water set.

## Renderer evidence and limitations

Actual framebuffer screenshots in `%TEMP%/ymca-rubberduck-source`:

- `water-native-ingame.png`: reconstructed native texture at its intended scale;
- `cliffs-native-ingame.png`: unwarped low/high cliff strips, candidate small endings
  and a complete paired corner;
- `mountain-diamond-top-ingame.png`: current generated terrain after explicitly masking
  its upper grass artwork to a diamond.

The cliff gallery has a flat heightmap and gallery-only foot anchors. Exposed black backs,
join placement and the unsupported rear of the paired corner demonstrate why these are
not drop-in production templates. It does not validate ramps, plateau access or walkability.

The rectangular upper-grass mask was corrected in `ExportTerrainFaces` and the derived
production asset rebuilt. This is a limited masking correction, **not** a claim that all
visible cut-outs are fixed: the Mountain Valleys screenshot still has angular contours
and unresolved surface/topology presentation. Native cliff composition and editor support remain outstanding. Filled traversable
starting plateaus and planned ramps are now integrated; see
[Mountain Valleys starting plateaus](mountain-valleys-start-plateaus.md).

Source PNGs/JPGs were not rewritten. Production native-water/native-cliff replacement is
not activated by the audit command. Diagnostic packages are kept in OUTPUT rather than
left in the ordinary lobby map list after screenshot testing.
