# Scrin rear-ground alignment review

All17 building/defense types were reviewed, plus inherited variants. Tall towers,
antennae and levitating parts are not treated as ground contacts. source-grid.png
and plan.json document the reviewed contact coordinates and requested translations.
No supplied PNG, body scale, occupied footprint, exit or production behavior changed.

Static buildings use the late building-position-feedback layer. The reviewed rear
contacts sit just inside the back grid edges. REAC, NERV and SCRT need3x3 decorative
bibs instead of2x2 so correcting the back does not push roots over the front. Their
new slabs extend FORWARD from the SAME rear grid corner (32px center translation),
not a moved gameplay footprint. The other bib sizes remain unchanged. The prepared
4x4 asset was not needed for this pass and is still unassigned.

MANI was already aligned at the reviewed contacts. PTUR, SHAR and SCOL retain their
existing registration, pivots and muzzles; their fixed ground bases already fit the
1x1 slabs. SFAC.Nukular/SCOL.Temp inherit their parent registration as appropriate.
SILO.SCRIN uses legacy SHP art: its actual idle frame12 was extracted for review,
and its visibly low cluster was raised34px; the1x1 bib stays fixed. Its decoration-only
translation follows without changing the fallback selection shape/click geometry.

The geometry exporter's default image lookup formerly stripped the variant suffix,
incorrectly reporting silo.shp rather than scrinsilo.shp. That diagnostic fallback
now matches RenderSprites' actual actor-name behavior; this is not an asset swap.
The silo review uses extracted SHP pixels, not a fabricated proxy box.

before.png / after.png are native composed reviews, NOT live screenshots. The after
sheet includes the resolved scaled bodies and actual bib offsets. Defense panels
show the fixed bases (the already-validated rotating parts were not changed).
Tests verify the actual runtime offsets, unchanged gameplay and grid anchors.
Originals and earlier reviews are preserved. Live appearance remains pending.
