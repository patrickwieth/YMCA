# v0.97.3 release preparation

User explicitly authorized a release including the integrated isometric branch,
not merely the productive lobby fixes. The release line merges isometric through
c833f596 and preserves tournament-bot packaging, stable-manifest ordering, News
configuration/bundled announcement, map archives and match-safe deployment.

Included: existing isometric runtime/art, foundation connections and all faction
color variants, China Soviet-style grids, GDI fixed grids, research-core native
placement, Soviet AI MCV handling, Skirmish callback fix and dedicated map waiting
state. Unfinished Blender studies, caches, temporary diagnostics and the protected
working-copy Nod conyard PNG are excluded. No user game process was stopped.

Release-source validation:
- CA/Cameo build: successful against isolated pinned patched engine.
- Modular suite: 341 passed. Tournament suite: 21 passed.
- Two explicit tournament host tests passed (real generated maps and HTTP hashes).
- Native callback argument, waiting-lobby, bib assets/palettes/registrations,
  Battlefield News and active-map/shellmap safety checks passed.
- Real admin/guest/late-join dedicated probe passed on release sources, including
  rejected ready/start commands before selection. UID:
  9eaa1e5ce2c3bc90a707d91506bcf6b6afb6cc06.
- GDI/Nod/China native visual/lifecycle review evidence is merged under art-source.
- GDI grid, color-mask and Soviet AI Python regressions passed.

A broad historical supplied-art test was also run; it is not fully green. It
still expects 13 rather than 14 calibrated GDI images and the replaced Research
Core's old 1024px/0.25-scale presentation. Its raw filename audit additionally
reports four legacy `sentry turret.png` references in old sequence definitions;
active native Sentry artwork uses later replacement sequences. These failures
are recorded rather than described as a successful full art audit. No legacy
art was fabricated to satisfy that test.

Public notes are concise English player-facing Features / Bug Fixes. Bot Docker
host version is advanced to v0.97.3; deployment must retain both active-match
checks and verify Discord registration plus the actual game version.

## Verified publication and deployment

- Release/tag v0.97.3 points to merge commit 9883b9f0 and includes isometric through
  c833f596. Release is public, non-draft and non-prerelease.
- Packaging run 44259557387 succeeded: Windows portable/installer, Linux AppImage,
  macOS DMG, stable manifest publication and game-server deployment.
- The public master list independently advertised ca@v0.97.3 on
  45.136.28.158:1234 and :1235; both were empty waiting lobbies with eight slots.
- Stable manifest is v0.97.3 (publication commit 4d05989d).
- Separate tournament deployment run 44262335547 succeeded. Its logs verified
  current-container Discord connection/command registration and actual tournament
  host version v0.97.3 at 2026-11-18T07:57Z.
- Both active-match checks stayed enabled; no force override was used.
