# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1693.
Additional inherited actor diagnostics: 8.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> chbattle, modular.custom.v03b63f59cc014510a0ddfdcfe2409aa6 -> chdragon, modular.custom.v6142c9631984422f90e907a029d1a59e -> chgtnk, modular.custom.v6a692e3f33df4a048548ccf3af58356a -> choverlord
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, propaganda.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v03b63f59cc014510a0ddfdcfe2409aa6` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, disabled.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6142c9631984422f90e907a029d1a59e` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6a692e3f33df4a048548ccf3af58356a` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v03b63f59cc014510a0ddfdcfe2409aa6`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v6142c9631984422f90e907a029d1a59e`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v6a692e3f33df4a048548ccf3af58356a`

## Unexpected diagnostics

