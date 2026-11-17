# Building-test air targets — local changes

Both disposable packages were regenerated: `testmap1-debug.oramap` and
`soviet-flak-voxel-test.oramap` (the latter retains its legacy filename).

- Airstrip display removed.
- Only `sentry.soviet` is placed; voxel Sentry showcase removed. Voxel study assets
  are retained, not deleted or used as the production Sentry.
- One map-local `AA.PRACTICE` beside each NSAM, Soviet sprite flak, AGUN and CRAM.
  These inherit unarmed TRAN (Chinook), now belong to Multi0 (the human player), spawn airborne
  at2c512, idle without landing and have10000000HP for repeated firing tests.
  Locations:79,17 /151,-55 /114,16 /148,-10. No production-menu entry.
- Shared helper `tools/prepare_air_defense_targets.py` is used by both generators.
  Normal PowerShell preparation now also needs Python (available in this workspace).
- Soviet cluster shell now creates the central air effect immediately, followed
  by four child explosions after4 ticks. Central effect adds no extra damage;
  the existing4x4000 damage budget and32-tick cadence remain unchanged.

Map-generation regression and three cluster tests pass; generated packages were
inspected. Full engine map lint runs but is not clean: existing global mod errors
include missing prerequisites/conditions and player-palette lint references; no
clean full-lint or live gameplay validation is claimed. TRAN's inherited generic
condition/palette warnings also appear on the map-only practice variant.

Original testmap1.oramap SHA256 remains
`ec6d572076d907c54041cde75dcc42428d6bbd2f87ccf91361d909662c8f3bda`.
No running game stopped, no commit/push/deploy. Restart `test-gebaeude.cmd` to load
updated map/rules. Live hovering and explosion timing review remains pending.
