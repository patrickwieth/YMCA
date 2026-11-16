# Global decorative-bib/grid registration — applied locally

The user requested grid-aligned bibs with buildings moving by the SAME translation,
and identified the China bunkers' cell-based mines as evidence of an incorrect origin.

## Registration

`resolved-before.json` and `resolved-after.json` are engine exports of every resolved
decorative-bib actor/variant. `grid-audit.json` / `grid-audit-after.json` use the actual
128x64 grid, TileScale1448, CPos basis(724,724)/(-724,724). All audited decorative bib
surface centers now lie on integer-cell rectangle anchors within0.1 native pixel.
The TOP surface is used: Nod's7px front lip and Scrin's raised side lip are excluded
from the plane calculation. No bib texture was stretched or overwritten.

`rules/bib-grid-alignment.yaml` changes the shared Building.LocalCenterOffset, not
just the bib sprite. Thus body, bib, attached turret/muzzle/smoke/support origins and
explicit selection bounds receive the same world translation. This intentionally
moves actor centers as a geometric correction; it is not a claim that world-space
muzzle/docking centers stayed fixed. Local attachment offsets and all footprint,
occupied/transit cells, exit cells, mine locations, costs/power/weapons remain intact.
For larger bibs, the nearest grid phase is selected and existing body/bib placement
is preserved; this does not resize the occupied footprint to the decorative slab.

Cell-anchored fallback selection decorations need a separate DecorationOffset to
follow the actor. That new decoration-only field does not change mouse/occupancy
bounds. Explicit entries, including zero offsets, prevent inherited variants from
receiving a parent's translation twice. Old cell-smudge bibs already use map-cell
positions and are not converted into decorative slabs by this operation.

## Bunkers

China's normal/fortified bunkers and AI/infantry variants have no extra bib, so their
shared wrong2x2 center offset0,-724 was corrected to0,0,0 directly. This moves body,
selection and garrison origins32px down, while the eight mine cells stay fixed.
The Soviet/North-Korean derivative retains its own explicitly registered origin/bib.

## Evidence / maintenance

- `applied.json`: exact per-actor translations; `before-after.png`: native composed
  grid review, with unchanged bunker mine-cell centers in yellow (NOT live capture).
- Resolved non-anchor rule fingerprints match before/after. This includes all local
  weapon/overlay offsets and gameplay fields; selection remains in the same position
  relative to its body (within native rounding).
- Legacy duplicate unnamed Inherits keys were named, preserving order. Duplicate
  OREP RevealsShroud and OBLI ReloadAmmoPool@3 blocks were consolidated to their
  PRE-EXISTING effective values, not turned into extra gameplay traits.
- `test_bib_grid_alignment.py` checks live resolved registrations and mine-cell origin.
- `audit_bib_grid_alignment.py` is read-only; `align_building_bibs_to_grid.py` writes
  the layer from the saved baseline/audit. Preserve snapshots before rebaselining.

Source PNGs and authoritative map unchanged. No process was stopped, no commit/push
or release. Live visual/production/docking review remains necessary after restart.
The Gatling platform was subsequently replaced from the supplied native cut; that
separate authorized change is tested in the Gatling suite, not disguised as a grid move.
