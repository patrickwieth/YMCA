# New GDI power artwork — intake only, binding pending

User supplied/replaced these originals while turret studies were in progress:

- `mods/ca/bits/gdi/buildings/power plant.png`: now 256x256, alpha bounds
  `(6,7,247,226)`, SHA256
  `bddc2cd6ec016eb637fe31a4509f27d13d726f444e58c8994654754fed89b2d8`.
- `mods/ca/bits/gdi/buildings/advanced power plant.png`: new 256x256, alpha bounds
  `(6,6,250,242)`, SHA256
  `ddd8eb4eea527e16d0e059852eddfbe3946601c3466bc20e910e4ea0ed25717d`.

No new binding, placement calibration, color derivative or gameplay change has been
performed for these inputs. The existing power-plant color-matched derivative is
based on the PREVIOUS original and must be refreshed after reviewing the changed
geometry. Do not claim the replacement is already visible in game merely because
the source PNG changed; Filename-only color overrides may still select the old art.

Next: inspect both pictures, map the advanced plant to its appropriate existing
actor, preserve gameplay/occupied cells, recalibrate visual origin and bib alignment,
regenerate the non-destructive GDI color layer, then run actual loader/alignment and
source-integrity tests. Do not alter the two approved yellow reference buildings.
The subsequent user request prioritized the Nod laser turret rendering study.
