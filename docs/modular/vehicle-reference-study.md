# Modular vehicles: reference study

Status: design research, not executable mod rules. Baseline: `modular` at
`7f5aeadf` (derived from `isometric`), not the newer `tournament-bot` rules.
No gameplay changes or new power/mass values are introduced by this document.

## Agreed scope

Players design a faction and its vehicles in the main menu. Designs are frozen,
validated and shared before a match. Match loading compiles these designs into
ordinary actor/weapon rules. No runtime ruleset mutation or power simulation.

Separate chassis, drive, armor, weapon carrier, weapon, ammunition and equipment.
Compatibility includes mechanical interfaces AND available artwork. Components
may be locked together without merging their conceptual responsibilities.
Existing vehicles are reference designs, not an excuse for arbitrary per-design
stat overrides. No noise mechanic.

## Reading the reference values

These are raw rule values, not SI measurements. Speeds are `Mobile.Speed`, HP are
`Health.HP`, delays/durations are simulation ticks and ranges use OpenRA cell
notation. Damage is the configured warhead value, NOT final damage per shot:
falloff, armor, other warheads and conditional modifiers still apply.

The table describes base states without veterancy, terrain, faction or upgrade
bonuses unless explicitly named. This is a targeted source inspection, not a
complete resolved-rules dump or a visual compatibility test.

## Reference designs

| Actor / state | Cost | HP | Speed | Armor | Main mount / weapon |
|---|---:|---:|---:|---|---|
| `chbattle` | 950 | 40000 | 100 | Heavy | Rotating single cannon, turret turn speed 12 |
| `chbattle` + `promotion.nuclear_tanks` | 950 | 40000 | 100 × 125% | Heavy | Same cannon; nuclear death effect |
| `chbattle.Autoloader` | 950 | 40000 | 100 | Heavy | Same turret, three-shot autoloader |
| `chbattle.Autoloader.PDL` | 1600 | 40000 | 100 | Heavy | Autoloader plus independent point defense |
| `chbattle.Autoloader.Reflector` | 950 | 40000 | 100 | Reflector | Autoloader plus reflector armor |
| `Juggernaut` | 2000 | 60000 | 50 | Heavy | Fixed three-barrel artillery; whole vehicle aims |
| `Juggernaut.Emp` | 2000 | 60000 | 50 | Heavy | Same gun and conventional damage plus EMP |
| `TTNK.RA2` | 1350 | 48000 | 100 | Heavy | Rotating twin Tesla mount, turret turn speed 28 |

Sources:
- `mods/ca/rules/china/vehicles.yaml`: `chbattle` and its variants.
- `mods/ca/rules/china/defaults.yaml`: `^AtomicTank`, `^UranShells`, `^HordeBonus`.
- `mods/ca/rules/gdi/vehicles.yaml`: `Juggernaut`, `.Emp`, `.Firerate`.
- `mods/ca/rules/gdi/defaults.yaml`: `^GDIWalkerUpgrades`.
- `mods/ca/rules/soviet/vehicles.yaml`: `TTNK.RA2`.
- `mods/ca/rules/soviet/defaults.yaml`: `^TeslaUnit`.
- `mods/ca/rules/defaults.yaml`: `^Tank`, `^FightingTank`,
  `^FightingTankTurreted`, `^PointLaserDefenseSystem`, `^ReflectorArmor`.

### Important distinctions

- Nuclear propulsion and nuclear ammunition are independent. `^AtomicTank`
  supplies a conditional 125% speed multiplier and `CHAtomicTankExplode` on
  death. `chbattle.Nuclear_Shells` instead changes the main weapon. Do not bundle
  nuclear shells into every nuclear drive.
- In THIS branch, isotope stability suppresses the nuclear death explosion;
  `being-warped` also prevents it. The newer branch's balance changes must not
  silently be assumed present here.
- The Battlemaster also has horde bonuses and uranium-shell firepower upgrades.
  These are separate rule layers, not intrinsic engine power.
