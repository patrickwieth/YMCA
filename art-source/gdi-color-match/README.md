# Active GDI yellow harmonization

[Matched buildings with the two references](matched-with-references.png) ·
[Before/after](before-after.png).

**Installed**, not just a preview. Weapons Factory (`weapons factory.png`) and
Communication Center (`communication center.png`) are the approved references;
their PNGs and sequence filenames remain unchanged.

## Scope and method

Fourteen color-only PNG derivatives cover the other twelve supplied GDI building
images plus the two Ion Defense layers. Nukular inherits the GDI conyard change.
Bibs, separate Upgrade Center modules, other faction art, actors, weapon stats,
placements, anchors, sprite sizes, selection/collision bounds and footprints are
unchanged. Ion Defense keeps its centered V11 socket, 2x2 bib and V10 glow/contours.

The reference palette consists of 32 frequently occurring yellow/gold/brown shade
colors sampled from the two approved PNGs. Eligible warm paint pixels are mapped
to that palette by nearest luminance. This matches hue/saturation while retaining
lighting differences between faces, rather than flattening every surface to one
RGB value or applying a global brightness filter. Source-painted markings using
the same yellow ramp follow the same mapping. No noise or invented RGB textures.

The first review is retained under `pass-01/`. Four pale warm stone/metal colors
were excluded from the final reference ramp so highlights stay gold rather than
washing out toward beige.

Black outlines, neutral/cool metal and blue/cyan emission are outside the selected
color range. The power plant's orange reactor glass is explicitly protected.
Changed-pixel and protected-region masks are stored here. Every output retains
exact original alpha, dimensions and embedded PNG text metadata, including the
Ion Defense FrameSize/FrameAmount fields.

## Binding and preservation

- Supplied originals remain untouched in `mods/ca/bits/gdi/buildings/`.
- Pre-color Ion exports remain untouched in `.../experimental/` and art-source.
- Active derivatives: `mods/ca/bits/gdi/buildings/color-matched/`.
- `mods/ca/sequences/gdi-building-colors.yaml` contains **Filename overrides only**,
  loaded immediately after `gdi-building-art.yaml` in the manifest.
- Reference names are deliberately excluded from the override layer.
- `binding.json` records both reference hashes, every original/output hash, mappings,
  metadata, changed-pixel counts and every affected image/state binding.

If upstream building art is regenerated, rerun the harmonizer to refresh the active
color derivatives; source-hash tests catch stale derivatives. The original geometry
calibration and art pipeline tests still validate their untouched source images.

## Validation

Release build and GDI-art/Ion/Guard Tower/selection engine checks pass. Color tests
verify exact originals/references, palette membership, masks, alpha, metadata and
actual resolved image/state bindings. Existing alignment, Upgrade Center, supplied
art, Guard Tower, Ion binding and Heavy Blaster tests pass. PNG case audit passes;
protected Nod conyard hash remains `3b1dae6f171c152a5dc6e81ed4afd02290332078`.

The review contact sheets are source composites, with large buildings reduced for
display only. Runtime scale remains 1. Live GPU appearance has not been checked.
No game restart, source map edit, commit, push or deployment.

From Game:

```sh
python tools/art/harmonize_gdi_yellow.py
python -B -m unittest discover -s tools -p test_gdi_building_colors.py
```

Reload through `Game/test-gebaeude.cmd` to view the current art in-game.
