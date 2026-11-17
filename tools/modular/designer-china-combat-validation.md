# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1705.
Additional inherited actor diagnostics: 20.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> chbattle, modular.custom.vb8d0b8de9f54423094a2df2fdd54d142 -> chdragon, modular.custom.vcbb64a5cc7a64592a3cc08cec440765a -> chgtnk, modular.custom.v09c46a3eff3b49c8b578a4c2e0621f82 -> choverlord, modular.custom.vc48b6b31c0324becb81c0c093f82b228 -> charty, modular.custom.v1badccbb86b148f68a76a553267d0af0 -> chcrawl2, modular.custom.v802bde556a184ce68ee21aa18d6ab896 -> chnukecann, modular.custom.v7114afa18275451198f08fb296f7b01e -> bixi
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: `modular.custom.v1badccbb86b148f68a76a553267d0af0.CargoInfo.PassengerConditions`: Missing actor `mort`.
OpenRA.Utility(1,1): Error: `modular.custom.v1badccbb86b148f68a76a553267d0af0.CargoInfo.PassengerConditions`: Missing actor `shok.nod`.
OpenRA.Utility(1,1): Error: `modular.custom.v1badccbb86b148f68a76a553267d0af0.CargoInfo.PassengerConditions`: Missing actor `tecn2`.
OpenRA.Utility(1,1): Error: `modular.custom.v1badccbb86b148f68a76a553267d0af0.CargoInfo.PassengerConditions`: Missing actor `tecn3`.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, propaganda.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vb8d0b8de9f54423094a2df2fdd54d142` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, disabled.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vcbb64a5cc7a64592a3cc08cec440765a` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v09c46a3eff3b49c8b578a4c2e0621f82` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vc48b6b31c0324becb81c0c093f82b228` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v1badccbb86b148f68a76a553267d0af0` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v802bde556a184ce68ee21aa18d6ab896` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, chronobeamed.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v7114afa18275451198f08fb296f7b01e` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vb8d0b8de9f54423094a2df2fdd54d142`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vcbb64a5cc7a64592a3cc08cec440765a`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v09c46a3eff3b49c8b578a4c2e0621f82`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vc48b6b31c0324becb81c0c093f82b228`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v1badccbb86b148f68a76a553267d0af0`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v802bde556a184ce68ee21aa18d6ab896`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v7114afa18275451198f08fb296f7b01e`

## Unexpected diagnostics