- Juggernaut walker upgrades apply 110% speed and a 90% received-damage
  multiplier under their respective conditions. Keep those outside the base fit.
- Tesla damage has a conditional 120% firepower multiplier.
- Static design power does not prohibit existing in-match EMP, terrain,
  veterancy or other gameplay modifiers. It only means module use never
  recalculates the design's mechanical power budget.

## Weapon reference data

| Weapon | Range / minimum | Reload | Burst / delay | Selected payload data |
|---|---|---:|---|---|
| `CHBattlemasterCannon` | 4c0 | 50 | Base cannon | SpreadDamage 5000 |
| `.Autoloader` | 4c0 | 50 | 3 / 5 | Same conventional payload |
| `.Nuclear_Shells` | 4c0 | 50 | Base cannon | SpreadDamage 10000, modified falloff, radiation and nuclear effect |
| `JuggernautGun` | 15c0 / 5c0 | 100 | 3 / 1 | SpreadDamage 1800, special falloff; inaccuracy 2c138 |
| `JuggernautGun.Emp` | 15c0 / 5c0 | 100 | 3 / 1 | Base damage retained; EMP range 0c768, duration 100 |
| `JuggernautGun.Firerate` | 15c0 / 5c0 | 70 | 3 / 1 | Same base payload |
| `TTankZapMK2` | 6c0 | 100 | 2 / 1 | SpreadDamage 6500 per configured shot |

Sources: `mods/ca/rules/china/weapons.yaml`, `gdi/weapons.yaml`,
`soviet/weapons.yaml` and their inherited templates in `mods/ca/weapons/`.

The Juggernaut also has `FireDelay: 33`, three muzzle offsets, an aiming-only
`JuggernautDummyAim` armament and animation/stop-to-fire behavior. These must
survive compilation; three visible barrels alone do not reproduce the unit.
Its EMP variant adds an effect instead of replacing all conventional damage.
The discussed 180 mm caliber is a design label, not a verified value in these
rules. Likewise, a smoothbore classification is not established by the
Battlemaster weapon definition.

## First component inventory (proposed IDs, not implemented)

| Component family | Initial candidates | Source responsibility / constraints |
|---|---|---|
| Chassis | `battlemaster-tracked`, `juggernaut-walker`, `heavy-tesla-tracked` | Movement type, hull geometry, base HP/mass/load, electrical base load, compatible mounts |
| Drive | `conventional-drive`, `nuclear-drive` | Mechanical kW, generator electrical limit/efficiency, mass/cost; nuclear death effect on nuclear variant |
| Armor | `heavy-armor`, `reflector-armor` | Armor target class and associated interactions; mass/cost and fixed electrical demand where applicable |
| Carrier | `battlemaster-turret`, `juggernaut-fixed-triple`, `tesla-twin-turret` | Aiming mode, mount/muzzle offsets, barrel layout, animation and visual constraints |
| Weapon | `battlemaster-cannon`, `juggernaut-artillery`, `tesla-emitter` | Compatible ammunition, range/reload, projectile behavior and fixed electrical demand |
| Ammunition / payload | `cannon-conventional`, `cannon-nuclear`, `artillery-conventional`, `artillery-emp`, `tesla-discharge` | Damage/effects and payload-specific electrical demand; energy discharge is not a physical shell |
| Equipment / mechanism | `point-defense-standard`, `autoloader` | Point defense subsystem; autoloader changes firing cycle without implying extra physical barrels |

Not every row is freely swappable. Weapon + carrier determine the final firing
cycle. Whether the autoloader belongs in a general equipment slot or a dedicated
weapon-mechanism slot remains open. Nuclear and conventional drive ratings are
unknown; the existing rules do not provide real engine specifications.

### PDL and reflector are not just numerical bonuses

