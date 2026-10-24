# Skirmish setup wizard

Entry: Singleplayer -> Skirmish.

1. Tactical / Operational / Strategic mode cards (currently schematic illustrations).
2. Player count, compatible preset cards, and **Generate and enter lobby** share one page.
   Clicking a card highlights/selects it without navigating to another setup page.
   **Custom Map Parameters** and **Hand Made Map** share a row below the cards.
   **Generate and enter lobby** is on a separate bottom row beside **Back**.
3. Generate/validate/export directly from that page, or use Custom Map Parameters' separate topology/options
   screen or the existing-map chooser. See [the current matrix and resource migration](customize-topology.md).

Strategic offers only compatible archetypes/presets and generates two locked teams.
Migration keeps central Tiberium and now supplies two neutral oil derricks per start island when tech buildings are enabled. Changing player count preserves custom options.
Hand Made Map uses the existing engine chooser's restricted map pool. Search, category
selection and random selection cannot widen that pool. Tactical includes legacy maps
without Operational/Strategic categories. Empty pools show a message instead of choosing
an unrelated map. Installed generated maps are included alongside packaged maps.

Generated packages are written under `SupportDir/maps/ca/generated`, registered as User
maps, and loaded directly into MapCache before starting the local server. Gameplay
validation is shared with the utility command through `MapPlanGenerator`.

`SkirmishLogicCA` retains the standard settings persistence but does not restore the
previous match's map/slots over a different explicitly selected map. No permanent engine
source changes are required; early experimental engine hooks were reverted.
For maps with locked teams, a fresh lobby now selects an opposing-team bot slot
rather than the first empty slot (which may be an ally). Restored client settings
are reconciled with map-locked values.

## Feedback follow-up checks

`%TEMP%/ymca-ui-probes/feedback-final` contains real framebuffer captures for both preset
pages, Customize, the Hand Made Map chooser and a newly generated Strategic lobby.
The opt-in `YMCA_SKIRMISH_UI_PROBE` diagnostic drives actual widget callbacks and
requires an output path beneath `%TEMP%/ymca-ui-probes/`. Without that explicit
variable, normal navigation is unchanged. Back/cancel, mode filtering, direct
preset access and the default opponent were checked: human team 1/spawn 1, bot
team 2/spawn 3. The runner preserved user settings and stopped only its own process.

See [rear rims and Strategic front follow-up](rubberduck-strategic-front-feedback.md).

## Earlier runtime checks

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
screens; that historical instrumentation was removed. The newer explicit opt-in
probe described above is inactive in normal play.

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
