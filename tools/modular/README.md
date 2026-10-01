# Modular vehicle tools

## Missing-vehicle inventory

See **`docs/modular/missing-vehicles.md`** for the complete current gap list: combat versus
support/economy/transport, unbound variants of supported families, and aircraft/naval appendices.
Regenerate with `python tools/modular/list_missing_vehicles.py`. Counts use native bindings,
not spreadsheet coverage; roles and production candidacy remain explicitly editorial.

## Latest: native main-menu faction editor

**Eigene Fraktion** supports **GDI/Eagle, Nod/Black Hand, China/Tank-General, Allies/England, Soviets/Russia and Scrin/Traveler-59**, plus **up to 16 custom
vehicle designs**, with New/Copy/Remove, shared budget, named faction save/load and
in-memory migration of existing single-tank profiles. The base selector saves the
previous faction in the library before starting an independent roster with a free name.
**Vorlagen** adds missing families for that base: **eight GDI families** (tank, MG scout,
MLRS, Mammoth, Juggernaut, native Hover MLRS, Disruptor, Mammoth Mk II), **four Nod
families** (light tank, Buggy, artillery, SSM) and **four China families** (Battlemaster,
Dragon, Gatling tank, Overlord), plus **four Allied bindings** (Challenger, Ranger,
field artillery, Prism tank) and **four Soviet bindings** (Heavy Tank, T-34, heavy Tesla,
Mobile Flak), plus **four Scrin bindings** (Gun Walker, Seeker, Corrupter, Devourer).
That is **28 faction-specific bindings / 27 families**:
Nod and Allied field artillery share a family. Their **90 supported combinations** use compatible
artwork and full existing weapons, including helper, visual and conditional upgrade channels.
Hum-Vee/Buggy mandatory sensors are counted once. Mixed-faction and non-GDI stationary
profiles fail native validation. Both test-map player slots use the selected base.

**Speichern und Testspiel** freezes the complete roster into a separate user map.
Each design is preplaced near start A; moving designs are additional named factory
choices. Stationary remains preplaced/Carryall-only. No Python needed at runtime,
no mutation of ongoing games, no overwriting immutable map snapshots. This is not
yet all 75 components, arbitrary map integration, or per-player faction syncing.

Read **`docs/modular/main-menu-designer.md`** for workflow, bindings and limitations.
**76 Python + 86 native tests pass**; all 90 numeric combinations match the independent
Python calculator. Release build succeeds. The eight-family engine differential
is **1685 control / 1698 generated errors**, all 13 additions inherited diagnostics,
**zero unexpected new errors**; see `designer-gdi-validation.md`. The Nod differential
is **1685 / 1691**, six inherited diagnostics, **zero unexpected new errors**; see
`designer-nod-validation.md`. China: **1685 / 1693**, eight inherited diagnostics,
**zero unexpected new errors**; see `designer-china-validation.md`. Allies: **1685 / 1692**,
seven inherited diagnostics, **zero unexpected new errors**; see `designer-allies-validation.md`.
Soviets: **1685 / 1693**, eight inherited diagnostics, **zero unexpected new errors**;
see `designer-soviet-validation.md`. Scrin: **1685 / 1689**, four inherited diagnostics,
**zero unexpected new errors**; see `designer-scrin-validation.md`. This is NOT a
clean full-mod lint or a live gameplay pass. Expanded UI/gameplay checks remain pending.

Complex families use template-locked weapon packages: no fake SpreadDamage
on a dummy aim weapon, no erased beam behavior, no colliding upgrade-channel names.
Hover MLRS, Mk II and SSM keep their special production unlock requirements.
SSM preserves ammo pools, reloading and missile graphics; Nod artillery uses the full
155mmTD package and original death explosion. Efficient generators are admitted for Nod
at the same shared price/performance. Broad calibration JSON and Excel files are untouched.
Dragon retains Black Napalm and both Firewall phases; Gatling keeps all eight ground/AA
channels and spin-up states. Battlemaster retains Horde/nuclear upgrades. Overlord inherits
Emperor art and 80% received damage for Tank General; shown HP/speed remain raw base values.
Conditional Emperor tooltips preserve custom names. China factory/radar requirements and
promotion exclusions remain; roof attachments and PDL variants are not enabled.
England preserves national Challenger art/90mm, Ranger M60mg/sensors and doctrine exclusions,
155mm artillery/death explosion, and the complete Prism beam/cluster package. Existing 85%
Armored Doctrine production discounts remain; displayed prices are raw base prices. Prism
keeps stock Light armor/wheeled movement and radar/promotion requirements. Shared ammunition
export now unions compatibility, preserving existing GDI HE profiles without granting HE to
Challenger. Russia adds full twin-cannon cadence, T-34 normal/cluster channels, Tesla
animation/delay/upgrades and independent Flak ground/AA packages. Production prerequisites
and promotion exclusions remain. The custom Soviet roster admits the North Korean T-34
without editing vanilla national rosters. Tesla Arc and other promoted variants remain open.
Scrin preserves Gun Walker ground/AA, Corrupter spew/animation/death explosion, Seeker
plasma discs and Devourer's full laser sweep/floating turret. Native light-hover behavior,
water/EMP risks, original turn rates, Light armor and production prerequisites remain.
Explicit Scrin drive/converter adapters use experimental shared physical ratings, not
claimed diesel technology or in-match electrical simulation. Human/Scrin converter admission
is enforced natively. All six base groups now have representatives, but further families,
complete faction rosters and the other subfactions are not integrated yet.