Standard PDL currently includes:
- An independent `pointdefense` turret (turn speed 1000) and armament.
- `AdvancedPointLaser`: reload 5, range 2c768.
- `PointDefense` interception of Missile/Bullet types.
- Ammo pool 12; refill count 12, delay 240, reset on fire.
- EMP/disabled conditions, UI pips and range circle.

Sources: `^PointLaserDefenseSystem` in `rules/defaults.yaml` and
`AdvancedPointLaser` in `weapons/other.yaml`.

Reflector changes `Armor.Type` to `Reflector`, armor targeting metadata and
energized palettes/conditions, and inherits corrosion behavior. It is not
merely a universal damage-resistance percentage. Referenced weapon-versus-armor
behavior must remain available. Its proposed electrical demand is NEW design
metadata; it is not present in the legacy trait.

The PDL template rewrites `AttackTurreted` armament/turret lists. It cannot safely
be pasted onto a fixed frontal artillery actor without checking attack-trait
composition. Modules need compatible behavior adapters as well as mount slots.

## Artwork constraints

| Vehicle | Evidence | Initial design restriction |
|---|---|---|
| Battlemaster | `sequences/voxels.yaml`: `idle: battlemaster`, `turret: battlemastertur`; actor uses `^VoxelTurretedTank` / `WithVoxelTurret` | Hull and complete turret are separate. No separate barrel is declared; keep gun and turret visuals coupled initially. |
| Juggernaut | `sequences/gdi.yaml`: `jugg.shp` stand/run/shoot/die; actor uses facing body and `AttackFrontal`, no separate turret renderer | Lock walker, fixed mount and visible gun assembly together. Allow compatible payload changes. |
| Heavy Tesla Tank | `sequences/soviet.yaml`: hull and turret/shoot occupy separate sequences within `heavyteslatank.shp`; actor uses `WithSpriteTurret` | Hull and complete animated twin-coil turret are separate; coils remain coupled to their mount. |

Separate sequences may share a file and still be modular. Separate files alone
do not prove arbitrary interoperability. Cross-mount swaps require visual checks
of scale, facings, attachment offsets, palette, occlusion and animations.
In particular, voxel Battlemaster and sprite Tesla parts are NOT automatically
compatible just because both have separate turrets.

This inspection establishes declared rendering structure, not pixel-level asset
quality. No artwork was altered or visually certified.

## Static power model

Design-only quantities, to be calibrated:

- `MechanicalPowerKW` on the drive.
- `MaxElectricalPowerKW` and `GeneratorEfficiency` on a separate generator component.
- `ElectricalDemandKW` on chassis and applicable selected modules/payloads.
- `MassKg` on physical components (no double-counting integrated parts).

```
E = sum(selected component electrical demands)
P_remaining = MechanicalPowerKW - E / GeneratorEfficiency
```

Validate generator capacity, positive efficiency and sufficient mechanical
reserve. Exact minimum reserve remains a design decision. A zero-demand,
zero-generator drive needs an explicit supported representation, not division
by zero. Demand ownership must be explicit so the carrier and weapon do not
both charge for the same actuator.

Illustrative ONLY: 500 kW mechanical, 100 kW maximum electrical, efficiency 0.25.
10 kW base load leaves 460 kW mechanical; an additional 50 kW Tesla load leaves
260 kW; 100 kW total leaves 100 kW. These are not inferred vehicle statistics.
There is no base-grid draw, per-shot drain, recharge energy or runtime allocation.
Existing PDL ammo/cooldown mechanics are independent of this design budget.

Candidate speed curve (not approved/calibrated yet):

```
v = min(v_max, v_reference * ((P_remaining / mass) /
                             (P_reference / mass_reference)) ^ alpha)
```

`alpha = 0.5` is a starting experiment, not a fixed rule. Use chassis-level
reference values, not a new reference speed for every finished design. Keep
locomotor terrain behavior. Turret speed needs its own carrier-based mapping;
do not assume it equals movement speed or dynamically competes with movement.
Final compilation must use deterministic, specified rounding for engine values.

## Calibration conflicts we must resolve explicitly

