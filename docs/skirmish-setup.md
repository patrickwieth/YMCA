# Skirmish setup wizard

Entry: Singleplayer -> Skirmish.

1. Tactical / Operational / Strategic mode cards (currently schematic illustrations).
2. Normal Map (generator) or Custom Map (installed maps).
3. Normal Map: compatible presets or Customize.
4. Player count and automatic dimensions; Customize adds an archetype/preset dropdown,
   resource layout, finite resources, tech buildings and applicable mountain-line options.
5. Generate/validate/export, or confirm the existing map, then enter the standard lobby.

Strategic offers only compatible archetypes/presets and generates two locked teams.
Migration retains its fixed economy. Changing player count preserves custom options.
Custom Map uses the existing engine chooser's restricted map pool. Search, category
selection and random selection cannot widen that pool. Tactical includes legacy maps
without Operational/Strategic categories. Empty pools show a message instead of choosing
an unrelated map. Installed generated maps are included alongside packaged maps.

Generated packages are written under `SupportDir/maps/ca/generated`, registered as User
maps, and loaded directly into MapCache before starting the local server. Gameplay
validation is shared with the utility command through `MapPlanGenerator`.

`SkirmishLogicCA` retains the standard settings persistence but does not restore the
previous match's map/slots over a different explicitly selected map. No permanent engine
source changes are required; early experimental engine hooks were reverted.

## Runtime checks

Real framebuffer captures are in `%TEMP%/ymca-skirmish-setup`:

- `ca-2026-09-11T071009219Z.png`: mode cards and corrected dialog background.
- `ca-2026-09-11T071017221Z.png`: Strategic preset stage.
- `ca-2026-09-11T071021229Z.png`: Customize stage.
- `final-generated-lobby.png`: generated Strategic TwoSides in the ordinary lobby,
  with locked teams/spawns; the previous Operational map was not restored.
- `final-custom-0.png`: existing map chooser restricted to the installed Operational map.
- `final-custom-1.png`: that selected Operational map in the ordinary lobby.

The final custom-map tab caption was subsequently changed from the engine's default
"Server Maps" to "Operational Maps" (or the selected mode).
Temporary game-thread navigation/screenshot instrumentation was used to traverse the
screens; it has been removed. No automatic menu navigation remains in the normal build.

CA Release build and TwoSides seed-42 validation for all three modes passed. The
`dotnet test --no-restore` invocation returned build success without reporting executed
tests; this is not recorded as a unit-suite pass.

## Remaining polish / scope

- Replace schematic mode graphics with final illustrations and localize new labels.
- The separate Multiplayer -> Create Server flow and the lobby's existing Change Map
  button are unchanged. The restricted pool currently applies to the setup wizard.
- Expand Customize with further supported suboptions and test all combinations/resolutions.
- Generation currently runs on the game thread because exporter/engine resources are not
  thread-safe; the UI shows a generating state beforehand, but has no cancellable worker.
- Terrain production gates are unchanged: the shore/cliff calibration prototypes are not
  silently enabled by the new menu.