The confirmed fix preventing ordinary skirmish restoration from replacing the chosen
snapshot remains active. Test sessions also do not overwrite ordinary skirmish setups.
The following sections document earlier stages; the current entry point is the editor.

## Earlier: first in-game map prototype

**`mods/ca/maps/modular-gdi-lab.oramap`** contains three generated GDI actors:
normal tank (900 credits), hover test (1100) and stationary platform (850).
A manual Carryall, original MTNK and enemy target are placed alongside them.
Read **`docs/modular/playable-prototype.md`** for launch/test instructions and limits.

`compile_prototype.py` is an OFFLINE compiler for a strict component subset, not
an automatic match-load JSON hook or a general trait composer. It emits normal
map rules, using full MTNK/120mm templates and the existing hover/transport traits.
Unsupported equipment, cargo, secondary mounts and CP unlocks fail closed.
The new mobility parts live in `prototype.json`; the broad catalog and old Excel
snapshots are unchanged. No factory production of the stationary actor yet.

```sh
python tools/modular/compile_prototype.py --output mods/ca/maps/modular-gdi-lab-v2.oramap
python tools/modular/check_prototype.py --map mods/ca/maps/modular-gdi-lab-v2.oramap
```

Use a new output path: existing maps are never overwritten. Both special vehicles
still use tracked tank placeholder artwork. **61 tests pass**; engine map/rule
lint ran, but is NOT globally clean. Differential errors only repeat the inherited
stock MTNK diagnostics; see `prototype-validation.md`. Interactive play and
Carryall/hover tests remain pending. Historical sections below describe earlier
steps and their then-pending mobility work.


## Latest: families instead of raw actor counts

Open **`vehicle-calibration-families.xlsx`** (13 sheets), starting with
**`FahrzeugFamilien`**, not the raw `FahrzeugInventar` appendix.

The current working grouping contains **108 provisional vehicle families** with
at least one local production declaration, plus **one prototype-only/review
family** (SFTNK). This is not engine-validated availability. The 388 raw blocks are
still available for auditing, but are no longer presented as a vehicle count.

**15 families** contain at least one scalar reference design. That does NOT mean
all variants, special abilities or artwork in those families have been modeled.
Individual covered/unmodeled actor IDs are shown. The 60 design examples remain
unchanged, including experimental combinations without legacy equivalents.

`vehicle-family-policy.json` records editorial grouping exceptions. Named Ranger
variants and national Medium Tank variants are grouped; the Heavy Tesla remains
separate from the light Tesla. Nona/BTR and Howitzer/Specter are not automatically
merged just because their rules share inheritance. Longest dotted anchors handle
ordinary upgrade branches. Unknown ambiguous roots are flagged for review.

The raw appendix separates 7 wrecks, 9 projectile actors, 1 attached helper,
1 unresolved actor, 4 locally disabled prototypes and 8 non-production forms.
A removed Buildable trait is NOT proof that a spawned/transform form is obsolete.
Disabled BATF/2TNK parents do not hide their relevant production variants.

Family base prices/HP/speed are local anchor values only; blanks mean unresolved,
not zero. Faction names identify source files, not resolved production allowlists.
The checked-in policy and reports are investigation tools, not a new rules loader.

Hover hardware and stationary Carryall platforms remain the next design task;
this change only clarifies vehicle coverage and does not implement either.


## Latest: running gear and cross-faction vehicle sweep

Open **`vehicle-calibration-running-gear.xlsx`** (12 sheets). Added explicit
running-gear selection in `Entwuerfe` AQ, resolved locomotor/body turn rate in
AR/AS, `Fahrprofile` terrain/crush metadata, and `FahrzeugInventar` coverage.
See `docs/modular/running-gear-and-coverage.md` for the design contract and next
vehicle families. Older sections/workbooks below remain historical snapshots.

- Rumpf, Fahrwerk and Motor are separate. Default gear is selected from the hull;
  `design.running_gear` can explicitly override it only if compatible.
- Existing hardware allocations were moved out of hulls, not charged twice.
  All **48** previous examples retain their scalar results (including the invalid
  generator example staying invalid). Regression fixture basis: `06f6fa36`.