1. Legacy PDL and reflector variants retain the base Battlemaster speed. EMP
   Juggernaut likewise retains speed 50. Adding nonzero electrical demand to
   these modules while retaining exactly the same engine, mass and an uncapped
   strictly increasing speed curve WILL reduce speed. We cannot promise exact
   legacy parity and ignore this mathematical conflict.
2. Possible explicit solutions: a genuine chassis speed cap with power headroom;
   a visible stronger-drive component in the legacy reference design; or a
   deliberately approved balance change. Do not hide compensation in actor IDs.
   PDL + reflector should still substantially consume the shared power reserve.
3. Legacy pricing is not simply additive: autoloader PDL costs +650, mass-production
   PDL costs +400 (600 -> 1000), while these reflector variants add no unit-price
   increase. Promotions also have separate research/access costs. An exact
   additive price fit is not established.
4. One speed observation cannot uniquely determine mass, engine power, efficiency
   and electrical load. These are new balancing parameters constrained by the
   reference behavior, not facts recoverable from YAML.
5. Do not double-count nuclear propulsion: either derive its speed from its drive
   rating or retain the legacy 125% upgrade as an explicit layer, not both.

## Agreed refinement: armor and commander unlocks

Existing engine armor classes and their damage interactions remain unchanged.
An installed armor component selects one existing class; armor components replace
one another rather than stacking Heavy and Reflector protection.

Armor modifiers apply ONLY to the chassis baseline, not to the completed vehicle.
Illustrative values agreed for the schema (not calibrated production values):

```yaml
reflector-armor:
  ArmorType: Reflector
  ElectricalDemandKW: 40
  ChassisMassPercent: 120
  ChassisHPPercent: 90
  ChassisCostPercent: 120
```

For a chassis of 10000 kg, 40000 HP and 500 credits, this produces 12000 kg,
36000 HP and 600 credits before other components, plus 40 kW electrical demand.
Add drive, carrier, weapon and equipment mass/cost separately AFTER applying the
chassis armor modifiers. Add chassis base electrical load and all module demands;
the armor demand is additional, not a replacement for the chassis base load.
Do not also add a standalone armor mass/cost for the same modifier contribution.

Reflector trades away Heavy protection for its existing armor matchup profile;
it is not Heavy armor plus an energy-resistance bonus. Do not replace the existing
Reflector damage table with Light or invent a new armor class.

Commander-unlocked vehicles need not match the base vehicle's power level.
Commander points unlock the COMPLETE DESIGN in the in-match commander tree, not
individual components globally. For example, a PDL + EMP design costing 2 CP must
be unlocked for 2 CP before it can be produced. Pay once per design unlock, not
per manufactured vehicle. Unlocking one design does not automatically unlock
other designs sharing its components. The design itself stays fixed; the unlock
only makes it available. Vehicle production costs remain a separate axis.

A high-efficiency generator is a proposed commander-unlocked component: improved
conversion efficiency can preserve mechanical reserve despite PDL electrical
load. Its exact efficiency, output limit, cost and unlock chain still need fitting;
no assumption that a generator alone or a universal +600 cost fits every vehicle.
Armor's conventional-protection tradeoff must also enter the calibration instead
of applying the PDL pricing model indiscriminately.

This refines the calibration conflicts above: stronger unlocked components and
armor tradeoffs are intended, visible design mechanisms, not hidden compensation.

## Agreed refinement: generator, ammunition mass and factions

The generator is a separate selectable component from the mechanical drive.
A baseline generator is always an option, has the lowest efficiency and adds no
price premium. Better generators can increase efficiency, cost and the resulting
design's CP requirement. Exact ratings remain uncalibrated. Generator selection
never bypasses mechanical power or electrical output limits.

Ammunition carries a fixed mass for the complete intended loadout, not a per-shell
mass implicitly charged only once. Add this mass to the vehicle total; it does
not decrease when firing. No new ammo-consumption, supply or power simulation is
introduced. Existing mechanics such as PDL ammunition/recharge remain independent.

