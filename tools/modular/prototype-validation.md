# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1691.
Additional inherited MTNK diagnostics: 6.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but stock MTNK actors.
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.tank` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.hover` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.stationary` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.tank`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.hover`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.stationary`

## Unexpected diagnostics

