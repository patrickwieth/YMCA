# LAN / peer-hosted map generation follow-up

Local fix after v0.97.3, synchronized as source changes in Game, Game-master and
Game-release. Not committed, published or deployed by this follow-up.

## Confirmed gap

v0.97.3 only advertised/handled generator recipes for ServerType.Dedicated.
The multiplayer creation wizard generated a map in the host's cache, but a LAN
server did not announce that recipe to other clients. Change Map also bypassed
the wizard for peer-hosted multiplayer. Guests without the map therefore could
not reproduce it as they could on a dedicated host.

This confirms the missing guest synchronization path. It does not establish a
cause for a separately reported host crash before reaching the lobby; a concrete
error/version would still be needed for that symptom.

## Fix

- Remember bounded immutable recipes for generated map UIDs per ModData, in a
  weak-keyed, thread-safe cache. This includes generation before server creation.
- Advertise and handle the same protocol on Multiplayer and Dedicated servers,
  not mission/local or Skirmish servers. Publish the initial peer-host map recipe
  and restore known recipes when returning to previously generated maps.
- Enable the existing wizard for peer-host lobby administrators. Guest permissions,
  readiness/map-pool restrictions, stale selection checks and cooldown remain.
- Keep peer-generated packages in the shared client's lifetime/cleanup registry.
  Server shutdown must not dispose map packages still used by the hosting client.
  Dedicated servers retain their private-package shutdown cleanup.
- Ordinary imported maps without a known recipe still use the usual map-sharing
  workflow. This is not arbitrary map upload. Offline-only generation settings
  outside the bounded network recipe remain locally supported.

## Validation

`tools/test-lan-map-generator.ps1` drives the actual Multiplayer -> Create Server
-> generator UI in a host process, then separate guest and late-join processes
with fresh support folders and no generated maps. It verifies:
- host creation reaches a real non-dedicated lobby;
- initial map reconstruction and exact UID agreement;
- rejection of a guest's direct generation command;
- host Change Map generation, synchronization and late joining;
- actual match start and simulation on host and late client.

Successful evidence: TEMP `ymca-lan-map-probes/pass03`.
Initial UID c65a2fd28fbbe518a8cc992ae322bd5fe6377685;
changed UID 8cab32087ebb200196278c341cb2a0ebae4c51f5.
The first probe attempt exposed a diagnostic startup race (Map initially null),
fixed by snapshotting initially available maps and waiting for synchronized state.

Dedicated admin/guest/late-join regression also passed, including the waiting-map
start guard. Native Skirmish creation and repeated map changes passed unchanged.
Modular tests: 341 passed. Two real tournament host/map-download smoke tests passed.
No user's game process was stopped. Test screenshots/logs are retained under
`docs/validation/lan-map-generation/`; local compiled DLLs are staged for the
existing safe installer after the user's game is closed.

The feature requires updated peer-host and guest clients; existing v0.97.3
packages do not contain this fix. No additional release was authorized here.
