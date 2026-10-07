# Main-menu faction designer

## Current workflow

**Play > New faction** creates a faction. **My factions > select a faction** opens its overview,
where names, catalog points and vehicle entries are managed. **Edit** opens the spatial vehicle
editor; Back returns to the overview. Deletion always requires confirmation, and renaming retains
the faction's identity instead of creating another library entry.

Current balancing factions use **level 50 / 100**, hence **50 catalog points**, with varying
per-vehicle point costs. The separate 16-design prototype limit remains. Other unit categories and
the custom tech-tier Commander Tree are not implemented yet.

See [faction navigation and silhouettes](faction-navigation.md) for persistence, migration, UI
boundaries, the new chassis-specific silhouettes and live acceptance checks.

## Previous flat-editor workflow (historical)

Before the overview was introduced, the entry point was **Eigene Fraktion**.

- Select **GDI / Eagle**, **Nod / Black Hand**, **China / Tank-General** or
  **Alliierte / England**, **Sowjets / Russland** or **Scrin / Traveler-59** in the top-right base dropdown.
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
- A **260×192 actor preview on the right** shows the bound body/turret/barrel artwork,
  using the actual sprite **and voxel** render pipelines. It rotates once every **24 seconds**;
  the button pauses/resumes rotation. All headings are fitted together to avoid zoom pumping.
  This is an inspection view, not a movement order: stationary vehicles can rotate in the
  viewer without gaining body rotation in-game. Hover/stationary placeholder art stays labelled
  in the existing warning. No actors are spawned and no live rules or simulation state change.
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
comparison, China a Battlemaster, England a Challenger, Russia a Heavy Tank and Scrin a Seeker. Non-GDI maps have no free
GDI Carryall. All retain the enemy target.
Moving designs are additional vehicle-factory choices, grouped at palette orders
1000 onward under your vehicle names. The selected base's stock units remain available too.
The highest selected tech tier is required for production; Hum-Vees also retain the
stock GDI factory prerequisite. Stationary designs remain **preplaced / Carryall-only**.
Their production is intentionally disabled until a real factory/deployment workflow exists.

## Supported assemblies

There are currently **168 supported component combinations in 89 faction-specific bindings**:
fifteen GDI bindings (52 combinations), fourteen Nod (28), eight China (24), thirteen Allied (17),
twenty-five Soviet (29) and fourteen Scrin (18). These represent **88 vehicle families**: ARTY and ARTY.nod are two faction-specific bindings
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
| Gun Walker | GUNW; GunWalkerZap + GunWalkerZapAA, frontal attack | 650 / 30000 / 113 | 650 / 30000 / 113 |
| Seeker | SEEK; PlasmaDiscs, LightHoverVehicle | 800 / 20000 / 135 | 800 / 20000 / 135 |
| Corrupter | CORR; CorrupterSpew, attack animation and death explosion | 700 / 45000 / 82 | 700 / 45000 / 82 |
| Devourer | DEVO; DevourerLaser burst, floating turret, LightHoverVehicle | 1250 / 35000 / 90 | 1250 / 35000 / 90 |
| Titan | TITN; TitanGun + TitanTusk, regeneration and walker/turret animation | 2000 / 100000 / 50 | 2000 / 100000 / 50 |
| Slingshot | SLNG; SlingshotAA, native LightHoverVehicle | 550 / 13500 / 133 | 550 / 13500 / 133 |
| MARV | MARV; IonZap.Marv, terrain harvesting and regeneration | 10000 / 200000 / 40 | 10000 / 200000 / 40 |

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

Traveler-59 adds four Scrin bindings with complete sprite/light/driver/attack inheritance.
Gun Walker retains distinct ground/AA weapons and its turn/move animation. Corrupter retains
its complete spew weapon, frontal aiming, shoot animation and death explosion; this does not
invent a healing or ammunition mechanic. Their walker hardware uses the original `wheeled`
engine locomotor despite walking graphics. Seeker and Devourer retain full LightHoverVehicle
behavior (`lighthover`), bobbing, water targeting and EMP/driver-death sinking risk. No duplicate
hover trait group is added. Devourer retains its floating turret, quantized facings and **1000**
body turn speed; its 13-shot offset laser sweep is not replaced with a single damage event.
All four preserve their original Light armor, production requirements and upgrade exclusions.

Scrin use separately admitted **drive/converter abstractions**, not diesel motors disguised
as established lore. Their numeric price/mass/power ratings reuse the shared baseline and
efficient-generator model, but physical allocations remain experimental. These are static
calculator budgets, not an in-match electrical network or fuel simulation. Human generators
cannot be selected on Scrin designs, and Scrin converters cannot be inserted into GDI profiles.
The broad JSON calibration catalog and Excel snapshots remain untouched. Tripods, Lacerators,
shield/teleport support, aircraft and promoted Scrin variants remain unbound. All six base
faction groups now have representatives, **not complete vehicle rosters or every subfaction**.

