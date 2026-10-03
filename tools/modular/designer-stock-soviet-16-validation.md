# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1694.
Additional inherited actor diagnostics: 9.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> ttnk, modular.custom.v855fd62334244bc685497c3d0a4f46ba -> tsar_tank, modular.custom.vcb58696fe96a41bf9c6f086eac6c6414 -> v3rl, modular.custom.v332a0f58a2a643f286bfc3163a49b0c9 -> apoc, modular.custom.v17fd2f101c2049bdadfcb31a8e38a4d3 -> kims_wheel
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v855fd62334244bc685497c3d0a4f46ba` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vcb58696fe96a41bf9c6f086eac6c6414` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v332a0f58a2a643f286bfc3163a49b0c9` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v17fd2f101c2049bdadfcb31a8e38a4d3` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v855fd62334244bc685497c3d0a4f46ba`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v332a0f58a2a643f286bfc3163a49b0c9`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v17fd2f101c2049bdadfcb31a8e38a4d3`

## Unexpected diagnostics

