# Development maps are not normal lobby maps

Active development is in **Game**; Game-master is reserved for releases. The earlier duplicate implementation there has been removed, preserving its pre-existing release fixes.

The byte-exact testmap1, testmap2, modular-gdi-lab and Soviet flak test packages now live in `mods/ca/development-maps/`. This directory is registered with classification `Unknown`. The engine still indexes original UIDs for explicit development launches / historical lookup; normal System/User/Remote chooser tabs do not list them. No map YAML, binary data, UID or replay file was changed.

Generators, test launchers, fixture readers and the custom-faction editor use the new path. New test packages must not be written to the normal `mods/ca/maps/` directory. User custom factions and ordinary generated tactical maps were not moved. Historical development docs may still mention the old fixture paths.

`hidden-development-maps.patch` also excludes Unknown maps from `IsSuitableInitialMap`: previously only the remembered map and fallback selection checked classification, but the preferred random choice did not. `--check-active-maps` checks archive classification and initial-map exclusion using the native cache.

Game was built and natively validated. It was initially built in an isolated output directory while the user client remained running. After that process had exited independently, its normal binaries were rebuilt too. The assistant did not stop or restart the user's game. A temporary byte-identical old-path compatibility copy of the lab was removed after the normal build succeeded; all originals remain in the development archive.

Validation: tracked fixture bytes match their original Git files; native map checks, fixture preparation tests, handcrafted package tests and Windows patch application/repeat/conflict checks pass. The three handcrafted maps are now installed in Game too. Relaunch `Game/launch-game.cmd` normally to load the clean menu. No commit, push, release or remote deployment.
