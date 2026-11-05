# GDI building artwork and Research Core

Local `Game / modular` integration. No release or production changes.

## GDI

`rules/gdi/building-art.yaml` binds nine buildings to independent sequences in
`sequences/gdi-building-art.yaml`. The original PNG bytes are unchanged, Scale 1.

| Actor | PNG under bits/gdi/buildings | Bib |
| --- | --- | --- |
| AFAC (also AFAC.Nukular) | conyard.png | 4x4 |
| NUKE | power plant.png | 3x3 |
| PYLE | barracks.png | 3x3 |
| HQ | radar.png | 3x3 |
| REP | service depot.png | 3x3 |
| HPAD.TD | helipad.png | 3x3 |
| PROC.TD | refinery.png | 3x3 |
| AFLD.GDI | airfield.png | 4x4 |
| GTEK | techcenter.png | 3x3 |

Foundations are decoration-only overlays, not new collision/buildable areas.
Footprints, selection geometry, power, exits and docking remain unchanged.
`sequences/gdi-building-placement.yaml` moves all supplied body states 32 screen
pixels upward from their initial bottom-anchor placement, toward the rear bib
edges. Bib positions/sizes and gameplay pivots are unchanged. This exposes more
front apron visually; it does not add new pathable cells. Visual refinement
in-game is pending.
The supplied 1x1/2x2 bibs are registered for future use.
NUK2 and the vehicle factory retain their existing artwork (no dedicated new PNG).
Normal/damaged/active/production states use the same supplied PNG; there is no
separate damage artwork. Legacy GDI make animations/icons remain. The refinery's
old resource tower overlay is hidden so it does not overlap the supplied PNG.
Nod derivatives and the color picker are explicitly isolated from GDI bibs.
Named inheritance keys prevent duplicate-key errors when merging the visual overlay.

## Research Core

`FCOM.checkpoint` keeps its actor ID, capture/experience/strategic-point mechanics
and footprint, but displays **Research Core** and uses
`bits/misc/buildings/research core.png`. Ordinary FCOM remains Forward Command.

The 1024x1024 source is not resized: sequence Scale 0.25 renders it at a 256px
canvas width. The 68px source offset becomes 17 screen pixels. All four states
use the same supplied PNG. A separate `FCOM.checkpoint.Husk` is named
**Ruin (Research Core)** and both capture and infiltration restore
`fcom.checkpoint`, not ordinary FCOM. It shares the core image until dedicated
ruin artwork is supplied.

## Filename casing

15 uppercase supplied-building PNG names were normalized, including the older
Nod Obelisk. References in YAML, utility checks and documentation were updated.
Tracked case-only renames use two-step `git mv`; these three renames are staged,
without committing. All other changes remain local. Image contents are preserved.
Protected Nod `conyard.png` remains blob `3b1dae6f171c152a5dc6e81ed4afd02290332078`.

`python tools/audit_png_case.py` checks supplied building assets plus changed/new
PNGs and exact casing of explicit PNG sequence paths, even on Windows. `--fix`
checks name collisions, preserves image hashes and updates filename references.
A lowercase filename alone is not a substitute for matching reference casing.

## Validation

- Release solution build passed (existing warnings remain).
- `--check-gdi-building-art` passed: bindings, dimensions, foundation isolation,
  core naming, checkpoint traits and restoration.
- `--check-selection-traits` passed for 1792 concrete actors.
- `--check-allied-building-art` passed.
- `python -B -m unittest discover -s tools -p 'test_supplied_building_art.py'`: 5 passed.
- PNG casing audit: 84 assets, no errors.
- Existing Allied Python alignment suite: 15 tests, 6 subtest failures due to
  changed APWR/Helipad alpha contours relative to historical registration baselines.
  No image or baseline was overwritten to force these tests to pass.
- Full-mod lint is not clean: existing missing actors/prerequisites/condition
  diagnostics, then the TEMPERAT legacy `fcom.bib0` sequence fails tileset lookup.
  This is not a successful full sprite-load or live rendering test.

Next: inspect the GDI buildings and Research Core in-game and refine offsets if
needed; provide dedicated damage/ruin artwork if desired.