The frozen actor and weapon IDs are unique per design and stable across renaming/reordering.
A copied design gets a new ID and an independent component dictionary. All designs are
validated before generation; incompatible parts, invalid faction, unsupported fields,
duplicate IDs/names and budget overflow fail closed. CP-bearing modules are not enabled yet.

## Preview and further GDI combat bindings

Titan is **sprite-based in this mod**, not a voxel model: `sequences/gdi.yaml` uses `titan.shp`
with 32 stand/run facings and a separate turret sequence at frame 416. It inherits `^TurretedWalker`.
The native binding retains both weapon channels, regeneration, death explosion, transport offset
and the `!upg.titan` production exclusion. Like Juggernaut, its Talon hull is admitted to the custom
GDI roster without modifying vanilla national availability.
Slingshot keeps its AA-only weapon, real light-hover behavior, death explosion and promotion.
MARV retains its ion burst, health regeneration, terrain-conditional CashTrickler/MARVest behavior
and `promotion.marv` / `miss.gdi` gates; no second harvesting implementation is injected.
These are the first three additions from the requested remaining combat inventory, **not all 61**.
The subsequent [shared-gear/combat expansion](shared-gear-combat-expansion.md) adds the ten
remaining Nod and four remaining China base combat groups. The subsequent
[all-ground-combat expansion](all-ground-combat.md) closes the remaining **44 base-family gaps**.
There are now **zero wholly missing production-candidate ground combat base-family groups**.
Promoted/national variants, support/economy vehicles, aircraft and naval remain separate inventory sections.

The viewer uses the bound stock actor as the graphics template. It does not simulate upgrades,
firing, passengers, damage or all conditional combat states. Initial visual conditions and ammo
are resolved on preview-only clones: for example one Emperor body for Tank General and the
loaded SSM turret. Original trait info is not mutated. Models/sequences/palettes use the current
menu world's caches; asset/render failures disable only the preview and write the detailed error
to the debug log. CPU trait checks are not proof of GPU rendering on the user's machine.

## Visual space prototype

The [side-view inventory](vehicle-space-prototype.md) is now embedded in the **single faction designer**;
there is no separate Space designer window. Faction/library/roster controls stay at the top. The left sidebar
reveals mount selection after chassis selection, and component options after mounting. The center shows
an independent tank-shaped contour around the grids; the right retains the rotating preview and adds a
scrollable vertical property list. Native component choices are applied on valid drops. Grid coordinates
and battery/PDL/Reflector planning blocks are still temporary and are not compiled; Save/Test asks for
confirmation before ignoring planning-only modules. Existing native profiles and maps retain their format.

## English component labels

All 216 runtime component labels are English, including chassis, running gear, engines, generators,
mounts, weapons, ammunition and armor. Native weapon/ability summaries and turret occupancy text are
English too. `tools/modular/designer_english_labels.py` is the explicit ID-keyed presentation adapter,
applied last by the catalog exporter so regeneration cannot restore the old German labels.
Only display text changes: internal IDs, user-authored design names, numeric fixtures, broad calibration
sources and Excel files remain unchanged. Existing saved rosters resolve the new labels without migration.

## Named ground turret modules

[Prism, artillery, missile and sonic turrets](ground-turret-modules.md) now have named native module
IDs on Prismtank, HOWI, STNK and DISR respectively. Each consumes one slot and displays its module
name and occupancy. Original weapon profiles and sprite/voxel assemblies remain source-qualified;
Battle Fortress does not yet accept arbitrary mixtures. Legacy mount IDs migrate on load, including
chassis-specific Integrated Mount migration for HOWI/STNK. All 168 numeric results remain unchanged;
no new electrical balancing is applied. `turret_modules` records source geometry/conditions for the
next explicit mounting pass, not permission to splice unreviewed graphics.

## Existing variants and loadout research

The [resolved loadout inventory](existing-vehicle-loadouts.md) captures all 388 ground actors from
the six faction files, including 252 editorial variants. There are still 247 gated-candidate variants
without native compiler bindings. It separates actual weapon channels and turret traits from physical
mount slots, records PDL/Reflector/cargo/deploy behavior, and highlights inherited anomalies.
The separate [air/naval loadout inventory](existing-air-naval-loadouts.md) now covers another
160 declarations, including 114 structural production candidates; carriers, slave drones/missiles,
nonproduction aircraft and non-mobile upgrade objects stay distinct. Aircraft speed and flight/landing
parameters, transport/Carryall, rearming bases and spawned aircraft are exported from resolved traits.
This is read-only research: no new power consumption, balance changes or runtime assembly admission.

## Shared gear and further combat coverage

