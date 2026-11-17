# Modular prototype engine validation

DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.

Control errors: 1685. Prototype errors: 1712.
Additional inherited actor diagnostics: 27.
Unexpected new diagnostics: 0.

The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.
Bindings: modular.custom.tank1 -> 2s3, modular.custom.vbb0ab87f36724708abcde35a7b9dcdde -> btr, modular.custom.v6dbe85de850945c8acac5a9f8acdd2fe -> chem_sprayer, modular.custom.v38e48d7631c74850b6673de012b4c88a -> dtrk, modular.custom.v2989b6698c434ed6acfa59b63f495786 -> devil_tank, modular.custom.vee8ec10db7884d0983e9ded75b065cb0 -> gene_splicer, modular.custom.v1fdb2bbcbbc240ce916307e911697a6f -> hq7, modular.custom.v7139ab00ebce4694a2438a074f4000e9 -> htk5, modular.custom.v76d0b3b399254c7ab9cf27b69f7f4401 -> hyena, modular.custom.v5a33814e0b13488fabea174b744eb8ba -> isu, modular.custom.veafa2717ad4946fd83689bce10f45754 -> katy, modular.custom.vf9d9066f6d85464aa572358205cae7cc -> nonasvk, modular.custom.va464bffca6f7425abde4574993306626 -> peoples_tank, modular.custom.v27ab71038e60410f8cf1997023c86ddf -> qtnk, modular.custom.v58fb0249aae74c27b1bdc4587bede1f9 -> rice_cooker, modular.custom.vf167dca66b9348878e57469bccc1d1b3 -> source_of_pollution
An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.

## Additional inherited diagnostics

OpenRA.Utility(1,1): Error: Actor type `modular.custom.tank1` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vbb0ab87f36724708abcde35a7b9dcdde` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, full.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v6dbe85de850945c8acac5a9f8acdd2fe` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v38e48d7631c74850b6673de012b4c88a` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, being-captured, driver-dead, notmobile, berserk.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v2989b6698c434ed6acfa59b63f495786` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vee8ec10db7884d0983e9ded75b065cb0` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v1fdb2bbcbbc240ce916307e911697a6f` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v7139ab00ebce4694a2438a074f4000e9` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v76d0b3b399254c7ab9cf27b69f7f4401` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v5a33814e0b13488fabea174b744eb8ba` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.veafa2717ad4946fd83689bce10f45754` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vf9d9066f6d85464aa572358205cae7cc` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, cargo, full.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.va464bffca6f7425abde4574993306626` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v27ab71038e60410f8cf1997023c86ddf` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.v58fb0249aae74c27b1bdc4587bede1f9` consumes conditions that are not granted: ra2sprite, tdsprite, scrinsprite, voxel, gdiupg1, sovietparachute, notmobile, ricecargo_full.
OpenRA.Utility(1,1): Error: Actor type `modular.custom.vf167dca66b9348878e57469bccc1d1b3` consumes conditions that are not granted: rasprite, ra2sprite, tdsprite, scrinsprite, gdiupg1, sovietparachute, notmobile.
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v6dbe85de850945c8acac5a9f8acdd2fe`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v38e48d7631c74850b6673de012b4c88a`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v2989b6698c434ed6acfa59b63f495786`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v1fdb2bbcbbc240ce916307e911697a6f`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v7139ab00ebce4694a2438a074f4000e9`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.v76d0b3b399254c7ab9cf27b69f7f4401`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.va464bffca6f7425abde4574993306626`
OpenRA.Utility(1,1): Error: Undefined palette reference `playerts` detected at `OpenRA.Mods.Cnc.Traits.Render.RenderVoxelsInfo` for `modular.custom.vf167dca66b9348878e57469bccc1d1b3`
OpenRA.Utility(1,1): Error: Buildable actor `modular.custom.vbb0ab87f36724708abcde35a7b9dcdde` has prereq `~promotion.btr` not provided by anything.
OpenRA.Utility(1,1): Error: Buildable actor `modular.custom.vee8ec10db7884d0983e9ded75b065cb0` has prereq `cloning_vats` not provided by anything.
OpenRA.Utility(1,1): Error: Buildable actor `modular.custom.vf9d9066f6d85464aa572358205cae7cc` has prereq `~promotion.nona_svk` not provided by anything.

## Unexpected diagnostics

