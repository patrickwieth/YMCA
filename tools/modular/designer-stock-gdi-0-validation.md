# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1689.
Additional inherited actor diagnostics: 4.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> gdrn, modular.custom.v408c6f5a791a44bd836638c3635f8f57 -> mdrn, modular.custom.vf7e796fb973f4051bda1fee17925028e -> vulc, modular.custom.v81bddd2351784b90b9ee6a995aa275d4 -> xo
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, disabled.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v408c6f5a791a44bd836638c3635f8f57` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, driver-dead, disabled.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vf7e796fb973f4051bda1fee17925028e` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v81bddd2351784b90b9ee6a995aa275d4` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.

## Unexpected diagnostics

