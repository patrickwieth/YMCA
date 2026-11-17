# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1689.
Additional inherited actor diagnostics: 4.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> gunw, modular.custom.v23db4583539f4c22a3bb240d9d73a9dc -> seek, modular.custom.vd6853d7bd0444ed8ac2358816baca09d -> corr, modular.custom.v1355c0b1954b4373942d2df6a7c5bae6 -> devo
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v23db4583539f4c22a3bb240d9d73a9dc` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vd6853d7bd0444ed8ac2358816baca09d` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v1355c0b1954b4373942d2df6a7c5bae6` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.

## Unexpected diagnostics

