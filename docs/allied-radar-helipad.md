# Supplied Allied Radar Dome and Helipad replacement

Local Game worktree only. Both replacement PNGs supplied on 2026-11-26 are bound
at Scale 1, with the existing footprints, foundations and gameplay unchanged.

- Radar: authoritative `bits/allies/buildings/radar dome.png`, idle and damaged.
  Final placement offset `0,25`, lowered 16 pixels following user placement feedback.
- Helipad: authoritative `bits/allies/buildings/helipad.png`, idle/damaged/active
  and damaged-active. The supplied file's disk capitalization was normalized to
  the existing tracked lowercase filename for case-sensitive platforms, without
  modifying its bytes. Offset `-10,59` registers the new 300×256 canvas and ground
  edge on the existing foundation, lowered 16 pixels following user feedback. No rescaling or aircraft/gameplay edits.
- Updated only `icons/radar.png` and `icons/helipad.png`, directly from the new
  PNGs; Fake Radar shares the updated portrait. All other icon bytes are preserved.

## Mast cutout

Supplied originals and previous artwork are archived in
`art-source/allied-radar-helipad/`. `prepare_allied_radar_masts.py` opens bounded
interior faces of the large lattice mast and roof masts. It removes only the
repeated `(0,1,7)` enclosed background fill within the recorded polygons, not a
global black color key. Outer dark contours, posts, crossbars, dishes, cables and
the building are retained. Only alpha changes: every RGB byte and every pixel
outside the explicit void mask stays identical. The mask and coordinates are
archived, so this operation is reproducible and inspectable.

## Validation

- Isolated build succeeded; native engine screenshot `native-final.png` shows
  both supplied buildings on their foundations and their updated production icons.
- Engine-resolved sequence snapshot confirms the final files, offsets and Scale 1
  for every affected body state, with no later filename shadowing.
- Four focused replacement tests plus five Allied and five faction icon tests pass.
  They check alpha-only cuts, untouched Helipad bytes, unrelated icon hashes,
  gameplay hashes and unrelated faction views. The two authorized icon replacements
  are explicitly recorded rather than silently replacing historical hash baselines.
- The older broad `--check-allied-building-art` checker still stops at its unrelated
  pre-existing `proc: footprint or image mismatch` expectation. This run is not
  counted as passing; the focused resolved-data and native checks above passed.

No user process was stopped, and no release, push or deployment was performed.
