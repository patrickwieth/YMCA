# Nod vehicle factory correction — installed locally

User found that AIRS used the supplied Nod war-factory PNG while Legion's factory
used the shared GDI image. Requested: legacy airstrip art, disable new airstrip
construction, all Nod factions get Legion-style ground factory with that Nod PNG.

## Implementation

- AIRS now resolves to `astrip` / original `astrip.shp`, including its16-frame
  delivery animation. `~disabled` removes it from new construction. The erroneous
  tall Nod-factory visual/bib inheritance was removed. The actor is retained for
  existing maps/editor placements; preplaced airstrips are not forcibly destroyed
  or prevented from finishing production.
- WEAP.NOD inherits WEAP.TD, the factory previously shared by GDI and Legion.
  It is available with `proc.td, ~structures.nod` for Nod, Black Hand, Marked and
  Legion. Ground production, price2000, HP150000, power-30, queues, footprint,
  spawn offset and exit cell are preserved from that factory.
- WEAP.NOD selects `weap.nod.png`, directly binding the unchanged authoritative
  `bits/nod/buildings/warfactory.png` atScale1, Bottom offset0,0. Idle/damaged/dead/
  placement use that PNG. No borrowed GDI door overlay. Nod3×3 decorative bib and
  the former Nod-art selection envelope replace the GDI envelope/bib.
- The factory retains the old `weap.td` technology token for shared-tech
  compatibility, but not the separate `structures.weap.td` build permission.
  Its infiltration proxy is Nod's `veh.nod.stolen`, as on the old Nod airstrip.
- The construction yard grants `structures.weap.td` only to GDI's four factions;
  Legion no longer gets the GDI/shared factory as a second menu choice. GDI's
  WEAP.TD resolved actor is byte-for-byte unchanged from the before snapshot.
- Medium China construction bindings added for both newly active image namespaces.
- Bot factory/production/MCV lists include WEAP.NOD. Nod factory limits/fractions
  now select it rather than the disabled AIRS. Existing AIRS can still be recognized
  by bots on legacy maps; unrelated faction factory priorities are unchanged.

Both disposable test-map generators replace only Nod display Actor10 at78,6 with
WEAP.NOD. Actor9 keeps the restored legacy airstrip; GDI Actor71 stays WEAP.TD.
Original testmap1.oramap is not edited. The dedicated test map was regenerated;
the normal building launcher regenerates its map at startup.

## Checks / limits

Runtime CA build succeeded. `--check-nod-war-factory` loads actual PNG/SHP/bib/
construction/placement states and trait dependencies. Python tests check all four
Nod build gates, unchanged GDI and Legion-style factory mechanics, correct image
paths, disabled legacy airstrip, AI lists and test-map separation. GDI authoritative
art regressions also pass. Source file hash is recorded in `art-source.json`.

`current-placement.png` is an offline native composite, not a gameplay screenshot.
No live vehicle drive-out, bot-match or build-menu click-through was observed.
Restart via `test-gebaeude.cmd` for current runtime rules and the updated test map.
No commit/push/release/deployment.
