# Main-menu faction designer

## Current workflow

Restart the locally built game, then **Eigene Fraktion**.

- Select **GDI / Eagle**, **Nod / Black Hand**, **China / Tank-General** or
  **Alliierte / England** or **Sowjets / Russland** in the top-right base dropdown.
  The ordinary roster of the selected base remains available. Switching needs confirmation:
  the current faction is first saved in the library, then a fresh independent roster is
  created. Existing library names are reserved; a suffix prevents accidental replacement.
  This is not a conversion or mixing of existing designs across base factions.
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
comparison, China a Battlemaster, England a Challenger and Russia a Heavy Tank. Non-GDI maps have no free
GDI Carryall. All retain the enemy target.
Moving designs are additional vehicle-factory choices, grouped at palette orders
1000 onward under your vehicle names. The selected base's stock units remain available too.
The highest selected tech tier is required for production; Hum-Vees also retain the
stock GDI factory prerequisite. Stationary designs remain **preplaced / Carryall-only**.
Their production is intentionally disabled until a real factory/deployment workflow exists.

## Supported assemblies

There are currently **82 supported component combinations in 24 faction-specific bindings**:
eight GDI bindings (42 combinations), four Nod (8), four China (16), four Allied (8) and
four Soviet (8). These represent **23 vehicle families**: ARTY and ARTY.nod are two faction-specific bindings
of the same field-artillery family, not two independently counted families.

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
| Battlemaster | chbattle; CHBattlemasterCannon, Horde, nuclear upgrades | 950 / 40000 / 100 | 950 / 40000 / 100 |
| Dragon | chdragon; normal/Black Napalm + two Firewall channels | 600 / 28000 / 103 | 600 / 28000 / 103 |
| Gatling tank | chgtnk; eight ground/AA spin-up channels | 800 / 30000 / 108 | 800 / 30000 / 108 |
| Overlord / Emperor | choverlord; OverlordCannon, conditional Emperor body/armor | 2000 / 95000 / 56 | 2000 / 95000 / 56 |
| Allied Challenger | Challenger_Tank / 2TNK; 90mm, doctrine discount | 800 / 45000 / 82 | 800 / 45000 / 82 |
| Allied Ranger | JEEP; M60mg, sensors, pre-doctrine availability | 400 / 15000 / 157 | 400 / 15000 / 157 |
| Allied field artillery | ARTY; 155mm, frontal attack and ammunition explosion | 550 / 10000 / 56 | 550 / 10000 / 56 |
| Allied Prism tank | Prismtank; PrisTLaser including secondary beam cluster | 1350 / 22000 / 82 | 1350 / 22000 / 82 |
| Soviet heavy tank | Heavy_Tank; 125mm twin cannon, original recoil/husk | 1100 / 65000 / 68 | 1100 / 65000 / 68 |
| T-34 | T-34; normal 30mm and conditional cluster-upgrade channel | 600 / 42000 / 100 | 600 / 42000 / 100 |
| Heavy Tesla tank | TTNK.RA2; full TTankZapMK2, fire delay and attack animation | 1350 / 48000 / 100 | 1350 / 48000 / 100 |
| Mobile Flak | FTRK; separate FLAK-23-AA and FLAK-23-AG packages | 500 / 15000 / 118 | 500 / 15000 / 118 |

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
Scalar weapon fields for the original three groups, Buggy, Ranger, Challenger and Soviet Heavy Tank are compiled from JSON; complete
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

China uses `chinatnk` (Tank General). All four assemblies retain complete actor inheritance,
including damaged/pursuit/attack modifiers, announcements and voices. Battlemaster retains
Horde, uranium shells and nuclear tank/isotope upgrades. Dragon retains Black Napalm,
deployment, both Firewall phases and forced-move/undeploy rules without adding a second
movement or deploy system. Gatling preserves all eight channels, independent air/ground
spin-up, engagement locks, 1/3/5 shot thresholds and 40-tick decay; it is not a fixed-DPS MG.

Overlord retains the conditional Emperor voxel body and **80% received damage** under the
Tank General. Displayed HP and speed are raw template values, not effective durability or
runtime upgrade/formation bonuses. Both normal and Emperor tooltips use the custom vehicle
name while keeping their original conditions. Original voxel images/scales and turret
rotation remain unchanged; superheavy tracks use `sheavytracked` and hull turn speed 8.
Original factory/radar and promotion-exclusion prerequisites are retained. Once an upgrade
replaces a stock family, the unupgraded custom assembly follows the same production exclusion.
Overlord roof attachments, PDL/reflectors, nuclear-shell variants and arbitrary module mixing
are **not** enabled by these four bindings. The native snapshot does not expose the offline
Overlord roof/equipment slots as usable options. All China families offer their two explicitly
bound diesel motors and the two shared generators, without per-design price adjustments.

