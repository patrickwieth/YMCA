# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1706.
Additional inherited actor diagnostics: 21.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> 1tnk, modular.custom.v644c0e6b9d64434f9c12abcd9642e69e -> apc, modular.custom.v1d2fe413df8e4094a3096cdac1de8cba -> batf.bunker, modular.custom.vd399d8d5c72b48e398e7b1b157456399 -> chpr, modular.custom.vfd03fa90a0514ce28ec47f8c5127b23e -> cryo, modular.custom.v0e9c3af8ad554ae98cc41b4a245280d8 -> ctnk, modular.custom.v9fb0945cb94143198b3e1ac8536eb353 -> ifv, modular.custom.v7dabb0ab4ae2447ca8e46a94715b8661 -> rtnk, modular.custom.v558dbc1ba7364d01ab69efcf741edd6a -> tnkd
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: `modular.custom.v1d2fe413df8e4094a3096cdac1de8cba.CargoInfo.PassengerConditions`: Missing actor `mort`.
OpenRA.Utility(1,1): Error: `modular.custom.v1d2fe413df8e4094a3096cdac1de8cba.CargoInfo.PassengerConditions`: Missing actor `shok.nod`.
OpenRA.Utility(1,1): Error: `modular.custom.v1d2fe413df8e4094a3096cdac1de8cba.CargoInfo.PassengerConditions`: Missing actor `tecn2`.
OpenRA.Utility(1,1): Error: `modular.custom.v1d2fe413df8e4094a3096cdac1de8cba.CargoInfo.PassengerConditions`: Missing actor `tecn3`.
OpenRA.Utility(1,1): Error: `modular.custom.v9fb0945cb94143198b3e1ac8536eb353.CargoInfo.PassengerConditions`: Missing actor `mort`.
OpenRA.Utility(1,1): Error: `modular.custom.v9fb0945cb94143198b3e1ac8536eb353.CargoInfo.PassengerConditions`: Missing actor `shok.nod`.
OpenRA.Utility(1,1): Error: `modular.custom.v9fb0945cb94143198b3e1ac8536eb353.CargoInfo.PassengerConditions`: Missing actor `snip`.
OpenRA.Utility(1,1): Error: `modular.custom.v9fb0945cb94143198b3e1ac8536eb353.CargoInfo.PassengerConditions`: Missing actor `tecn2`.
OpenRA.Utility(1,1): Error: `modular.custom.v9fb0945cb94143198b3e1ac8536eb353.CargoInfo.PassengerConditions`: Missing actor `tecn3`.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, onwater.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v644c0e6b9d64434f9c12abcd9642e69e` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v1d2fe413df8e4094a3096cdac1de8cba` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, assault-move.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vd399d8d5c72b48e398e7b1b157456399` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vfd03fa90a0514ce28ec47f8c5127b23e` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v0e9c3af8ad554ae98cc41b4a245280d8` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v9fb0945cb94143198b3e1ac8536eb353` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, disabled.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v7dabb0ab4ae2447ca8e46a94715b8661` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v558dbc1ba7364d01ab69efcf741edd6a` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v644c0e6b9d64434f9c12abcd9642e69e`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v0e9c3af8ad554ae98cc41b4a245280d8`

## Unexpected diagnostics

