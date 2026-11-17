# Original roof / wall assembly reference

```
--rubberduck-native-corner-lab SOURCE OUTPUT layers=roof
```

This extends the connected original wall contour from the template catalog with
**23 connected, complete original grass diamonds** (corrected from the initial 24;
see the inward-corner correction below). It does not modify production
art, topology, collision or editor behavior.

## Placement, not new artwork

- Ground source: `grassland/grass_tiles.png`, rectangle `(0,0,128,64)`.
- The original RGBA is preserved, including its edge alpha. No recoloring,
  resampling, generated masks or polygon clipping is used.
- A roof-cell offset `(x,y)` has source-space center
  `(64 + 64*(x-y), 64 + 32*(x+y))`. The exported sprite offset is `64,64`.
- The boundary comes from the actual 14 wall-piece placements, not a separately
  maintained guessed silhouette. Straight southwest pieces contribute their
  `(64,32)..(128,64)` roof edge; southeast pieces contribute `(0,64)..(64,32)`.
  Inward connector 16 contributes the two edges through `(64,32)`.
- All eleven front edges must be covered by complete roof diamonds. A cardinal
  connectivity check rejects disconnected roof patches. PNG decode verifies both
  the exact source pixels and the sprite anchor.
- Rear extent is a deliberate reference cutoff at source y=-128. Rear and side
  boundary corners are **not yet matched to original cliff closures**. The visible
  stepped cutoff is not presented as a finished plateau edge.

`roof-placements.tsv` records every placement; `roof-provenance.txt` records the
source hash and limitations. The offline checkerboard composition and the client
map use the same roof placement list and original images.

## Actual-client review

Evidence: `%TEMP%/ymca-original-roof`.

- `reference/ingame.png`: first view on the prior flat grass background. This did
  not clearly distinguish the roof from the background and was insufficient for
  judging the patch outline.
- `contrast/ingame.png`: actual-client view on a sand diagnostic floor. The complete
  roof patch is visible; the front joins the original straight, outer and inward
  wall pieces without a fallback wall. Inspected alongside the offline composition.
- `final`: regenerated after deriving the roof boundary directly from wall pieces;
  roof placements and composed image compared to the inspected contrast version.
- `production-regression`: all ten fitted production cliff images must remain
  byte-identical to installed art after running the existing plateau exporter.

All map heights remain zero and all reference actors are non-occupying decorations.
This tests source-art composition, **not playable elevation, buildability, routes,
ramps, a closed perimeter or editor transaction correctness**. No new movement or
performance certification is claimed. The subsequent [closed original reference](rubberduck-original-closed-reference.md)
now covers the rear/side closure and all four inward turns. Production integration
remains separate.

## Inward-corner correction

The initial evidence above used an incorrect middle point `(64,96)` for source
slot 16, adding one roof diamond hidden behind its rock artwork. Its actual upper
roof point is `(64,32)`, consistent with original grass frame 12. The corrected
23-diamond open patch is under `%TEMP%/ymca-original-closed/roof`; the historical
24-diamond captures above are not a valid roof-footprint oracle. Production was
not affected.