- Added **12** reference rows: Hum-Vee, Buggy, Nod Light Tank, T-34, Devil Tank,
  GDI Battle Tank, Allied Medium Tank parent + Challenger/Leclerc/Leopard,
  Soviet Heavy Tank and Allied Artillery. Shared MG/sensor, light cannon and
  medium-cannon families. No individual rebates; global CP value unchanged.
- New base prices/HP/speeds match by initial calibration of hull values, NOT an
  independent balancing proof. The three Allied skins share the same parent;
  2TNK itself is disabled. Special abilities/upgrades are not compiled yet.
- The automated inventory covers six faction vehicle files, including helpers,
  variants and disabled prototypes. Local fields only: blank is unresolved, not
  zero. It does not claim all those vehicles are modeled or even buildable.
- Wheels/tracks/legs reference actual existing engine locomotors; no new terrain
  movement rules. Juggernaut's legs still use sheavytracked. ARTY keeps Light
  armor and lighttracked (no infantry crush). Hover and generic cargo are next
  capabilities, not silently substituted components.

Regenerate the coverage report with:

```sh
python tools/modular/survey_vehicles.py --output tools/modular/vehicle-coverage.md
```


## Latest: artillery sprite as experimental mortar carrier

Open **`vehicle-calibration-mortar-drafts.xlsx`**. The separated `artytur.shp`
identified from Battle Fortress now backs an explicitly experimental roof-carrier
concept. It is not promoted to the normal graphics-compatible allowlist.

Three NEW designs (no legacy target) retain the heavy main gun and add:
- Light artillery carrier: proposed 200 credits / 400 kg / 2 kWe, no CP.
- Vehicle mortar: proposed 350 credits / 600 kg / 3 kWe, no CP.
- HE loadout: proposed 100 credits / 300 kg, no CP.

Thus the additional package costs **650 credits**, not a price inferred from the
150-credit infantry mortar. Range 11 cells, minimum 1, reload 60 and raw damage
5500 come from the existing `Mortar`/`MortarPrototype` rules. A 15-tick fire delay
from the infantry actor is retained as an initial carrier proposal, not a proven
requirement of the artillery sprite. No invented caliber is assigned.

| Experimental design | Price | CP | Speed |
|---|---:|---:|---:|
| Overlord + mortar roof | 2650 | 0 | 54 |
| + PDL | 3300 | 1 | 55 |
| + Reflector | 2650 | 1 | 55 |

All retain 95000 base HP. Existing main cannon, protection, generator and motor
parameters are unchanged. These costs do not prove that long-range artillery on
an Overlord is balanced; they are an explicit first package proposal. The draft
flag still checks the roof slot, faction, weapon and payload compatibility.

The report and Excel now show the auxiliary weapon's range/minimum range, reload,
burst and raw damage separately from the retained main gun. These are template
fields, NOT DPS or simulated projectile behavior. Existing Gatling spin-up/AA
variants still use their real templates later; its simple displayed row is cold
ground fire. Neither auxiliary weapon changes the reported main-gun values.

Actual sprite offset/scale, muzzle origin, aim/attack composition and runtime
behavior still need an in-game prototype. No game YAML, module permissions in
production, or engine traits were changed. Earlier notes about not yet finding
a mortar graphic describe the pre-Battle-Fortress inspection only.


## Latest: flame-roof drafts and asset inspection

Latest workbook: **`vehicle-calibration-roof-drafts.xlsx`**. Three flame-roof
candidates reuse existing Dragon carrier/weapon/fuel components. They appear as
explicit `/ draft` combinations WITHOUT legacy targets, not as tested game units.
See `docs/modular/roof-attachment-candidates.md` and `roof-sprite-preview.png` for
the asset evidence, engine scale limitation and remaining mount checks.

The sprite alternative `chdragontur.shp` exists separately with 32 facings; raw
frames were actually exported and inspected. No Overlord in-game composite has
been tested. The calculator requires both a chassis experimental allowlist AND
`experimental_graphics: true`. This does not bypass occupied slots or weapon
compatibility. The mortar weapon templates exist, but a suitable separated mortar
turret has not yet been identified in the inspected China assets.


## Latest: shared roof slot and selectable starting infantry

Open **`vehicle-calibration-loadouts.xlsx`** (10 sheets). Older workbook names and
loaded-bunker representations below are historical snapshots, superseded here.

Overlord now exposes one main carrier and one optional auxiliary carrier, sharing
its roof attachment point with bunker and speaker equipment. `auxiliary_mount`
selects carrier, weapon and ammunition separately; their prices, mass, power,
tech, tiers and CP contributions are counted alongside the main assembly. Main
weapon results are NOT overwritten by the secondary weapon. The approved roof
carrier currently is `gatling-roof-carrier`, with existing gatling weapon/ammo.
A carrier count of two is a maximum; it does not imply two guns when a bunker or
speaker occupies the roof. Python rejects conflicting roof use, wrong payloads
and unapproved auxiliary mounts. PDL is independent equipment, not a roof mount.

