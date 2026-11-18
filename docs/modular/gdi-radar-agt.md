# GDI radar gold and Advanced Guard Tower foundation

Follow-up after v0.97.3. Local Game/isometric changes only, not yet released.

- ATWR now has the existing 1x1 GDI fixed-grid bib and corner-color treatment,
  hidden during construction. The prior legacy bib remains removed. Model,
  voxel turret, sockets, footprint and weapons are unchanged.
- Radar paint uses seven bounded authored gold tones from the existing GDI
  Construction Yard/Barracks palette. Near-identical source pixels within two
  RGB steps retain their small variations. Metals, white flags, outlines,
  transparency and geometry are unchanged. This is not a global image filter.

The untouched supplied radar is archived at
`art-source/gdi-radar-gold/radar-original.png`. After comparing the derivative
against other GDI buildings, the approved-request color adjustment was promoted
explicitly to the authoritative `bits/gdi/buildings/radar.png`. Runtime filenames
remain unchanged; there is no hidden alternative-color override layer.

Reproduce with `tools/art/match_gdi_radar_gold.py` (review) and `--install`
(explicit promotion, guarded against overwriting a newer supplied replacement).
Validation: `tools/test_gdi_radar_agt.py`, native `--check-bib-connections` and a
native lifecycle map using ATWR as the defense. Cardinal/diagonal links, capture,
removal, placement and destruction passed. The final radar review supplies power
to its owner so low-power dimming cannot disguise the actual paint color.

Evidence: `art-source/gdi-radar-gold/` (source comparison, powered native render,
lifecycle log). The local destructive test map is removed from runtime map lists.
No game process belonging to the user was stopped; no new release was triggered.
