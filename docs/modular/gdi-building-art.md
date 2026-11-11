# GDI building artwork and Research Core

Local `Game / modular` integration. No release or production changes.

## Helipad — corrected bib registration

The body now uses Bottom offset `-1,-25` (43px down from the rear-edge calibration),
and its visual selection ground center moves to Y=0. The 3x3 bib stays fixed.
Landing/docking, exits, occupied 2x3 cells and all gameplay remain unchanged.
[Before/after and regression checks](../../art-source/gdi-helipad/README.md).

## Guard Tower — supplied native PNG

The existing `GTWR` now uses the original `guard tower.png` (128x150, Scale 1),
with its own foundation, a separate native **1x1 gdibib1x1** overlay and original icon/muzzle animation. Gameplay stats and
footprint remain intact; muzzle height is adjusted to the upper windows.
[Binding, source hash and checks](../../art-source/gdi-guard-tower/README.md).
The existing test-map Actor75 at `141,-15` uses it. Live review pending.

## Ion Defense — V10 art, V11 centering and 2x2 bib

The approved V10 is now installed at **160px native resolution**, Scale 1: wider
cross section, straight glowing roof conductor, refined contours and GDI-gold armor.
The larger 192px alternative remains a preview; oversized V5 is archived.

The existing `Ion_Defense` uses separate fixed-base and 32-facing turret PNGs
through `ion_defense.native`. Its 2x2 gameplay footprint, weapon stats, cost, HP,
power and production remain intact. No runtime image scaling.
Turret/muzzle coordinates are explicitly recalibrated to the smaller geometry;
all 32 engine muzzle projections are within 0.609 native pixel.
V11 moves the fixed bearing and turret together onto the platform center (about
6px right / 4px down) and adds the original native `gdibib2x2` as a decorative
idle overlay. Turret PNG bytes remain V10; `Turreted.Offset` is now `0,0,226`.
Engine centroid error is 0.464px. No footprint or collision changes.
See [active binding and validation](../../art-source/gdi%20ion%20cannon/attempt-11-centering/README.md).
The existing test-map Actor84 at `169,-35` uses it. Live visual/firing review pending.

## GDI

**Color harmonization is installed:** Weapons Factory and Communication Center
remain unchanged as yellow references. Other supplied GDI building images, Guard
Tower and Ion Defense use color-only derivatives in `bits/gdi/buildings/color-matched/`.
`sequences/gdi-building-colors.yaml` overrides Filename only; original PNGs, alpha,
metadata, placements and all gameplay values are preserved.
[Comparison, source hashes and tests](../../art-source/gdi-color-match/README.md).

`rules/gdi/building-art.yaml` binds thirteen buildings to independent sequences in
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
| AFLD.GDI | airfield.png | 4x5 (unscaled source-copy composition) |
| GTEK | techcenter.png | 3x3 |
| WEAP.TD | weapons factory.png | 4x4 |
| EYE | communication center.png | 3x3 |
| UPGC | upgrade center.png | 3x3 |
| SYRD.gdi | naval yard.png | none (water visible) |

Foundations are decoration-only overlays, not new collision/buildable areas.
Footprints, mouse-interaction bounds, power, exits and docking remain unchanged.
`sequences/gdi-building-placement.yaml` now uses per-building native offsets,
paired with `IsometricSelectable.DecorationBounds`/`DecorationHeight` in the visual
rules. This supersedes the initial blanket 32px upward shift. The body/selection
graphics are registered by reviewed rear ground contacts on the two back bib
edges, following the rear-body convention used for Allied/Nod art. Selection
frames follow the native artwork and remain on the bib. Existing bib offsets and
gameplay pivots stay fixed; only the airfield receives a longer decorative bib.
No additional cells become occupied or pathable.
The supplied 1x1/2x2 bibs are registered for future use.
NUK2 retains its existing artwork (no dedicated new PNG).

The new factory (384x384) and advanced communications center (256x256) are rendered
at Scale 1, now using bottom-anchor offsets `-27,-34` and `-29,-52` respectively.
`Weapons Factory.png` was renamed
to `weapons factory.png` without changing bytes for exact Linux paths. The factory
placement preview also uses its PNG; its obsolete `WithProductionDoorOverlayCA`
is removed entirely so the SHP door does not overlay the supplied artwork. Vehicles
pictured in the factory PNG are static artwork, not additional spawned units.
EYE keeps its radar, Ion Cannon, power, prerequisites and gameplay footprint;
HQ continues using `radar.png`. EYE's six unnamed inheritance keys were given
unique names in their original order so the visual override can merge safely.