The new Gatling assembly references `^ChinaGatlingOverlord` and `^SensorEquipment`
for its full legacy behavior. The 50-credit carrier +150 weapon +100 loadout is a
provisional base price allocation, offset by 1 CP; it is not a measured value of
its sensor capability. Legacy/source actor generation is still future work.
Gatling PDL computes 2650 versus legacy 2800, deliberately leaving -150 residual.
The source actor's mixed sprite/voxel rendering and armament names must be bound
properly by a future compiler; arithmetic tests do not certify rendering.

`firing-bunker` is now EMPTY: 100 credits, 600 kg, 5 kWe and capacity 4. It carries
no fixed `included_units`. Each design owns an optional `crew` list of actor/count
pairs. Infantry catalog prices: Tank Hunter 300, Red Guard 80, Minigunner 200;
100 kg per passenger package is still hypothetical. Original bunker designs
select four Tank Hunters explicitly, preserving their existing computed totals.

Examples using the same base bunker vehicle:
- Empty: 1800 credits, 1 CP.
- Two Minigunners: 2200 credits, 1 CP; two capacity points unused.
- Two Tank Hunters + two Red Guards: 2560 credits, 1 CP.
- Four Tank Hunters: 3000 credits, 1 CP.

Crew cost/mass is added once, NOT also stored in the module price. Capacity uses
transport weights. Python rejects noninteger/negative counts, overcapacity, crew
without a bunker, duplicate type entries and faction-incompatible infantry. Crew
tech requirements contribute to the design maximum. This initial catalog uses
standard infantry with no additional CP/tier contribution; rules for custom
CP-gated infantry are not established. Faction policy is the agreed base China
catalog, rather than preserving all historical general restrictions.

Workbook sheets `Infanterie` and `Startbesatzung` are formula-linked to designs.
Change infantry price/mass or existing crew rows to recalculate costs/weight.
A zero Excel count disables a row; JSON omits that entry instead. New rows/designs
should be added in JSON and exported to a new workbook, not assumed to extend
fixed formula ranges. `Entwuerfe` includes separate auxiliary selections plus crew
cost/mass columns. Excel only checks scalar limits; full roof/cargo/faction and
compatibility validation stays in Python. Formula round-trip checks do not execute
Excel calculations. Empty/partial/mixed examples have no fabricated legacy target.

Next candidates: flame or mortar roof carriers. They are deliberately not enabled
until mount geometry/artwork and behavior compatibility are verified. Existing
Dragon flame behavior can be reused; that alone does not prove its complete turret
fits this mount. An empty/filled transport remains an equipment module and uses
existing open-topped/passenger traits, not a new combat simulation.


## Latest: Bunker Overlord and included infantry

Latest workbook: **`vehicle-calibration-bunker.xlsx`**. Older names below are
preserved historical snapshots. Three configured bunker references are added:

| Variant | Legacy / configured price | Legacy / configured speed |
|---|---|---|
| Bunker | 3000 / 3000 | 56 / 55 |
| Bunker + PDL | 3600 / 3650 | 56 / 55 |
| Bunker + Reflector | 3000 / 3000 | 56 / 55 |

All retain 95000 base HP; defense designs cost 2 CP total, base bunker 1 CP.
This uses the same heavy engines, generators, armor and PDL as the other heavy
references, with no new price correction. Bunker costs 1300 gross: **100 empty
hardware + four Tank Hunters at 300 each**. Then the shared 300-credit CP deduction
makes the complete vehicle cost 3000. CHE3's 300-credit standalone price is sourced;
the 100-credit empty-module price is an inferred trial, not an independently
established market value. Do not conclude a large physical bunker inherently
costs only 100 credits.

Module mass 1000 kg includes a proposed empty mass of 600 plus four 100 kg
passenger packages. Load 5 kWe and these masses remain experimental. Passenger
transport weight is 1 per infantry (engine default), fitting capacity 4. No new
runtime passenger mass behavior is introduced: this is a fixed initial design
loadout for the offline calculator.

Gross module cost/mass already include infantry. `included_units`, `empty_cost`
and `empty_mass` expose that breakdown and Python validates it to prevent double
counting. Editing troop price/count requires updating gross module totals too;
Excel records the breakdown but does not validate its consistency. Manufacturing
currently scales the whole loaded package; no manufacturing bunker reference is
added and personnel pricing under manufacturing is still provisional.

A physical cargo slot does not cost another main turret slot. The loaded bunker
is equipment, with references to `^AttackingCargoAndTurret` and, when PDL is
selected, `^AttackingCargoAndPDL`. Existing traits handle mounted passenger fire.
No new attack implementation is written. Source turretbunker geometry must be
bound during compilation. Bunker and speaker are provisionally mutually exclusive
because there is no verified combined visual; this is not a new gameplay nerf.
The new calculator validation does not prove runtime trait composition, pathing,
loading/unloading or artwork alignment. Gatling's second carrier is still next.


