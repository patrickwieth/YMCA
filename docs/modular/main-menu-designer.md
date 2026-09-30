# Main-menu faction designer

## Current workflow

Restart the locally built game, then **Eigene Fraktion**.

- The base is still **GDI / Eagle**, with its ordinary roster retained.
- **Neu / Kopie / Entfernen** manage up to **16 own vehicle designs**. Select a
  design in the numbered dropdown to edit it. Removing a design needs confirmation.
- **Vorlagen** adds missing cannon, MG and missile assemblies in one click. It
  does not replace your existing designs, names or selections.
- **Einbaugruppe** selects the compatible hull/weapon/artwork group. Incompatible
  parts are explicitly reset to valid defaults; compatible motor/generator choices
  are retained. Other dropdowns only offer supported combinations for this group.
- Rename each vehicle. Duplicate names within the faction are rejected.
- The displayed price, HP, speed, power, tech and weapon summary refer to the
  selected vehicle. **Entwuerfe …/50** is the shared development budget for the
  additional designs, not the existing base roster or commander-tree CP budget.
- **Speichern** saves all designs, not just the selected vehicle. **Fraktion laden**
  opens a previously saved named faction. Saving under a new distinct faction name
  creates another library entry. Names are matched case-insensitively, ignoring
  outer spaces. Unsaved changes prompt before leaving or loading another faction.
- **Speichern und Testspiel** freezes the whole roster into a separate map and opens
  its skirmish lobby. No Python installation is needed by players.

Use the first player slot and start **A**. One example of **every configured design**
stands near that start, alongside the stock MTNK comparison, enemy target and Carryall.
Moving designs are additional vehicle-factory choices, grouped at palette orders
1000 onward under your vehicle names. The stock GDI units remain available too.
The highest selected tech tier is required for production; Hum-Vees also retain the
stock GDI factory prerequisite. Stationary designs remain **preplaced / Carryall-only**.
Their production is intentionally disabled until a real factory/deployment workflow exists.

## Supported assemblies

There are currently **32 supported component combinations** across these groups:

| Assembly | Existing actor / full weapon bindings | Reference price / HP / speed | Configured default |
|---|---|---|---|
| Battle tank | MTNK; 120mm or experimental 120mmHEAT | 900 / 52000 / 82 | 900 / 52000 / 82 |
| MG scout | HMMV; M60mgTD, including sensor/driver/targeting traits | 400 / 15000 / 157 | 400 / 15000 / 157 |
| Missile launcher | MLRS; **227mm + 227mmAA** | 950 / 16000 / 82 | 950 / 16000 / 82 |

These exact default scalar matches are constructed calibration fits, NOT independent
balance validation. Allocations for mass, power and component prices remain experimental.

- Tank: tracks, experimental hover or GDI-only stationary gear; two diesel motors,
  two generators, normal or HE shells. Hover baseline is 1100 / 52000 / 79; stationary
  baseline 850 / 52000 / 0. HE adds 25 credits. Hover/stationary tank art is a placeholder.
- MG scout: real Hum-Vee body and MG turret; light wheels or a stationary test platform,
  light diesel, two generators, Light armor. Inherited scout sensors are mandatory and
  transparently counted once: **50 credits / 50 kg / 5 kWe**, included in the 400 baseline.
  Stationary scout art remains a parked-vehicle placeholder.
- MLRS: real MLRS body, launcher and aiming animation. The two weapon channels retain
  independent targeting, reloads, projectile guidance, falloff, armor matchups, effects,
  muzzle positions, delays and attack behavior. Ground defaults: reload 120, burst 2,
  range 10/minimum 4, raw damage 1300 with existing falloff; AA: reload 80, burst 2,
  range 10.5, raw damage 3000. Raw damage is NOT DPS.
- The MLRS keeps the stock **wheeled engine locomotor** despite its tracked-looking
  artwork. Its frontal attack requires hull turning: a stationary MLRS is rejected,
  rather than shipping a launcher that cannot aim. No improvised hover MLRS conversion.

Parts remain conceptually separate, but graphically bound assemblies prevent a Hum-Vee
MG or MLRS launcher from silently becoming a cannon-looking tank. MG/MLRS rendering
inherits their complete original actor templates, not just the MTNK with another weapon.
Scalar weapon fields are compiled from JSON; complete inherited projectile and warhead
packages remain intact. MLRS secondary range/damage/reload are explicit independent fields.

