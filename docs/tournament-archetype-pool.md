# Tournament archetype pool command correction

After v0.97.4 rollout, `/map-remove` still suggested legacy official maps even
when generated tournaments were enabled. Both mutation handlers also rejected
archetype-mode operations. The earlier tournament readiness checks did not cover
these administration commands.

The shared pool commands now use the configured mode consistently:

- `/map-remove` lists enabled archetypes and disables the chosen type.
- `/map-add` lists disabled archetypes and restores the chosen type.
- `/map-pool` displays the enabled pool, not the unconditional catalogue.
- Older state files default to all nine archetypes enabled. Exclusions persist in
  `DisabledMapArchetypes`, independently of the legacy `MapPool`.
- Creation and start use the enabled pool. Start refreshes it, matching existing
  fixed-map behavior. An empty pool blocks creation/start explicitly.
- Running tournaments retain their copied pool and immutable map requests. Pool
  edits do not change their choices, deciders, seeds or packages.
- Handlers validate known IDs; display names cannot introduce custom archetypes.

Tests cover legacy-state loading, persistence/restart, case-insensitive IDs,
autocomplete add/remove/filtering, empty pools, unknown/duplicate/concurrent
commands, legacy-mode isolation and pending versus running tournament behavior.
This is a bot-only correction; clients remain compatible with v0.97.4.

Rollout: pending guarded deployment; no tournament cancellation or state reset is
part of this correction.
