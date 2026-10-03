# Ground combat base-family coverage completed

The remaining **44 production-candidate ground combat families** are now bound in the native
configurator. The generated [inventory](missing-vehicles.md) has **zero wholly missing combat
base-family groups** across GDI, Nod, China, Allies, Soviets and Scrin.

This is not a claim that every national/promotion variant, aircraft, ship, support/economy vehicle
or helper actor is a configurable design. Those are still listed separately in the inventory.
In particular, the disabled `BATF` actor is not re-enabled: Battle Fortress with its required
Bunker Module is bound to the real, buildable `BATF.Bunker` package.

| Base | Native bindings | Component combinations |
|---|---:|---:|
| GDI / Eagle | 15 | 52 |
| Nod / Black Hand | 14 | 28 |
| China / Tank General | 8 | 24 |
| Allies / England | 13 | 17 |
| Soviets / Russia | 25 | 29 |
| Scrin / Traveler | 14 | 18 |
| Total | **89** | **168** |

These are **88 editorial families** because the Nod/Allied field artillery bindings share a family.
The new baseline list is `tools/modular/stock-combat-baselines.json`, exported from resolved engine
ActorInfo with `--export-combat-baselines`. It includes the full trait-name inventory for review.
`generate_stock_combat_bindings.py` produces the explicit native actor whitelist. Editing the
runtime catalog alone cannot bind new actors or arbitrary assemblies.

## Original behavior, shared components

The additional 44 bindings retain their complete original armaments directly on their parent
actor. They do not rewrite weapons into generic guns or substitute fake passenger fire. This
preserves conditional weapons, projectiles, warheads, cargo, initial passengers, ports, cloak,
deploy/transform rules, drones, beams, missile actors, disguise, shields and other special traits.
Sprite/voxel image and faction-image overrides also remain inherited rather than replaced by
an assumed actor-name image. Only the established custom name, baseline price/HP/armor/mobile
values and custom-roster production admission are emitted. Baseline price, HP, armor, speed,
body turn and locomotor match the resolved stock actor exactly.

**Original Armament (chassis-bound)** is an explicit fixed-package adapter, not a universal gun:
the stock arsenal and special hardware are priced in the chassis. It is not freely interchangeable
or a zero-cost retrofit. Shared drive, gear, mounting and stores costs are deducted from the stock
price exactly once to obtain the remaining chassis allocation. These are provisional allocations,
not independently measured masses, power requirements or economically validated retrofit prices.
The additional fixed assemblies offer only the stock parts, not speculative motor/weapon upgrades.
Their chassis development tier is uniformly one; original production technology and promotion
requirements are separate and remain intact. The shared 50-point policy is unchanged.

No per-vehicle running-gear clones were added. Existing gear is reused where its locomotor and
capacity fit. Two genuinely different profiles are added: **Heavy Wheels** (`heavywheeled`, Chrono
Tank) and **Amphibious Micro Drive** (`seal`, Mini Drone, including cell sharing and water behavior
from the stock rules). Total gear count is **16**. Walker/track capacity limits were expanded where
needed, but existing hull limits are unchanged: all **124 prior numeric combinations remain exactly
unchanged**, checked against the previous committed fixtures. The broad calibration catalog/Excel
snapshots are not changed by this native adapter.

## Battle Fortress and Bunker Module

- Chassis: **Battle Fortress**, `chassis_class: superheavy`, with **3 turret/mount slots**.
- Separate carrier component: **Bunker Module**, compatible only with `superheavy` chassis,
  requiring **all 3 slots** (`slots_required: 3`). The UI shows **3/3 occupied**. No additional
  turret fits alongside it. Capacity/occupancy is checked by both native and Python calculators;
  this does not yet enable three independently configured turrets or unreviewed graphics combinations.
- Bunker allocation: **1000 credits**; the other fitted components total **2000**, preserving the
  stock Bunker Fortress price of **3000**. This split follows the stock 2000/3000 reference difference
  but is not an independently validated generic bunker installation price.
- The explicit combination inherits `BATF.Bunker`: original bunker graphics, five initial rocket
  infantry, attacking cargo/ports, M60 and ZSU channels, loading conditions and hull behavior.
- `promotion.battle_fortress.bunker` and `!upg.batf2` remain required. The disabled base actor is not
  exposed as a buildable, module-less design.
- Class compatibility is necessary, not sufficient: other super-heavy chassis do not automatically
  acquire bunker graphics or firing ports. Each real assembly still needs a supported binding.

The class check is native validation, not merely a UI filter. Ordinary chassis reject the module.
Tests also reject a tampered Battle Fortress class even if its explicit module choice is unchanged.

## Production and representative national rosters

The custom faction retains its representative base commander tree. Explicit national admission
maps ZOCOM/Talon designs to custom GDI, German designs to custom Allies, Reaper/Harvester designs
to custom Scrin, and Iraqi/North Korean designs to custom Soviets. Vanilla rosters are not edited.
Stock factories, technology, promotion gates and negative replacement prerequisites are retained.
Consequently catalog availability does not promise every unit can immediately be manufactured:
some original promotions/structures may remain inaccessible through a representative base's tree.
They are available as frozen/preplaced test designs; unrestricted cross-national unlock planning
is separate work and is not silently bypassed here. Existing faction-wide modifiers also stay active.

## Roster limit versus catalog size

The existing **16 designs / 50 points per saved faction** limits remain. In particular the Soviet
catalog has 25 bindings, not 25 automatically added units in every saved roster. The template button
now fills available slots without throwing away successful additions when the catalog is larger.
It reports remaining types; choose a different chassis for an existing slot or create another saved
faction. Existing design IDs/names/parts are preserved. Template names are sanitized, including
apostrophes/parentheses in stock names, so labels such as Devil's Tongue and Bunker no longer make
the whole template action fail validation.

## Verification

- **92 Python / 174 native tests**, all **168** calculator combinations checked independently.
- All 44 new baselines checked against the engine-resolved originals, plus the prior 14 combat
  additions; **89** preview-only trait configurations checked headlessly.
- Every new design compiled into immutable maps, including empty map-local weapon files (stock
  weapons are inherited). Soviet additions are tested in two batches respecting the 16-design limit.
- Differential lint, all against the identical map with stock actors:
  - GDI: **1685 / 1689**, 4 inherited additions.
  - Allies: **1685 / 1706**, 21 inherited additions.
  - Scrin: **1685 / 1695**, 10 inherited additions.
  - Soviet batches: **1685 / 1712** and **1685 / 1694**, 27 and 9 inherited additions.
  - **Zero unexpected diagnostics** in all five batches.
  - Reports: `tools/modular/designer-stock-*-validation.md`.

Inherited full-mod lint errors remain. These checks are not a live combat, cargo, transformation,
production or GPU rendering pass for all newly added units. The earlier viewer was visually confirmed
by the user; the expanded vehicle set still needs live testing. No release/push is implied.
