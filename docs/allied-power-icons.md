# 96×72 production icons and Allied power-plant trial

## Current local implementation

Production buttons use **96×72 logical pixels**, three columns, with 1px gaps.
The sidebar is 340 logical pixels wide. Existing user UI-scale settings are not
changed: e.g. 1.25× displays a cell at 120×90 physical pixels, 1.5× at 144×108.
This is the production sidebar modernization pilot, not a complete HUD redesign;
support-power icons and the existing chrome artwork remain legacy assets.

The two Allied power icons use **192×144 source PNGs** so the renderer can retain
image detail at higher UI scales instead of enlarging a 96×72 raster. They are
close-ups cropped directly from the authoritative building PNGs. At the user's request, captions use the original OsShp pixel lettering again,
scaled to the 96px button width (1.5× nominal glyph size), not the earlier trial's
smooth Bold font. The same pixel-font choice is retained in observer production icons. Other factions' icon bindings and
all building gameplay remain unchanged; legacy 64×48 cameos temporarily scale up.

- Runtime: `mods/ca/bits/allies/icons/{power-plant,fusion-power-plant}.png`.
- Editing masters: `art-source/allied-power-icons/*-256x192.png`.
- Generator: `tools/prepare_allied_power_icons.py` (overwrites generated icons and
  masters, never the original building artwork).
- Crop coordinates and source hashes: `art-source/allied-power-icons/provenance.json`.
- Earlier 96px source trial: `art-source/allied-power-icons/history/96-source-trial/`.
- Original 64px trial screenshot remains `validation/native-sidebar.png`.

## Rendering and layout

`FitIconsToCell` is opt-in on the production palette. Body icons and progress/darken
masks fit the cell; badges use the corresponding 1.5× nominal overlay geometry.
Observer icons and the CA selection portrait fit their existing display bounds,
so higher-resolution sources do not enlarge or overlap those widgets.

`SidebarImageWidget` stretches only the middle strip of chrome, retaining the
category rail and outside borders. The radar gets a wider usable viewport and a
black backing; its offline image preserves its aspect ratio. Visible row limits
include gaps and the bottom cap. Existing click, queue and cancellation behavior
is retained; ordinary gameplay and map-generation code are untouched.

Engine changes are registered in `tools/patches/production-icons-sized.patch`.
The ordinary building-test launcher rebuilds and reapplies registered patches.

## Verification

- `tools/test_allied_power_icons.py`: source hashes, source dimensions, bindings,
  retirement of the separate oil-platform water overlay.
- `tools/test_production_icon_layout.py`: sidebar/cell/row geometry and size contracts.
- Native diagnostic `ProductionPaletteReviewProbe`: real per-cell corner hitboxes,
  mouse-wheel scroll down/up, actual production click/progress, configured right-click
  cancel and left-click restart. No global production behavior is overridden.
- Fixtures are made with `tools/prepare_production_icon_probe.py` from the disposable
  prepared building grid; `--scroll` adds diagnostic-only buildables.
  Enable the probe using `YMCA_ICON_REVIEW=1`, then use the native runner's
  `-UIScale`, `-WindowWidth`, `-WindowHeight` options.
- The fixture disables only its POWR early-game 1% time boost and fast-build cheat
  to make progress visible. None of these fixture rules are installed in normal maps.
- Final HD-source native results/screenshots are archived under
  `art-source/allied-power-icons/validation/`. The report records the **actual** UI
  scale: the engine may fall back to 1× if a requested scale exceeds window limits.

The user's manually added waterline in `oil-platform.png` was not modified.
No release, push or deployment is included.