## Latest: configured heavy vehicles

Open **`vehicle-calibration-heavy-designs.xlsx`**. It contains seven new configured
Overlord references, not just the legacy-only `SchwereReferenzen` sheet. All older
workbook names below refer to preserved snapshots.

New proposed components: heavy tracked chassis, 1000/1045 kW heavy diesels,
heavy twin carrier, heavy cannon, conventional/nuclear heavy loadouts and a
propaganda speaker. Player-facing names are recorded separately from stable IDs.
Existing PDL, Reflector, generators, CP credit value and lighter vehicles were
NOT retuned. Heavy chassis mass, power and price allocation are fitted proposals,
not physical facts inferred from the art or a general size-scaling law.

| Reference | Legacy / configured price | Legacy / configured speed |
|---|---|---|
| Overlord | 2000 / 2000 | 56 / 56 |
| Nuclear shells | 2500 / 2500 | 56 / 56 |
| Nuclear shells + PDL | 3000 / 3150 | 56 / 56 |
| Nuclear shells + Reflector | 2500 / 2500 | 56 / 56 |
| Propaganda | 2200 / 2200 | 56 / 53 |
| Propaganda + PDL | 2800 / 2850 | 56 / 55 |
| Propaganda + Reflector | 2200 / 2200 | 56 / 55 |

All use 95000 base HP; Emperor's conditional received-damage modifier and nuclear
propulsion/isotope upgrades are excluded. Base Overlord tech2 is a prototype
mapping of the radar prerequisite. Upgraded references require tech3. Nuclear
payload changes range from 5 to 5.5 cells via an explicit ammunition override,
consistent with composing projectile/weapon data rather than only a warhead.
Its existing complete nuclear effect remains a source binding for the future
compiler; the calculator does not execute damage or radiation effects.

The speaker's 500-credit/500-kg/20-kWe proposal fits its base price after 1 CP,
but shows a mobility deficit. Preserve the existing `^PropagandaSpeaker` behavior
when compiling. Its aura is not being reimplemented. No hidden HP/speed bonus or
price correction hides the current 150-credit nuclear-PDL and 50-credit
speaker-PDL residuals. These are base fits plus reuse tests, not held-out evidence
that the entire catalog is balanced.

Gatling Overlord and Bunker Overlord remain legacy-only entries: implementing
multiple carriers and prefilled firing cargo is a separate composition step, not
something this single-carrier calculator silently claims to cover. The joint
`Preisfit` sheets still fit Battlemaster prices only.


## Current correction: Type 59 and manufacturing module

Latest workbook: **`vehicle-calibration-manufacturing.xlsx`**. Older workbook names
and historical results below are retained for context, not the current selection.
`battlemaster` is the stable technical ID; its display name is **Type 59 Chassis**.
The duplicated `battlemaster-mass-produced` chassis has been removed. All three
mass-production designs now use the same chassis plus `mass-production`.

Manufacturing is one optional dedicated design selection, not physical equipment
consuming the PDL slot. This prototype choice keeps manufacturing separate from
vehicle-mounted hardware. The module costs 1 CP, adds no mass/price and currently
uses **95% hardware cost** as an explicitly provisional trial, NOT an approved
final value or the optimum of the separate joint-fit study.

`final price = raw hardware * manufacturing factor - CP * global credit value`.
Armor chassis modifiers are applied before this hardware-wide factor. The global
CP deduction is never scaled. Python, report and Excel use the same rule. Prices
remain unrounded decimals in the research calculator; game credit rounding is
not yet decided. At the current fixed hardware prices, mass-production base/PDL/
Reflector cost **602.5 / 1205 / 587.5**, against legacy **600 / 1000 / 600**. These
residuals replace the earlier cheaper-chassis results; no special rebate is added.

Existing weapon behavior is reused, not reimplemented: see
`docs/modular/ammunition-components.md` for projectile/impact payload ownership,
Säure/Korrosion, Toxin/DriverPoisonDamage and safe armament composition. New payload
concepts are not assigned invented prices or advertised as compiled actor rules.


Run from Game (Python standard library only):

```sh
python tools/modular/calibrate.py
python -m unittest discover -s tools/modular -v
```

`catalog.json` is an experimental research fixture, NOT engine YAML or the final
saved-design format. No gameplay rules or artwork are modified. Legacy targets
were transcribed at `7f5aeadf` and their selected source blocks have now been
compared against integrated commit `9d8a9dec`; see
`docs/modular/vehicle-reference-study.md`. All mass, power, prices and catalog
tiers are proposals. Matching fitted targets does not establish general balance.

## Excel workbook

