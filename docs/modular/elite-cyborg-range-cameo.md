# Elite Cyborg (Range) cameo palette

Local correction after v0.97.5, not committed/pushed/released.

`cyborgelite.range` explicitly removed its parent's `Buildable.IconPalette` with
`-IconPalette:`. The engine consequently used its default `chrome` palette instead
of the parent's `chrometd`. The shared `elite_cyborg/icon` is `rmbocicnh.shp`, indexed
for the TD palette: the wrong interpretation produces the pale blue/green image.

Removed only that palette deletion. Range now inherits the same intended palette
as the normal Elite Cyborg. The two promotion actors already explicitly use
`chrometd`; their icons/overlays were not changed. No artwork, unit rendering,
weapons, range, costs or prerequisites were changed.

`--check-elite-cyborg-icons OUTPUTDIR` checks the resolved actor/image/sequence/file
and palette for both units and both promotions, then exports the real source cameo
through the engine's frame/palette loaders with `chrome` and `chrometd`. The actual
images were compared: TD restores the dark metallic cyborg and green eyepiece.
Evidence: `art-source/elite-cyborg-range-cameo/`. Normal Release build succeeds.