EVERY component declares its allowed factions. A design's selected faction must
be allowed by every selected component, in addition to satisfying mechanical,
weapon/ammunition and artwork compatibility. Nod cannot select a GDI chassis.
Shared components, such as generators, may list multiple factions. Nuclear drives
are available only to China and Soviet. These are design-catalog restrictions,
not changes to existing armor classes or a decision to merge gameplay branches.

Illustrative schema field (not yet executable mod syntax):

```yaml
nuclear-drive:
  AllowedFactions: [china, soviet]

gdi-walker-chassis:
  AllowedFactions: [gdi]

shared-generator:
  AllowedFactions: [gdi, nod, china, soviet]
```

The shared list is illustrative, not the complete faction catalog. Base-faction permissions include their subfactions unless explicitly restricted.
Long term, players select a base faction rather than a subfaction; existing
subfactions become predefined example faction designs using the same components.
Missing faction metadata must not silently grant universal access. Validate these restrictions at match admission as well as in the designer.

## Agreed refinement: catalog budget

Catalog points are separate from in-match commander points and production credits.
The general's level determines the catalog budget (level 1 = 1 point); use level
50 = 50 points for initial development. Progression through challenges is a later
feature, not part of the initial calculator.

Every component has a catalog tier: 0, 1, 2 or 3, costing respectively 0, 1, 2 or
3 catalog points. A design's catalog cost is the sum of its selected component
tiers. Charge this sum again for EACH design admitted to the catalog, even when
components are shared between designs. There is no once-per-catalog component
purchase or discount for reuse.

```
design_catalog_cost = sum(selected component tiers)
catalog_cost = sum(design_catalog_cost for each catalog entry)
catalog_cost <= general_level
```

Example: components with tiers 0 + 1 + 2 + 0 cost 3 points per design. Two such
designs cost 6 points, even if they share most or all components. Component tiers
do not automatically set commander-point requirements; those remain a separate
design-unlock balancing axis.

Chassis have a minimum catalog tier of 1; there are no tier-0 chassis. Other
components may be tier 0. Thus every vehicle design costs at least one catalog
point, limiting a 50-point catalog to at most 50 vehicle designs without a
separate vehicle slot cap.

Commander-tree unlocks have no prerequisite chains between designs. Each design
is unlocked directly for its complete CP cost; a 2-CP design does not require a
previous 1-CP variant. Zero-CP designs are already unlocked. Normal production
building and tech prerequisites remain independent of commander unlocks.

## Agreed refinement: mounts, equipment and production

Each chassis explicitly defines its weapon-carrier slots, whether they hold a
rotating turret or a fixed gun. Display the slot count in the designer only when
it exceeds one. Slot count and interchangeability are independent: a visually
integrated fixed gun remains locked to its chassis even when represented by a
separate logical component. Compatible ammunition and other non-visual choices
can remain selectable. Separate artwork permits alternatives only after checking
compatibility. PDL is equipment, not an additional main weapon-carrier slot.

The weapon carrier owns firing behavior, including barrel arrangement, salvo
pattern and shot timing; ammunition owns impact effects. Weapon parameters such
as caliber, range and projectile flight must compose with those responsibilities
without duplicate ownership of firing-cycle values.

Most components contribute zero commander points. Special components such as
PDL, Reflector, certain ammunition or carriers can contribute one CP. Sum selected
component CP contributions for the complete design's direct unlock cost. Keep
this separate from catalog tiers. The highest component tech requirement sets
the design's minimum tech level; production costs and build time follow the
agreed cost aggregation and existing production rules.

Vehicles use the existing standard `^VehicleVision` baseline. Recon and sensor
equipment should reuse the appropriate existing Ranger/mobile-sensor behaviors,
not introduce an unrelated vision system. The exact source traits need auditing
before implementation. Chassis determine locomotion.