Open **`tools/modular/vehicle-calibration-overlord-study.xlsx`** in Excel.
The `SchwereReferenzen` sheet contains original Overlord-family prices only, not
yet configured designs. See `docs/modular/overlord-study.md` for behavior and
naming proposals. The previous `vehicle-calibration-china-expanded.xlsx` is retained.
This snapshot adds Dragon/Gatling families; `vehicle-calibration-integrated.xlsx`
is retained as the earlier integrated-state snapshot.
The original `vehicle-calibration.xlsx` is retained as the old, pre-merge snapshot. Eight sheets provide
instructions, editable parameters, all components, design selections/calculations,
legacy-vs-new comparison, sampled price fits and every fit residual. Excel tables
have filters; fixed headers and component-ID dropdowns aid navigation. Hardware,
CP discount and final unit prices are shown separately.

To export a new snapshot:

```sh
python -m pip install -r tools/modular/requirements.txt
python tools/modular/export_excel.py --output tools/modular/vehicle-calibration-next.xlsx
```

`catalog.json` remains the source of truth. Change it to add components/designs,
then regenerate to a NEW filename. Existing workbooks are never overwritten.
Excel edits are what-if scenarios, not imported back to JSON. Formulas recalculate
on opening in Excel; openpyxl does not evaluate them. Python price/speed snapshot
columns remain static for immediate comparison. The sampled price-fit sheets are
also static and not linked to edited Excel parameters. The catalog budget entry
is informational, not a sum across the alternative/mixed-faction study designs.
Only electrical/mass/price limits are checked in workbook formulas; use Python
for faction, slot and compatibility validation. Round-trip tests check structure
and formula links, not Excel-calculated values. Missing spreadsheet formula
caches are expected until Excel or another compatible spreadsheet app recalculates.

## Integrated reference audit

Game merge `9d8a9dec` combines modular/isometric with tournament-bot; engine
`484afea27c` combines isometric-building-anchor and cameo-engine. Master was not
additionally merged. Local conyard.png artwork remains unchanged by this work.

`python tools/modular/audit_reference_changes.py --output tools/modular/reference-audit.md`
compares 42 selected actor, weapon, promotion and shared-trait source blocks from
`7f5aeadf` to `9d8a9dec`. One changed: `^AtomicTank` now grants isotope stability a
90% received-damage multiplier in addition to suppressing the nuclear death effect.
Current base-state price/HP/speed targets stay unchanged. Damage resistance is NOT
converted to extra HP, nor silently applied to every nuclear-drive design.

This reproducible audit compares source text, not complete resolved MiniYaml
inheritance or all weapon matchups. Repeated root definitions are preserved for
comparison. It does not certify full gameplay equivalence. The workbook labels
its reference conditions explicitly; hypothetical fit parameters remain separate.

## Historical branch freshness / merge preview (before integration)

Remote refs fetched during this workbook task:
- `origin/master`: `7628f569`, does NOT contain release `v0.96.20` (`09474add`).
- `origin/tournament-bot`: `98769f53`, DOES contain that release and its newer balance.
- `modular`: `7f5aeadf`, current legacy source for this study.

`git merge-tree --write-tree` previews only: master merges cleanly; tournament-bot
has a content conflict in `mod.config`. No working-tree merge was executed.
For current release balance, tournament-bot is the relevant source, not master
alone. Resolve engine revision choice with the isometric engine work rather than
blindly selecting either version. After integration, re-audit reference targets
and regenerate reports/workbook from the new source commit. The local conyard.png
change was left untouched. The workbook remains explicitly labeled with its OLD
reference basis until that audit occurs.

## Dragon and Gatling reference families

Six designs now cover Dragon and Gatling bases, PDL and Reflector variants.
Legacy Dragon prices are 600 / 1200 / 600, HP 28000, speed 103; Gatling prices
are 800 / 1400 / 800, HP 30000, speed 108. Both defenses require one CP and
tech3. Source actor, weapon, promotion and gatling-state blocks are added to the
reproducible audit (now 65 selected blocks).

New chassis/carrier/weapon/loadout splits are fitted ONLY to the base vehicles.
Their masses are hypothetical, not recovered engine specifications. Existing
PDL/Reflector, drive/generator and global CP parameters remain unchanged. Each
upgraded design uses the same larger diesel and efficient generator already used
by Battlemaster. Result: both base and Reflector prices match; PDL comes out 1250
and 1450, **50 credits above** the respective legacy prices. HP and rounded speeds
match all six. These residuals are not repaired with a family-specific rebate.
The price-increment table now exposes +650 (Battlemaster), +600 (Dragon/Gatling)
and +400 (mass-produced Battlemaster) legacy PDL increments.

This is a scalar comparison, not a complete reconstruction:
- Dragon's regular flamer is burst 8, interval 4, reload 45, range 5; damage 2500
  is only one warhead. Firewall deploy cycles, Black Napalm, fire clusters, heat
  and garrison damage are recorded but not executed by the calculator. Its
  deploy capability does not expand the first implementation scope implicitly.
