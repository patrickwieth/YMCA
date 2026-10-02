# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1701.
Additional inherited actor diagnostics: 16.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> mtnk, modular.custom.v28f96bc55bbc4655b25ebc48b0bf5745 -> hmmv, modular.custom.v6a7db0a28ba14104a6d2c3e9c341620e -> mlrs, modular.custom.v744086b4a97d4effbe5c15fbca79ce07 -> juggernaut, modular.custom.va6039c29a33642ed9aeb0a5692ecb61b -> mammoth, modular.custom.ve6901e07b4d54d8484461427a528ca5b -> hmlrs, modular.custom.v67ba575e53274f958f464b62605b0e8b -> disr, modular.custom.v9b33968f7e9548a9bf694c98cf72167f -> mammothmk2, modular.custom.v6b3cc3cac999450793d294bea440cbfb -> titn, modular.custom.v07c3c9ddd2dd4914b41f3730ca1ddf1d -> slng, modular.custom.ve657a7628b93419696cf71da896c665f -> marv
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v28f96bc55bbc4655b25ebc48b0bf5745` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6a7db0a28ba14104a6d2c3e9c341620e` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v744086b4a97d4effbe5c15fbca79ce07` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.va6039c29a33642ed9aeb0a5692ecb61b` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.ve6901e07b4d54d8484461427a528ca5b` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, gdiupg2.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v67ba575e53274f958f464b62605b0e8b` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, gdiupg2.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v9b33968f7e9548a9bf694c98cf72167f` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6b3cc3cac999450793d294bea440cbfb` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v07c3c9ddd2dd4914b41f3730ca1ddf1d` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.ve657a7628b93419696cf71da896c665f` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v28f96bc55bbc4655b25ebc48b0bf5745`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v6a7db0a28ba14104a6d2c3e9c341620e`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.va6039c29a33642ed9aeb0a5692ecb61b`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerra2` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v9b33968f7e9548a9bf694c98cf72167f`

## Unexpected diagnostics