Equipment-slot count is explicit per chassis, defaulting to its catalog tier;
large chassis can have deliberate exceptions. Power demand, mass, price and CP
requirements remain the main balancing levers. Drive, generator, armor, primary
weapon carrier, weapon and ammunition do not occupy equipment slots. Chassis and
module compatibility/exclusion rules still apply; extra slots never authorize
physically or graphically incompatible combinations.

Harvesters and transports are not fundamentally excluded from the architecture:
- Harvester chassis retain the existing harvesting behavior with a restricted
  catalog of faction-appropriate options (stealth, gun, shield or chrono).
- A compatible chassis may take transport equipment defining capacity and
  passenger restrictions. Passenger firing is a separate capability, not an
  automatic consequence of cargo capacity.
- Deployable vehicles remain deferred pending analysis of actor/state changes.

Start calibration with the combat references already listed; supporting those
specialist roles in the architecture is not a claim that they are implemented.

## Historical calibration step: explicit component credit discounts (superseded)

The following records the earlier experiment, NOT the current pricing rule.
The global CP-value decision below supersedes all individual discount fields.

CP-gated designs may be stronger at the same production price. Components may
carry an explicit design-level credit discount, independent of their CP and
catalog-tier values. Apply discounts after chassis armor price modifiers and
component costs, once per installed component. Require a positive final price.
There is no fixed credit conversion per CP; a typical value may be inferred later
from balanced, comparable designs. Expensive zero-CP alternatives are deferred.

The first offline fixture gives EMP ammunition a 200-credit discount: Juggernaut
with efficient generator costs 2200 before discount, then 2000, with speed 50,
60000 HP and 1 CP. The same ammunition with baseline generator costs 1800 and
moves at speed 45. The discount is not secretly restricted to a named design.
Small deviations around 1% are acceptable for calibration; they need not be
eliminated with special-case corrections. This is not yet engine implementation.

Reports should compare existing vehicles against equivalent configured candidates
with old/new price, HP and speed and explicit percentage deviations. Separate
novel combinations without real legacy counterparts. Scalar matches are not
proof of matching weapons, effects, prerequisites or rendering.

## Current calibration rule: uniform CP credit value

No special rebates per component or design. Each CP contributes the same global
credit reduction: `price = total hardware cost - total CP * credits_per_cp`.
CP remains a design unlock requirement; this formula determines unit production
price. Catalog tier points remain separate. Hardware choices (including larger
drives and efficient generators) explain performance and cost differences.

The offline fixture now starts with 300 credits/CP. Autoloader, PDL, nuclear
propulsion and EMP hardware prices were retuned with this single value; the
Battlemaster defense references both select the same 550 kW diesel and 75%
generator. Reflector's early illustrative mass/HP/price modifiers are replaced
by 100% each, with 57.5 kWe load. This produces matched reference price, HP and
rounded speed for the autoloader, PDL and Reflector variants without actor-specific
corrections. The sample is small and fitted; 300 is not uniquely inferred.

See `tools/modular/calibration-report.md` for full hardware prices and residuals,
and `tools/modular/README.md` for current assumptions and problematic combinations.
Future tuning must use shared components/global CP value and display every affected
reference, not add special discounts. Engine integration remains unimplemented.

## Suggested next step

Build a small offline calibration table/calculator, before a designer UI:
- Start with the Battlemaster family, Juggernaut/EMP and Heavy Tesla Tank above.
- Record current target values separately from proposed component parameters.
- Choose shared masses/ratings and test power headroom, curves and chassis caps.
- Compare original designs and new combinations (especially PDL + reflector).
- Report discrepancies rather than silently fitting each design independently.

Then agree the price/upgrade and legacy-speed tradeoffs. Only after that implement
an executable component schema and compiler. Generated rule tests should verify
weapon cycles/effects, death conditions, armor, rendering traits and derived
movement values. Match synchronization, saved designs/replays and dependency-
aware asset loading are separate later tasks, not solved by this document.
