# GDI color studies — preview only, no runtime overrides

## Authoritative runtime files

The user explicitly requires `mods/ca/bits/gdi/buildings/` to be the final source of
truth. All supplied building PNGs now bind directly from that folder. The old
`sequences/gdi-building-colors.yaml` layer is retired and removed from the manifest.
Its previous content/report are preserved under `retired-runtime-overrides/`.

The bug: replacing `power plant.png` did not affect the running art selection because
a later Filename override still selected `color-matched/power plant.png`. The new
advanced plant also lacked its own binding. Both are corrected; see
`../gdi-power-plant-refresh/README.md`.

`tools/art/harmonize_gdi_yellow.py` now generates **preview derivatives only**. It
never writes a runtime sequence override or overwrites the supplied root originals.
`binding.json.sequence_images` records the actual canonical paths for regression
checks, not paths to be injected into the game. Historical derivative PNGs remain
available in `color-matched/` but are not selected by the active building bindings.

The approved Ion Defense colored base/turret were copied byte-for-byte from their
previous active derivatives into canonical `heavy-blaster-base.png` and
`heavy-blaster-turret32.png`. The main art sequences select those files directly.
Its approved pixels, frame metadata, placement and geometry are unchanged. Further
regeneration/promotion must be explicit; previews cannot silently replace them.

Weapons Factory and Communication Center remain unchanged reference PNGs. The
preview algorithm still matches eligible warm paint by luminance to their sampled
palette, preserving alpha/metadata, neutral metal and cool illumination. The old
orange-reactor protection polygon applies only to the historical 256x281 source.
New cyan reactor glass is naturally excluded by the warm-paint predicate.

The previous supplied-building derivatives are no longer active, so those buildings
now show the exact colors of the authoritative root PNGs. This is intentional, not
a claim that all unmodified originals have identical paint shades.

Tests: `test_gdi_building_colors.py` validates preview generation; the separate
`test_gdi_authoritative_art.py` verifies direct resolved paths, both new plants,
unchanged gameplay/Nod isolation and byte-identical Ion promotion.
