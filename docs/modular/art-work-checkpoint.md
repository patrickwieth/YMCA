# Modular art work checkpoint

Local work checkpoint, not a release or an assertion that every study is approved.

## Installed / retained work

- GDI building placement, color-matched derivatives and separate foundations.
- Centered native Ion Defense, independent stationary base / 32-facing turret and
  2x2 decorative bib; historical modeling and contour/color studies retained.
- Guard Tower PNG and 1x1 bib, corrected Helipad visual placement, Upgrade Center
  modules, Naval Yard and supporting calibration/engine validation tools.
- Soviet building alignment and source artwork, independent sprite Sentry study;
  rejected voxel comparison retained as an experiment, not an artistic approval.

## Unfinished / review-only

- AA Gun and Sonic Turret pass-05 were explicitly rejected by the user. Pass-06
  rework exists; further pass-07 code is written but not rendered/reviewed. Do not
  install these as approved assets. See their art-source READMEs.
- Nod laser turret pass-04 is the current independently rendered review candidate:
  separate geometry, 32 native directions, source comparisons and tests. Not bound
  to a gameplay actor, weapon or production slot; unseen surfaces are inferred.
- Replacement GDI power plant and new advanced power plant source PNGs are included
  as supplied. Their new bindings/alignment/color refresh are still pending. The
  existing power-plant color derivative still corresponds to the previous source;
  full color/source consistency tests are not expected to be green until refreshed.
  See `art-source/gdi-power-plant-refresh/README.md`.

## Validation and boundaries

The Nod study's four tests passed using a separate utility copy with the freshly
compiled CA assembly. Normal solution output copying was blocked by the running
Game's DLL lock; the game was not stopped. This is not a full-build or live-game
validation claim. Detailed study geometry/render controls are stored with the art.

The protected `mods/ca/bits/nod/buildings/conyard.png` local modification is expressly
excluded from this checkpoint. Its working copy is preserved. The original
`mods/ca/maps/testmap1.oramap` is unchanged. Python caches, automatic `.blend1`
backups and disposable test-map artifacts are not included; existing files are
left on disk. Main `.blend` models and historical review passes are included.

No Game-master/news/release work is included. No tag, release publication or server
deployment is authorized by this checkpoint push. The modular branch retains an
older release-edited deployment trigger, so do not edit releases as part of it.
