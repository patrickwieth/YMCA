# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1698.
Additional inherited actor diagnostics: 13.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> mtnk, modular.custom.ve90f8fc0c31b4d299546201cf4cc65b0 -> hmmv, modular.custom.v23e754dff84340a4aa992b9f24525163 -> mlrs, modular.custom.v133290b197764ffc9c7ecceb0a4474b0 -> juggernaut, modular.custom.v4ddf31ef6fab402db5c15912058d8367 -> mammoth, modular.custom.v352100a988254d2daf236b136c308d3a -> hmlrs, modular.custom.vc32591da24524646b1f98b1bdb8febea -> disr, modular.custom.v396a1f5e122a4d57acdcda74667d3a50 -> mammothmk2
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.ve90f8fc0c31b4d299546201cf4cc65b0` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v23e754dff84340a4aa992b9f24525163` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v133290b197764ffc9c7ecceb0a4474b0` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v4ddf31ef6fab402db5c15912058d8367` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v352100a988254d2daf236b136c308d3a` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, gdiupg2.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vc32591da24524646b1f98b1bdb8febea` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, gdiupg2.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v396a1f5e122a4d57acdcda74667d3a50` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.ve90f8fc0c31b4d299546201cf4cc65b0`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v23e754dff84340a4aa992b9f24525163`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v4ddf31ef6fab402db5c15912058d8367`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerra2` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v396a1f5e122a4d57acdcda74667d3a50`

## Unexpected diagnostics

