# GDI foundation joints — November 19, 2026

User requested replacing player-colored corner panels with player-colored joints,
matching Soviet/China treatment. Implemented in Game / isometric, merged to master and published in v0.97.5.
See `../release-0.97.5-validation.md`.

- All five GDI foundation sizes (1x1, 2x2, 3x3, 4x4, 4x5) now bind new indexed
  `bib*-joint-remap.png` overlays. EMP inherits the same mask with its existing offset.
- Only authored joints, their highlights/shadows and the black seam edges are masked.
  Every concrete plate pixel, including the former colored corners, stays neutral.
- Premultiplied 65% joint tones and lifted opaque black edges follow the existing
  Soviet/China approach. Palette indices 1–9 remap through `gdibibplayer`.
- Same-owner gap fillers now carry matching joint colors too. Connection geometry,
  owner isolation, gameplay footprints and building artwork are unchanged.
- The existing `^GdiBibCornerColor` template name remains for inheritance compatibility,
  but its resolved behavior is joint remapping, not corner surfaces.
- Original supplied PNGs, existing GDI grid derivatives and old corner masks are
  preserved byte-for-byte. Nod masks and all other faction remaps are unchanged.

Validation: mask tests and fixed-grid tests pass; native `--check-bib-connections`
resolves registrations and actor/palette bindings. Local GUI review covers several
owner colors and all five sizes. The live connection probe passes creation, removal,
capture, same-owner recoloring and destruction. Normal Game Release build succeeds.
Evidence and preserved-file hashes: `art-source/gdi-joint-remap/`.

## Subsequent rock-rendering work

The separate Tiberium projection defect and translucent unit silhouettes were
subsequently implemented and checked in native fixtures. See `rock-occlusion.md`
for the reproduction, visibility safeguards, evidence and local build status.