The frozen actor and weapon IDs are unique per design and stable across renaming/reordering.
A copied design gets a new ID and an independent component dictionary. All designs are
validated before generation; incompatible parts, invalid faction, unsupported fields,
duplicate IDs/names and budget overflow fail closed. CP-bearing modules are not enabled yet.

## Persistence and match isolation

- Active profile: `<OpenRA SupportDir>/Modular/custom-faction.json`.
- Named library: `<OpenRA SupportDir>/Modular/Factions/faction-<name hash>.json`.
- The old **schema-1 single-tank profile migrates in memory** to schema 2 without
  changing its name, parts or calculated values. Opening alone does not rewrite it.
- Explicit saves use a temporary file and retain the preceding file as `.bak`.
  Library and active-profile saves are individually atomic, not a two-file transaction.
  Corrupt profiles are reported, never silently repaired or overwritten on opening.
- Maps: `<OpenRA SupportDir>/maps/ca/modular/custom-<content hash>.oramap`.
  Existing snapshots are never overwritten; each contains its frozen roster and rules.
- Ordinary matches never read the editable profile JSON. Editing it cannot change
  already generated rules or a running match.
- `CustomFactionSkirmishLogic` bypasses ordinary skirmish restoration **and persistence**
  for these test sessions. The earlier bug where the lobby silently restored the old
  lab map remains fixed. A fresh default bot is added; normal skirmishes retain stock
  behavior. Stock AI build lists are not yet extended to deliberately produce new IDs.

The test map renames the existing `eagle` faction's display name via `FactionCA@11`;
its internal faction ID, original roster and commander behavior remain intact. Other
Eagle players on that snapshot can produce the same custom roster. This is not yet
independent per-player faction admission/synchronization.

## Scope still open

This is both a multi-design editor and an expanded native compiler, but **not all 75
catalog components or all factions**. It still uses the known lab terrain, not arbitrary
map selection. CP unlocks, additional vehicle families, freely composable multiple mounts,
passenger loadouts, stationary factory/deployment handling, real hover/platform art,
main-menu vehicle previews and dependency-aware asset loading remain future work.
Normal map transfer/checksums carry snapshots; multiplayer/save-load equivalence still
needs live validation. There is no dedicated water course on this terrain yet.

## Implementation and checks

- `OpenRA.Mods.CA/Modular/CustomVehicleAssembly.cs`: explicit graphics/trait bindings.
- `CustomFactionDesign.cs`: native calculations and actor/weapon/map generation.
- `CustomFactionRoster.cs`: roster validation, stable IDs, migration and library saving.
- `OpenRA.Mods.CA/Widgets/Logic/CustomFactionLogic.cs`: full editor and test launch.
- `mods/ca/modular/designer-catalog.json`: generated supported component snapshot.
- `tools/modular/export_designer_catalog.py`: developer-only exporter and independent
  Python numeric fixtures. Broad catalog and user-edited Excel snapshots are unchanged.

```sh
python tools/modular/export_designer_catalog.py
python -m unittest discover -s tools/modular -q
dotnet test Modular.Tests/Modular.Tests.csproj -c Release
dotnet build OpenRA.Mods.CA/OpenRA.Mods.CA.csproj -c Release
```

Engine differential check (a fresh file path is required for export):

```sh
MODULAR_ROSTER_EXPORT=/absolute/new-roster.oramap dotnet test Modular.Tests/Modular.Tests.csproj -c Release
python tools/modular/check_prototype.py --map /absolute/new-roster.oramap
```

Verified: **64 Python tests**, **30 native tests**, and a successful **YMCA.sln Release build**.
The six-design differential reports **1685 control / 1697 generated errors**: 12
inherited condition/palette diagnostics, **zero unexpected new errors**. Full-mod
lint remains unclean for the pre-existing reasons.

Native tests cover schema migration, roundtrip/backups, library isolation, shared budget,
copy independence, namespace stability, invalid combinations and weapon-channel preservation.
All 32 offered numeric combinations are checked against the independent Python calculator.
The six-design engine sample also includes tank hover and both stationary hull variants.
See `tools/modular/designer-roster-validation.md` for the exact differential result.

The earlier single-design menu/lobby path was confirmed by the user. This expanded
multi-design UI, factory production, mixed-weapon firing and transport still need
interactive confirmation; successful builds/rule loading are not a live gameplay pass.
