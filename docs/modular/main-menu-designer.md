# Main-menu faction designer

## Current workflow

Restart the locally built game, then **Eigene Fraktion**.

- Select **GDI / Eagle** or **Nod / Black Hand** in the top-right base dropdown.
  The ordinary roster of the selected base remains available. Switching needs confirmation:
  the current faction is first saved in the library, then a fresh independent roster is
  created. Existing library names are reserved; a suffix prevents accidental replacement.
  This is not a conversion or mixing of existing GDI and Nod designs.
- **Neu / Kopie / Entfernen** manage up to **16 own vehicle designs**. Select a
  design in the numbered dropdown to edit it. Removing a design needs confirmation.
- **Vorlagen** adds missing supported assemblies for the selected base in one click. It
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
stands near that start. GDI keeps its MTNK comparison and Carryall; Nod gets an LTNK
comparison and no free GDI Carryall. Both maps retain the enemy target.
Moving designs are additional vehicle-factory choices, grouped at palette orders
1000 onward under your vehicle names. The selected base's stock units remain available too.
The highest selected tech tier is required for production; Hum-Vees also retain the
stock GDI factory prerequisite. Stationary designs remain **preplaced / Carryall-only**.
Their production is intentionally disabled until a real factory/deployment workflow exists.

## Supported assemblies

There are currently **50 supported component combinations across twelve families**:
eight GDI families (42 combinations) and four Nod families (8 combinations).

| Assembly | Existing actor / full weapon bindings | Reference price / HP / speed | Configured default |
|---|---|---|---|
| Battle tank | MTNK; 120mm or experimental 120mmHEAT | 900 / 52000 / 82 | 900 / 52000 / 82 |
| MG scout | HMMV; M60mgTD, including sensor/driver/targeting traits | 400 / 15000 / 157 | 400 / 15000 / 157 |
| Missile launcher | MLRS; **227mm + 227mmAA** | 950 / 16000 / 82 | 950 / 16000 / 82 |
| Juggernaut | JuggernautGun + JuggernautDummyAim | 2000 / 60000 / 50 | 2000 / 60000 / 50 |
| Mammoth | 130mmTD + MammothTusk, regeneration | 1700 / 78000 / 52 | 1700 / 78000 / 52 |
| Native Hover MLRS | Four ground/AA/upgrade channels | 1150 / 18000 / 113 | 1150 / 18000 / 113 |
| Disruptor | SonicZap + visual beam + both upgrade channels | 1500 / 75000 / 56 | 1500 / 75000 / 56 |
| Mammoth Mk II | Railgun, ground missiles, AA missiles, regeneration | 10000 / 350000 / 35 | 10000 / 350000 / 35 |
| Nod light tank | LTNK; complete 30mm package | 625 / 41250 / 100 | 625 / 41250 / 100 |
| Nod buggy | BGGY; M60mgTD + sensors | 350 / 14000 / 157 | 350 / 14000 / 157 |
| Nod artillery | ARTY.nod; 155mmTD, frontal attack, ammunition explosion | 550 / 10000 / 56 | 550 / 10000 / 56 |
| Nod SSM | SSM; HonestJohn, ammo pool/reload and missile graphics | 1050 / 15000 / 82 | 1050 / 15000 / 82 |

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
Scalar weapon fields for the original three groups and the Buggy are compiled from JSON; complete
inherited projectile and warhead packages remain intact. MLRS secondary range/damage/reload
are explicit independent fields. The other families use **template-locked weapon
packages** instead: each channel derives its complete source weapon without scalar overrides.
Their JSON records configure physical/economic values, not a fake single damage/cadence for
a multi-weapon or beam system. Neutral calculator cadence fields are not emitted as rules.

Juggernaut retains stop-to-fire, aim/attack/run/death animations and its TargetDamage dummy
weapon; it does not acquire a turret or a firing delay on the dummy. Disruptor retains the
AreaBeam behavior, allied-damage rules, visual companion and conditional upgrade channels.
Native Hover MLRS uses its actual hover artwork and does not get a second hover trait group.
Mk II retains AttackFollow, regeneration, repair/transport weight and its original voxel scale;
its voxel image is explicitly rebound so generated actor IDs cannot become missing asset names.

Each preserved channel gets a unique `.w0`, `.w1`, etc. weapon ID; armament names, conditions,
attack lists, muzzle positions and delays remain inherited. No added Carryable or turret traits
are forced onto these complex families. Walker gear reuses `sheavytracked` for Juggernaut and
`heavytracked` for Mk II, exactly as in the original rules.

The original `promotion.hover_mlrs` and `promotion.mammoth_mkii` / `miss.gdi` unlocks remain
production requirements in addition to calculated tech. Preplaced examples are for testing.
The custom GDI roster allows selected GDI families independently of their original national
unit-list restrictions (e.g. Talon Juggernaut / ZOCOM Disruptor); the base remains Eagle, so
inherited nation-specific upgrades are only active if that owner actually grants them. Global
unit availability, existing faction rules and the original actors are not modified.

Nod uses native faction admission, not just a filtered menu: mixed-faction profiles and
stationary Nod builds are rejected even when loading hand-edited JSON. New/copy/templates
use the selected base. The efficient generator is explicitly admitted for Nod with the
same shared price and ratings; broad JSON calibration and workbook files are not edited.
Buggy sensors are charged once (50 credits / 50 kg / 5 kWe), just like Hum-Vee sensors.
Its PDL/AA promotion exclusions remain; those variants are not yet designer assemblies.
SSM keeps its original Temple and `promotion.ssm_launcher` requirements, two-round ammo
pool, reload behavior, aiming movement pause and zero/one/two-missile turret sequences.
Nod artillery retains `lighttracked`; SSM retains stock `wheeled` despite tracked art.
No generic reload/damage overrides are inserted into those complete weapon templates.
`HonestJohn` uses the exact weapon declaration case for MiniYaml inheritance, unlike the
case-insensitive weapon lookup performed by an Armament.

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

The test map renames the selected faction's display name: `eagle` / `FactionCA@11` or
`blackh` / `FactionCA@13`. Both playable slots are locked to that base; production requires
its `structures.<base>` token. Internal faction IDs, original rosters and commander behavior
remain intact. Other players of that base on the snapshot can produce the same custom roster. This is not yet
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
- `CustomFactionBases.cs`: explicit base bindings and independent new-roster creation.
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

Verified: **67 Python tests**, **51 native tests**, and a successful **YMCA.sln Release build**.
The eight-family differential reports **1685 control / 1698 generated errors**: 13
inherited condition/palette diagnostics, **zero unexpected new errors**. Full-mod
lint remains unclean for the pre-existing reasons.

Native tests cover schema migration, roundtrip/backups, library isolation, shared budget,
copy independence, namespace stability, invalid combinations and weapon-channel preservation.
All 50 offered numeric combinations are checked against the independent Python calculator.
The earlier six-design sample includes tank hover and both stationary hull variants.
The eight-family sample can be exported with `MODULAR_GDI_EXPORT=/absolute/new-gdi.oramap`.
See `tools/modular/designer-gdi-validation.md` for the GDI differential result.
The Nod sample exports with `MODULAR_NOD_EXPORT=/absolute/new-nod.oramap` and reports
**1685 control / 1691 generated errors**, six inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-nod-validation.md`. Native tests also cover faction
filtering/rejection, independent library names, sensor accounting and both player slots.

The earlier single-design menu/lobby path was confirmed by the user. This expanded
multi-design UI, factory production, mixed-weapon firing and transport still need
interactive confirmation; successful builds/rule loading are not a live gameplay pass.