- Gatling uses separate ground/AA spin-up states and reload stages 12/8/4/2.
  Ground range/damage are 6/350, air 9.5/660; the simple weapon columns show only
  a cold ground representative, NOT total DPS or the complete weapon system.
- Gatling's legacy 6-cell sight override differs from the proposed generic
  vehicle-vision policy; this remains an explicit unresolved behavior detail.
- Gatling defense promotions were Tank-General-only. The hypothetical catalog
  follows the agreed China base-faction availability; actual game rules unchanged.

Excel includes per-design reference notes and component behavior notes. The
`Preisfit` sheets STILL fit only the Battlemaster family; no silent extension of
that mathematical model to the new chassis has occurred.

## One global CP value

```
hardware = chassis_cost * armor_cost_percent / 100 + other_component_costs
CP = sum(component CP contributions)
production_cost = hardware * manufacturing_factor - CP * credits_per_cp
```

The initial global value is **300 credits per CP**, not a measured universal fact.
No module-specific or design-specific discounts are supported; the old
`design_credit_discount` field is rejected. Final cost must be positive. Catalog
points, tech and production credits remain independent axes. CP is paid once to
unlock the design; credits are paid per manufactured vehicle.

## Shared component fit

| Hardware | Previous proposal | Current proposal |
|---|---|---|
| Baseline diesel | 500 kW / 100 credits | unchanged |
| Larger diesel | absent | 550 kW / 200 credits, same 600 kg package mass |
| Efficient generator | 75% / 200 credits | unchanged |
| Autoloader carrier | 100 credits | 400 credits, 1 CP |
| PDL | 450 credits / 20 kWe | 650 credits / 47 kWe, 1 CP |
| Reflector armor | 120% mass, 90% HP, 120% chassis price, 40 kWe | 100% mass/HP/chassis price, 57.5 kWe, 1 CP |
| EMP loadout | 200 credits | 300 credits, 1 CP |
| Nuclear drive | 100 credits | 400 credits, 1 CP; power unchanged |

Both defense variants use the SAME larger diesel and efficient generator. Armor
class behavior is unchanged: Reflector replaces Heavy, not an extra universal
resistance bonus. Neutral armor modifiers supersede the early illustrative
percentages. All alternate designs automatically use these new shared values.
The larger drive is currently admitted by the Battlemaster chassis; broader
chassis compatibility has not been certified.

Example full prices:
- Autoloader: 1250 hardware - 1 x 300 = **950**.
- Autoloader + PDL: 2200 hardware - 2 x 300 = **1600**.
- Autoloader + Reflector: 1550 hardware - 2 x 300 = **950**.
- EMP Juggernaut + efficient generator: 2300 - 300 = **2000**.

The report includes each hardware contribution and old/new scalar deviations.
Both defense references now round to speed 100 and preserve 40000 HP. Autoloader
retains burst 3, shot interval 5 and reload 50; the shared cannon/ammunition retain
range 4 cells and raw damage 5000. Nuclear drive's 758.75 kW remains an algebraic
fit to 125% speed, not historical engine data.

These ratings fit a small sample; 300 credits/CP is not uniquely determined.
The successful fit is NOT proof that every future combination is balanced.
Examples still needing evaluation: baseline-generator EMP Juggernaut costs 1800
and moves at 45; PDL + Reflector exceeds the baseline generator's 100 kW output.
With the efficient generator and smaller diesel it costs 1200 and moves at 86.
This is not the earlier near-immobility goal, and the global discount makes such
2-CP combinations cheaper. Keep this visible rather than adding special rebates.

## Extended check: nuclear shells and mass production

Six additional references use UNCHANGED diesel/generator/PDL/Reflector prices and
ratings and the same 300 credits/CP. Two new provisional components are needed:

- Nuclear shell loadout: 350 credits (normal loadout 50), 1 CP, same 400 kg mass,
  no assumed electric load. The +300 hardware cost fits the base nuclear-shell
  variant; its PDL/Reflector combinations then reuse the existing defense package.
  Nuclear ammunition does not select a nuclear engine. Damage 10000 is only the
  raw warhead value; radiation, spread, falloff, recoil and effects still need
  composition in an eventual engine compiler.
- Mass-produced Battlemaster chassis: 450 rather than 500 credits, 1 CP, same
  physical ratings and graphics. This is a proposed manufacturing variant, not
  evidence of a different legacy hull. The 50-credit hardware saving plus global
  300-credit CP reduction fits the 600-credit base target. It does NOT give other
  installed components an extra discount. Modeling manufacturing as a chassis
  variant is provisional; a general manufacturing system is not implemented.

This fits five of the six added scalar targets. The exception is mass-production
PDL: **1850 hardware - 600 CP = 1250 credits**, but the existing unit costs **1000**.
HP 40000, rounded speed 100 and 2 CP match. The +250-credit residual is retained.

