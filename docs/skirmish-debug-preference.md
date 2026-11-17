# Remember the local Skirmish debug option

The normal `DeveloperMode` default remains disabled. `SkirmishLogicCA` now remembers the last local Skirmish `cheats` option independently of map-specific slots/options, including freshly generated wizard maps.

Storage: `<SupportDir>/skirmish-debug.ca.txt`, a boolean, written via temporary file and replacement. Existing installations migrate the old `Options/cheats` value from `skirmish.ca.yaml` when no preference exists. Missing/invalid preferences never force debug on. I/O failures are logged rather than aborting the lobby.

Only `ServerType.Skirmish` uses this preference. Dedicated/network lobbies, campaigns and `Launch.Map` diagnostics do not read or update it. Locked maps keep their effective option and do not overwrite the saved choice. The preferred value remains available when returning to an unlocked map. Changes made through ordinary lobby commands, including toggling off, persist automatically. Other options still use existing persistence.

The developer's own support file was explicitly set to `True` as requested; it is outside the repository and is not shipped. Fresh installations retain the default Off.

Validation:

- `--skirmish-debug-preference-test`: nine scenarios pass (fresh default, restart/new generated map, both saved values, locked-on/off maps, absent option, legacy migration, malformed data).
- Actual isolated-support UI/lobby probe: restored On in a fresh wizard-generated lobby; accepted and persisted Off, then On; retained On across an actual switch to a different map. Probe activation requires both existing `YMCA_SKIRMISH_UI_PROBE` and explicit `YMCA_SKIRMISH_DEBUG_PROBE=1`. Its uniquely titled temporary basin fixture was removed from the playable map list afterward.
- Normal Release build installed successfully. No engine-source change was required.

Evidence: `%TEMP%/ymca-debug-preference` and `%TEMP%/ymca-ui-probes/debug-preference/results.txt`. A first diagnostic map lookup was ambiguous because two temporary maps shared a title; a uniquely titled fixture corrected the test setup. No user-owned process was terminated. Changes remain uncommitted/unpushed.
