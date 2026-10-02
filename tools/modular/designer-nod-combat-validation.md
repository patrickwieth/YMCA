# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1706.
Additional inherited actor diagnostics: 21.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> ltnk, modular.custom.vd9532d23c91b4ff38570b0b9120ff3fb -> bggy, modular.custom.vf495561c47b74e41af49deadba3e58a5 -> arty.nod, modular.custom.v2453ec4c2fae4119845034e0b6dabab3 -> ssm, modular.custom.v43d3833a7d8f410396e54f63bfc90efe -> apc2, modular.custom.veaa0f4cf15c442d49efb35c3a9ed54c6 -> bike, modular.custom.va7579fd9d2cb4f7c8e3fbb5030acf8a1 -> beam_cannon, modular.custom.vb9ac769c9add4a37b3b96203524a8b42 -> ftnk, modular.custom.vfd6834965eb443be8c69e41173b4d488 -> hftk, modular.custom.v569d037ee3e14780893e0c265d36dba1 -> howi, modular.custom.v3034610404304d95b911bb2aee361dc6 -> spec, modular.custom.v1056cc91a700405db1638c40e6e1e21b -> stnk, modular.custom.vcdb744751bfa445d98e9f796d7b68b22 -> ttrk, modular.custom.v61630300febf4184a26c1bc2dd81a71e -> wtnk
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, onwater.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vd9532d23c91b4ff38570b0b9120ff3fb` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vf495561c47b74e41af49deadba3e58a5` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v2453ec4c2fae4119845034e0b6dabab3` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v43d3833a7d8f410396e54f63bfc90efe` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.veaa0f4cf15c442d49efb35c3a9ed54c6` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.va7579fd9d2cb4f7c8e3fbb5030acf8a1` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vb9ac769c9add4a37b3b96203524a8b42` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vfd6834965eb443be8c69e41173b4d488` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v569d037ee3e14780893e0c265d36dba1` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v3034610404304d95b911bb2aee361dc6` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, airborne.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v1056cc91a700405db1638c40e6e1e21b` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vcdb744751bfa445d98e9f796d7b68b22` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, being-captured, driver-dead, notmobile, berserk.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v61630300febf4184a26c1bc2dd81a71e` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vd9532d23c91b4ff38570b0b9120ff3fb`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v43d3833a7d8f410396e54f63bfc90efe`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.veaa0f4cf15c442d49efb35c3a9ed54c6`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vb9ac769c9add4a37b3b96203524a8b42`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v1056cc91a700405db1638c40e6e1e21b`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vcdb744751bfa445d98e9f796d7b68b22`

## Unexpected diagnostics