The key constraint is independent of the global CP value: the SAME PDL package
adds +650 in the autoloader/nuclear families but only +400 in the legacy mass-
production family. With identical motor/generator/PDL additions and one added CP,
an additive system cannot reproduce both increments. Changing the shared CP
value moves both increments together; a chassis-only discount cancels when
comparing each family to its own base.

We have NOT added a -250 special rebate, changed the old vehicle, or created a
cheaper identical PDL. Options for later discussion are a genuinely different
hardware package, a broadly justified manufacturing-cost model, or accepting a
legacy price mismatch. None is approved or implemented by this investigation.
A smaller fitted component catalog is not a unique solution or independent proof
of hardware value: both new base components were calibrated, while the defense
combinations exercise shared prices without additional fitting.

Legacy sources: `china/vehicles.yaml` for the six actors, `china/weapons.yaml` for
`CHBattlemasterCannon.Nuclear_Shells`, and `china/commander-tree.yaml` for promotion
chains. Nuclear shells are originally Nuke-General-only and mass production is
Tank-General-only; the proposed designer uses their China base-faction catalog
as agreed. This does not modify current in-game faction access. Single-shot burst
interval 0 in the report means not applicable, not the engine's literal default.

## Joint manufacturing/CP price study

Run `python tools/modular/fit_prices.py --output tools/modular/price-fit-report.md`.
This separate experiment leaves `catalog.json` and gameplay untouched. It replaces
the cheap-chassis assumption with one percentage factor applied to all hardware
of mass-production designs, BEFORE the same CP deduction used by all designs.

It samples CP credit values 0/100/200/300/400/600/1000/1500/2000 and manufacturing
factors 0.001..1.000, jointly fitting three nonnegative package premiums against
nine reference prices. Autoloader/nuclear upgrade premiums are shared because
their price and CP targets are identical. Defense premiums include all required
motor/generator changes; individual allocations remain underdetermined. The fit
minimizes equal-weight squared credit error, not percentage error or combat power.
Tests recover synthetic known parameters and verify real residuals are retained.

At 300 credits/CP the best sampled factor is 0.893 (10.7% hardware saving), with
RMSE about 40 credits and maximum deviation about 68. The mathematical best among
sampled CP values is 0, but that is NOT a recommendation to remove CP value: it
assigns almost no price premium to major upgrades and cannot necessarily fund
current motor/generator costs. This exposes the limitations of fitting only
observed unit prices.

A perfect positive-CP fit is structurally impossible under identical defense
packages: regular Reflector's zero price increment implies Q=R, while mass-
produced Reflector's zero increment implies f*Q=R. Positive R then forces f=1,
which cannot make PDL cost both +650 and +400. Even R=0 has slightly inconsistent
base/PDL production ratios. Full candidate residuals and equations are in the
report; no exceptions or special rebates were introduced.

This experiment assumes no separate manufacturing hardware fee. Therefore CP
values >=950 make the mass-produced base price nonpositive and are reported as
infeasible under THIS assumption, not ruled out universally. Allowing additional
shared manufacturing hardware cost or different actual packages would be a new
model, not a result established here. No large-vehicle scaling is introduced.

## Implemented validation and calculations

- One chassis, drive, generator, armor, carrier, weapon and ammunition selection.
- Faction allowlists, compatible weapon/carrier/ammo and chassis equipment slots.
- Armor percentages affect only chassis mass/HP/price, then add other components.
- Fixed ammunition loadout mass, electrical demand and conversion losses.
- Generator output, mechanical reserve and chassis mass limits.
- Power-to-weight square-root speed curve and chassis caps; half-up rounding.
  Offline floating-point calculations are NOT multiplayer simulation code.
- Sum of catalog tiers, sum of CP, maximum component tech requirement.
- Single-faction roster budget, default level 50, charging each design separately.
- Invalid experimental rows are displayed as invalid, not silently adjusted.

Baseline generator mass is accounted for in baseline chassis/drive figures in
this fixture; its zero incremental mass does not mean physical masslessness.

## Sources and limitations

Battlemaster variants: `mods/ca/rules/china/vehicles.yaml`, `weapons.yaml` and
`commander-tree.yaml`. `UsePointsOnProduction.cs` spends one point per promotion;
the legacy autoloader plus defense chain costs 2 total. The new design unlocks
directly for 2 CP. Defense components require tech 3; tech 1 denotes no explicit
tier2/tier3 requirement, NOT absence of factory prerequisites.

PDL recharge/interception, Reflector damage interactions, horde and uranium
upgrades, death effects, animation and rendering remain references, not simulated
or engine-tested behavior. No executable actor rules are emitted. Full schema
validation of untrusted data, multiple mounts, turret-speed fitting, build time,
UI, multiplayer, assets, harvesters and cargo remain out of scope.

Next: expand the reference sample using the SAME component values and global CP
value, exposing all residuals. Only after broader calibration choose final ratings
and implement the engine compiler.