Source SHA256:
- weapons factory.png: `f6936d61011ee47d25fabdac16230def09537a0d56061ef4baef8892be63c94f`
- communication center.png: `304af5ccb955288cca77a8aeabdea9d84b4d1a3526759e12483d5b7c0e7c5e4e`

Native-size placement preview: `gdi-new-buildings-preview.png`, reproducible with
`python tools/art/preview_gdi_new_buildings.py`. It uses the supplied bib and body
PNGs without resampling and the same bottom-anchor offsets as the sequences;
it is an illustration, not a live-game screenshot.

New-building validation: Release build passed with four existing warnings; all
seven supplied-art Python tests, the 11-building GDI utility check and 1814-actor
selection check passed. Resolved-rule before/after comparisons for WEAP.TD and EYE
changed only RenderSprites, legacy/new bib traits, and the factory's visual door.
No in-game visual/docking test is claimed; offsets remain subject to live review.
Normal/damaged/active/production states use the same supplied PNG; there is no
separate damage artwork. Legacy GDI make animations/icons remain. The refinery's
old resource tower overlay is hidden so it does not overlap the supplied PNG.
Nod derivatives and the color picker are explicitly isolated from GDI bibs and
GDI decoration bounds. Nod's own selection heights/ground shapes are preserved.
Named inheritance keys prevent duplicate-key errors when merging the visual overlay.

## Upgrade Center addition

`UPGC` now uses `upgc.gdi.png` with the original 256x256 `upgrade center.png`
at Scale 1 / Bottom anchor / Offset `1,-21`. Idle, active, damaged and dead
states all use this PNG. Its 3x3 decorative bib and 272x136 visual selection
base (height 176) follow the same rear-contact calibration as the other GDI art.

[Upgrade Center preview](gdi-upgrade-center-preview.png) shows the base building,
not an upgraded/live-game actor. Make/icon, all upgrade-module SHPs, flare and
shield effects are inherited unchanged. Module/effect attachment positions still
need live visual review; no new module artwork or socket calibration is claimed.
Upgrades, prerequisite grants, support powers, weapons, delivery, exits, occupied
cells and mouse bounds are unchanged. Duplicate inheritance keys were named in
original order, and an identical duplicate ammo-pip declaration was collapsed;
the resolved actor still has exactly the same single ammo-pip trait.

Source SHA256: `b47fb503e756f3ef48c905ea5eee50f87635761b5eace6bd1606461ceedcebc6`.
`test_gdi_upgrade_center.py` compares all nonvisual traits against a pre-edit
fingerprint and verifies every inherited non-body sequence stays identical.
The validation report is `gdi-upgrade-center-validation.json`. Release build,
12-building GDI utility, Allied utility and selection check pass. The prior
11 GDI building registrations and reference Allied/Nod geometry are unchanged.

## Naval Yard addition

`SYRD.gdi` now uses `gsyrd.gdi.png` with the untouched 300x256 `naval yard.png`,
Scale 1, Bottom anchor and Offset `8,5`. Normal and damaged body states use the
PNG; original construction, icons and effect sequences are inherited unchanged.
There is NO bib under the naval yard: the real map water remains visible through
and around the piers. Its 3x3 calibration envelope is mathematical, not a sprite. Water-only
placement, the `XXX xxx XXX` footprint, production, repair, exits, hit shapes,
sonar infiltration and mouse bounds remain unchanged. The ship pictured in the
PNG is static artwork, not an additional spawned unit.

[Naval Yard preview](gdi-naval-yard-preview.png) is reproducible with
`python tools/art/preview_gdi_naval_yard.py`; actual water/ship-docking review
remains pending. The disposable test-map water preparer now fills the Allied AND
GDI yard footprints plus their one-cell exit borders with existing water terrain.
Regression tests are included in `test_gdi_upgrade_center.py`;
the pre-edit gameplay fingerprint and `gdi-naval-yard-validation.json` verify
that only sprite binding and selection decoration changed.
All twelve previous GDI registrations and reference geometry are unchanged.

Source SHA256: `de10cd6e13323fa83ea99c3731bd1463799f29867c0296bf836d489473e159e8`.

## Per-building alignment and selection review

The old GDI decoration height was 30px for every supplied building, with several
footprint-derived ground diamonds centered 32px right/16px down relative to the
new body image. Moving PNGs alone did not move or resize those selection frames.