England's Challenger inherits the buildable national actor, not the disabled `2TNK` parent.
It keeps the 90mm weapon and Challenger graphics; its medium cannon hardware remains shared
with GDI. The exporter unions allowed ammunition for shared weapons, while the native binding
still denies GDI HE ammunition to Challenger. Existing GDI HE designs continue to load unchanged.
Ranger uses M60mg (not M60mgTD), an explicitly bound 48-turn turret, 80-turn hull and the shared
50-credit sensors counted once. It retains all three doctrine exclusions: the standard Ranger
is only producible before selecting a doctrine. Doctrine-specific Jeep variants remain unsupported.

Prism tank inherits the full PrisTLaser/LaserZap/FireCluster/PrisTBurst chain, targeting rules,
Prism Tech, death husk and voices. Its original promotion and radar requirements remain.
Despite its tank graphics, stock Prismtank is **Light armor / wheeled locomotor**; the native
binding preserves that. Displayed prices are **base prices**: inherited Armored Doctrine can
apply its common 85% production multiplier to Challenger, artillery and Prism tank in-game.
This is the existing faction-wide doctrine, not a designer-specific rebate. Range, damage,
cluster beams and price modifiers are not flattened to match a single preview number.
Allied generator/sensor admission uses unchanged shared component prices and ratings.
Battle Fortress/cargo weapons, Cryo/Chrono vehicles, other national tank graphics and other
Allied units are not included in these four bindings.

Russia adds four Soviet bindings. Heavy Tank keeps the full 125mm twin-shot package,
radar requirement and `!upg.heavy_tank` exclusion. Its shared medium cannon hardware uses
the Soviet twin carrier's cadence; GDI's single-shot cadence is unchanged. T-34 retains
both standard and conditional cluster weapons and the existing cluster prerequisite trait.
Like the custom GDI family policy, the custom Soviet roster explicitly admits the originally
North-Korean T-34 hull under Russia; vanilla national availability is not modified. Conditional
upgrades only activate if the owner actually grants their prerequisites.

Heavy Tesla preserves TTankZapMK2, the 13-tick armament delay, sprite turret attack animation,
Tesla damage upgrade and death husk. It keeps `dome`, Russia and `!promotion.tesla_arc`
requirements. The separate Arc actor variant is not included, and the designer does not
pretend its base weapon is already the Arc weapon. The 50 kWe weapon demand is counted once
(60 kWe total baseline). Mobile Flak preserves independent ground/air targeting, projectiles,
range and spread, Light armor/wheeled movement, and its barrage-promotion exclusion.
ISU, V3/missile launchers, Apocalypse, support/cargo units, Devil Tank and promoted variants
remain outside these four bindings. No stationary Soviet conversion is offered.

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
`blackh` / `FactionCA@13`, `chinatnk` / `FactionCA@22`, `england` / `FactionCA@1`
or `russia` / `FactionCA@5`.
Both playable slots are locked to that base; production requires
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

Verified: **72 Python tests**, **78 native tests**, and a successful **YMCA.sln Release build**.
The eight-family differential reports **1685 control / 1698 generated errors**: 13
inherited condition/palette diagnostics, **zero unexpected new errors**. Full-mod
lint remains unclean for the pre-existing reasons.

Native tests cover schema migration, roundtrip/backups, library isolation, shared budget,
copy independence, namespace stability, invalid combinations and weapon-channel preservation.
All 82 offered numeric combinations are checked against the independent Python calculator.
The earlier six-design sample includes tank hover and both stationary hull variants.
The eight-family sample can be exported with `MODULAR_GDI_EXPORT=/absolute/new-gdi.oramap`.
See `tools/modular/designer-gdi-validation.md` for the GDI differential result.
The Nod sample exports with `MODULAR_NOD_EXPORT=/absolute/new-nod.oramap` and reports
**1685 control / 1691 generated errors**, six inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-nod-validation.md`. Native tests also cover faction
filtering/rejection, independent library names, sensor accounting and both player slots.
The China sample exports with `MODULAR_CHINA_EXPORT=/absolute/new-china.oramap` and reports
**1685 control / 1693 generated errors**, eight inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-china-validation.md`. Tests cover all Dragon/Gatling
channels against the source declarations, conditional Emperor names, raw baseline values,
original production exclusions, faction isolation and deterministic map freezing.
The Allied sample exports with `MODULAR_ALLIES_EXPORT=/absolute/new-allies.oramap` and reports
**1685 control / 1692 generated errors**, seven inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-allies-validation.md`. Tests also cover shared
ammunition union/GDI HE regression, Ranger rotation/sensors/doctrine exclusions, Prism's
complete weapon package and movement/armor binding, and England player/reference selection.
The differential checker recognizes inspected parent names containing underscores and hyphens.
The Soviet sample exports with `MODULAR_SOVIET_EXPORT=/absolute/new-soviet.oramap` and reports
**1685 control / 1693 generated errors**, eight inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-soviet-validation.md`. Tests cover source scalar
baselines, both T-34 channels, independent Flak weapons, Tesla inheritance/power accounting,
shared cannon cadence isolation, national reference actor and both player slots.

The earlier single-design menu/lobby path was confirmed by the user. This expanded
multi-design UI, factory production, mixed-weapon firing and transport still need
interactive confirmation; successful builds/rule loading are not a live gameplay pass.
