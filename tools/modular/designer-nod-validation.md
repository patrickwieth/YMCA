# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1691.
Additional inherited actor diagnostics: 6.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> ltnk, modular.custom.vc0b7178e0c3e4a93a28f3a36bcd2812e -> bggy, modular.custom.v5f4270bb55864cf09fd3102aa4cda3ee -> arty.nod, modular.custom.v6e03f023256f4f68a0af299927d38541 -> ssm
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile, onwater.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vc0b7178e0c3e4a93a28f3a36bcd2812e` consumes conditions that are not granted: rasprite, ra2sprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v5f4270bb55864cf09fd3102aa4cda3ee` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6e03f023256f4f68a0af299927d38541` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.tank1`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vc0b7178e0c3e4a93a28f3a36bcd2812e`

## Unexpected diagnostics

