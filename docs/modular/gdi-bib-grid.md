# GDI fixed foundation grid

User-requested in Game/isometric; not released or copied into production branches.

All GDI foundation sizes now use the supplied native 128x64 1x1 tile as their
raster reference. Integer translations keep its 64x32 half-cell panel spacing
and joint phase consistent for 1x1, 2x2, 3x3, 4x4 and 4x5, including the inherited
EMP foundation variant and the existing 1x1 gap connectors.

Original PNGs are untouched. Explicit runtime bindings in
`sequences/gdi-building-art.yaml` select `bib2x2-grid.png`, `bib3x3-grid.png`,
`bib4x4-grid.png` and `bib4x5-grid.png`, directly in the authoritative
`bits/gdi/buildings/` folder. The original 1x1 remains the runtime tile. There is
no hidden later filename override or color-matched folder redirect.

The generated textures retain each original canvas and exact alpha silhouette.
Scale, anchor, offsets, buildings, player colors and gameplay are unchanged.
This changes foundation raster spacing, not GDI building artwork.

Reproduce: `python tools/art/generate_gdi_bib_grid.py`.
Validate: `python tools/test_gdi_bib_grid.py` and native
`--check-bib-connections`. The latter resolves all GDI sizes and the EMP variant,
including their actual effective filenames. Source/output hashes are recorded
in `art-source/gdi-bib-grid/manifest.json`.

Native review used NUKE/GTWR for cardinal and diagonal link lifecycle checks,
plus Ion Defense, Construction Yard and Airfield to display all sizes. The test
passed removal, capture, placement, destruction and owner-isolation checks.
Screenshots were inspected: panel density and joints remain consistent across
sizes and connectors. Evidence is retained in `art-source/gdi-bib-grid/`;
the destructive diagnostic map was removed from the runtime map list afterward.

The user's game was not stopped. Rules/sequences must be reloaded to see the
change; the separately pending waiting-lobby DLL installation is still required
before restarting the normal Game runtime.
