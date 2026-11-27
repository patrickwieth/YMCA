# Urban maps and civilian garrisons — planning/code inventory

Status: planned, not implemented or runtime-validated for the new civilian art.
The user supplied the collection in `mods/ca/bits/misc/buildings/`; initial visual
categorization is in `urban-art-inventory.md`. Cutouts and runtime registration come
first. The map is now explicitly conceived as **Urban Warfare**, combining a city,
adjacent villages and outer settlements, plus roads and parked vehicles. See
`urban-warfare-map.md` for the district layout and asset-kit plan. No release, gameplay changes or asset substitutions are authorized by this
code inventory.

## Requested behavior

- Neutral civilian buildings can hold infantry.
- Occupation shows a flag in the occupying player's color; the building itself need
  not be recolored. This is distinct from the open building-remap work.
- Occupants fire from the building, optionally with a bonus weaker than a bunker.
  The bonus is tentative; no damage/range/capacity numbers have been approved.
- When the last occupant leaves, the building returns to neutral and the flag goes
  away. Empty/dead/ejected occupancy transitions must not leave permanent ownership.

## Existing code

The AS assembly is already loaded by `mods/ca/mod.yaml`.

- `engine/OpenRA.Mods.AS/Traits/Garrisonable.cs`: typed occupancy, capacity, entry,
  unloading, loaded conditions, exit/entered events and ejection settings.
- `engine/OpenRA.Mods.AS/Traits/Garrisoner.cs`: infantry entry orders, relationship
  checks and general/per-building conditions while garrisoned.
- `engine/OpenRA.Mods.AS/Traits/ChangeOwnerOnGarrisoner.cs`: first entry can transfer
  the building to the entrant; last exit restores the remembered original owner.
  For an initially neutral civilian building this is the desired neutral return.
  This depends on configuring relationships correctly: the entry handler skips an
  original owner already allied with the entrant. External owner changes also
  update the remembered owner, so arbitrary capture must not be enabled blindly.
- `engine/OpenRA.Mods.AS/Traits/Attack/AttackOpenTopped.cs`: fires actual occupant
  armaments from configured ports; supports both Cargo/Passenger and
  Garrisonable/Garrisoner event paths.
- `engine/OpenRA.Mods.Common/Traits/Render/WithIdleOverlay.cs`: conditional sprite
  overlay with player palette support, suitable for an occupied-only remapped flag.
  Existing flag sequences (`flagfly.shp`, `ctflag.shp`) are candidates, not yet an
  approved/rendered urban-building flag integration.

Current China/Soviet bunkers (`chbunker`, `NKOREA_BUNKER`) use **Cargo** plus
`^AttackingCargo`/`AttackOpenTopped`, not the temporary-neutral Garrisonable ownership
path. Existing infantry already have many `mounted`/`mountedaa` armaments. Those
are useful references, but may use stronger bunker/elite weapons; do not copy them
unchanged while describing the urban bonus as weaker. A dedicated urban condition,
armament selection or modifier policy must be designed and checked across factions.

## Remaining integration

1. Preserve supplied originals; create cutouts, appropriate footprints/ground
   registration, health/selection geometry and per-building entrance/exit locations.
2. Define a shared civilian-garrison actor template and enable Garrisoner on the
   intended infantry types. Do not disturb existing transport/bunker boarding.
3. Connect temporary ownership, occupation condition, flag palette/anchor and firing
   ports to the actual art. Define allowed occupants/capacity and any modest bonus.
4. Test neutral -> occupied -> neutral, first/last entrant, allies/enemies, occupied
   building targeting, blocked exits, occupant loss and building destruction.
   Explicitly review `OwnerChangedAffectsGarrisoners` so allied infantry are not
   silently transferred to another player. Prevent unintended selling/permanent
   capture. Reuse stock mechanisms where correct rather than assuming all edge
   cases already match the desired design.
5. Integrate civilian groups into urban maps with usable streets and entrances;
   preserve starts, resources and path access. Add bot use afterward if desired
   (`LoadGarrisonerBotModuleCA` already exists).

Backlog clarification: building remap colors remain open. Scrin building
visual differentiation is closed for now; Archipelago/long-term playtests are not
backlog items. See `building-followup-pending.md` for the current summary.
