# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1693.
Additional inherited actor diagnostics: 8.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> heavy_tank, modular.custom.v98cfee7bcd8b4bd89c778cb093f0d2f7 -> t-34, modular.custom.v9ca741ba7b1e43828c32d5c91087d7b9 -> ttnk.ra2, modular.custom.ve6391587aafb4b16a7f39c42d3eae94d -> ftrk
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v98cfee7bcd8b4bd89c778cb093f0d2f7` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v9ca741ba7b1e43828c32d5c91087d7b9` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.ve6391587aafb4b16a7f39c42d3eae94d` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v98cfee7bcd8b4bd89c778cb093f0d2f7`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.ve6391587aafb4b16a7f39c42d3eae94d`
OpenRA.Utility(1,1): Error: Buildable actor `modular.custom.ve6391587aafb4b16a7f39c42d3eae94d` has prereq `~!promotion.flak_track.barrage` not provided by anything.

## Unexpected diagnostics