See [shared running gear and combat expansion](shared-gear-combat-expansion.md) for all fourteen
new stock baselines, preserved cargo/cloak/deployment/boost/missile behavior, exact non-national
production gates and the explicit admission of national units to the custom representative rosters.
MLRS, SSM and Prism now share **Light Tracks** without changing their `wheeled` movement behavior.
The light-tracks compatibility class is distinct from the engine locomotor. Gear count drops from
16 to **14**; old profile IDs migrate on load and all 96 previous numeric combinations stay unchanged.
New families also share mount/store hardware rather than creating a separate copy for every vehicle.
Class admission does not override explicit graphics/trait assembly restrictions.
The follow-up adds 44 stock combat bindings using shared hardware and chassis-bound original armaments.
Only two distinct gear profiles are added (Heavy Wheels and Amphibious Micro Drive), bringing gear count
back to **16**. All 124 preceding numeric combinations are unchanged.
**Battle Fortress** is a **superheavy chassis** with a separate **Bunker Module**, not a chassis named
Bunker. Battle Fortress has **3 turret slots** and the bunker occupies **3/3**, leaving none
for another turret. Capacity is validated natively and by the independent calculator.
The 1000-credit module is superheavy-only and the complete 3000-credit assembly inherits the
original `BATF.Bunker` cargo/weapons/art and promotion gates. Other superheavy hulls require explicit
bunker-compatible graphics/trait bindings before the module is offered there.
The template button now fills available slots without rolling back when the catalog exceeds the
unchanged **16-design / 50-point** limit. Further chassis stay selectable individually.

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
`russia` / `FactionCA@5` or `traveler` / `FactionCA@18`.
Both playable slots are locked to that base; production requires
its `structures.<base>` token. Internal faction IDs, original rosters and commander behavior
remain intact. Other players of that base on the snapshot can produce the same custom roster. This is not yet
independent per-player faction admission/synchronization.

## Scope still open

The complete missing-vehicle checklist (combat, economy/support, variants and air/naval)
is in **`docs/modular/missing-vehicles.md`**, generated from rules and actual native bindings.

This is both a multi-design editor and an expanded native compiler, but **not all 75
catalog components, complete faction rosters or all subfactions**. It still uses the known lab terrain, not arbitrary
map selection. CP unlocks, additional vehicle families, freely composable multiple mounts,
passenger loadouts, stationary factory/deployment handling, real hover/platform art,
full preview state simulation and dependency-aware asset loading remain future work.
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

Verified: **110 Python tests**, **180 native tests**, and a successful **YMCA.sln Release build**.
The eight-family differential reports **1685 control / 1698 generated errors**: 13
inherited condition/palette diagnostics, **zero unexpected new errors**. Full-mod
lint remains unclean for the pre-existing reasons.

Native tests cover schema migration, roundtrip/backups, library isolation, shared budget,
copy independence, namespace stability, invalid combinations and weapon-channel preservation.
All 168 offered numeric combinations are checked against the independent Python calculator.
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
The Scrin sample exports with `MODULAR_SCRIN_EXPORT=/absolute/new-scrin.oramap` and reports
**1685 control / 1689 generated errors**, four inherited diagnostics and **zero unexpected
new errors**; see `tools/modular/designer-scrin-validation.md`. Tests cover all four raw
baselines, turn rates, weapon inheritance, native hover/no duplicate traits, Light armor,
converter admission/isolation, shared ratings and Traveler map/snapshot selection.
The expanded eleven-design GDI sample reports **1685 / 1701** errors, all 16 additions
inherited, **zero unexpected diagnostics**: `tools/modular/designer-gdi-expanded-validation.md`.
`--check-custom-faction-previews` validates all **89** preview-only trait bindings headlessly,
including source isolation, single Emperor body and loaded SSM state. Separate tests cover
24-second rotation, fitting, UI registration and Titan's sprite source. Interactive sprite/voxel
rendering was confirmed by the user for the earlier viewer; the newly added models/abilities
still need live verification. Additional engine-resolved checks cover all fourteen new raw stock
baselines (cost, HP, speed, turn, armor, locomotor) and every intrinsic armament.
Expanded Nod differential: **1685 / 1706**, 21 inherited additions; China: **1685 / 1705**, 20 inherited
additions. Both have **zero unexpected diagnostics**; see `designer-{nod,china}-combat-validation.md`.
The final 44 additions were checked in five immutable-map batches, all with zero unexpected diagnostics:
GDI 1685/1689, Allies 1685/1706, Scrin 1685/1695, Soviets 1685/1712 and 1685/1694.
See `designer-stock-*-validation.md` and [the final coverage report](all-ground-combat.md).

The earlier single-design menu/lobby path was confirmed by the user. This expanded
multi-design UI, factory production, mixed-weapon firing and transport still need
interactive confirmation; successful builds/rule loading are not a live gameplay pass.