Current reviewed values (native source pixels; height is decoration-only):

| Actor | Body offset X,Y | Visual ground width/depth | Selection height |
| --- | --- | --- | ---: |
| AFAC / AFAC.Nukular | 1,-39 | 320x160 | 143 |
| NUKE | -14,-36 | 272x136 | 190 |
| PYLE | 9,-56 | 224x112 | 109 |
| HQ | -49,-45 | 224x112 | 168 |
| REP | 6,-12 | 288x144 | 134 |
| HPAD.TD | -1,-68 | 208x104 | 46 |
| PROC.TD | -32,-40 | 224x112 | 107 |
| AFLD.GDI | 19,4 | 416x208 | 181 |
| GTEK | 16,-32 | 240x120 | 160 |
| WEAP.TD | -27,-34 | 400x200 | 169 |
| EYE | -29,-52 | 256x128 | 201 |
| UPGC | 1,-21 | 272x136 | 176 |
| SYRD.gdi | 8,5 | 304x152 | 88 |

`tools/gdi-building-alignment.json` records the source hashes, offsets and exact
world-unit decoration bounds and reviewed native ground-contact points. The two
rear contact extrema are aligned using isometric axes `u=x+2y`, `v=-x+2y` with a
small clearance inside the rear bib edges. This accounts for asymmetric compounds;
centering the PNG canvas or only fitting the frontmost ramp does not do that.
A first lower-contour-only proposal was discarded where it put real feet beyond
the rear bib edges. The final positions were reviewed again with resolved overlays.

Selection diamonds follow the art's ground arrangement, contain the complete
silhouette including towers/antennas, and stay within the bib. They are visual
UI envelopes, NOT collision footprints inferred from alpha. Interaction bounds
and occupied cells stay unchanged. No original PNG was cropped, resized or recolored.

The airfield's long native runway needs a 4x5 decorative base. `bib4x5.png` is a
576x288 composition of two WHOLE unscaled supplied `bib4x4.png` images at offsets
`64,0` and `0,32`, one cell apart. It adds no painted pixels or new colors and does
not modify the supplied bib. Provenance is in `tools/gdi-bib4x5-provenance.json`;
reproduce/check with `python tools/compose_gdi_airfield_bib.py [--check]`.
The actual AFLD.GDI footprint remains 3x2, with unchanged aircraft/docking behavior.

`gdi-building-alignment-preview.png` overlays the ACTUAL resolved engine selection
polygon in green and its ground corners in amber; pink marks the actor pivot.
`gdi-building-alignment-geometry.json` also includes unchanged Allied/Nod reference
registrations. These are reproducible diagnostics, not live-game screenshots.

The exporter accepts optional actor names, keeping its original Allied defaults:

```text
dotnet engine/bin/OpenRA.Utility.dll mods/ca --export-allied-selection-geometry AFAC NUKE PYLE HQ REP HPAD.TD PROC.TD AFLD.GDI GTEK WEAP.TD EYE
python tools/calibrate_gdi_building_alignment.py docs/modular/gdi-building-alignment-geometry.json --preview docs/modular/gdi-building-alignment-preview.png
python -B -m unittest discover -s tools -p 'test_gdi_building_alignment.py'
```

Validation: six new alignment tests and seven supplied-art tests pass; resolved
GDI/Allied utility checks and the 1814-actor selection check pass. A 19-actor before/
after comparison confirms only GDI selection-decoration fields and the airfield's
decoration-image binding changed in rules, including the Nukular selection variant.
Nod/colorpicker resolved rules and Allied/Nod reference geometry remain identical.
Existing bib offsets, interaction bounds/Height, occupancy, exits, docking and all
original source PNG hashes are unchanged. See
`gdi-building-alignment-validation.json`. Release build passed with four existing
warnings. A live in-game placement/selection review remains pending.

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
Tracked case-only renames used two-step `git mv` in the earlier building checkpoint.
The newly supplied factory was likewise normalized without changing image bytes.
The current alignment work is local and has not been committed or pushed.
Protected Nod `conyard.png` remains blob `3b1dae6f171c152a5dc6e81ed4afd02290332078`.

`python tools/audit_png_case.py` checks supplied building assets plus changed/new
PNGs and exact casing of explicit PNG sequence paths, even on Windows. `--fix`
checks name collisions, preserves image hashes and updates filename references.
A lowercase filename alone is not a substitute for matching reference casing.

## Historical initial-integration validation

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
