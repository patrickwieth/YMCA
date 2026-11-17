# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1695.
Additional inherited actor diagnostics: 10.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> atmz, modular.custom.vf83fe1f2ed7b4f4b888550c8b52cbd91 -> channeler, modular.custom.v4b9142e6c1194a348efbd26b8dbdc5f3 -> hexapod, modular.custom.v3e299c974983443a8efeb750c0d8879d -> intl, modular.custom.v6765b10bc16f4975b765995d2535a07a -> lace, modular.custom.v588b8235d77a414aa09993a76d98a430 -> lchr, modular.custom.va75c0198d25a42b9b000bbeb80265573 -> rptp, modular.custom.v99d5be9813fe4e22b3d1ae0a0a5ab950 -> ruin, modular.custom.v94b1668fdc1843af8d6cb7c9247d4a46 -> stcr, modular.custom.vcdcf0acedc2d4e79b0735a3382964380 -> tpod
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vf83fe1f2ed7b4f4b888550c8b52cbd91` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v4b9142e6c1194a348efbd26b8dbdc5f3` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v3e299c974983443a8efeb750c0d8879d` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6765b10bc16f4975b765995d2535a07a` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v588b8235d77a414aa09993a76d98a430` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.va75c0198d25a42b9b000bbeb80265573` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v99d5be9813fe4e22b3d1ae0a0a5ab950` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v94b1668fdc1843af8d6cb7c9247d4a46` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vcdcf0acedc2d4e79b0735a3382964380` consumes conditions that are not granted: rasprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.

## Unexpected diagnostics

