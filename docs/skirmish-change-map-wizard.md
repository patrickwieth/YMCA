# Skirmish Change Map reopens generation

`GeneratedMapLobbyLogic` runs after the normal lobby logic and replaces only the local Skirmish host's Change Map callback. It reopens the existing mode/preset/customization wizard. Hand Made Map still provides the existing-map chooser.

Selecting a generated or existing map issues the normal `map UID` order to the current server; it does not create another server or discard the lobby. Cancel leaves the current map alone. Original button visibility/readiness/game-start guards remain active and are rechecked on selection. Ordinary multiplayer and non-host map browsing retain the original chooser. Generated packages retain the established session ownership/normal-exit cleanup behavior.

Real UI regression: `YMCA_SKIRMISH_CHANGE_MAP_PROBE=1`, `%TEMP%/ymca-ui-probes/change-map/results.txt`. It clicks Change Map, cancels, reopens the wizard, opens/cancels Hand Made Map, generates Plains, and verifies the changed map in the same OrderManager/lobby. Result: `CHANGE MAP COMPLETE`.

The test launcher sets both engine/mod search environment variables and waits for server loading; initial short/incorrectly configured runs were not passes. The existing bot-default probe remains separate from this map-switch test, which permits a saved local spectator preference.
