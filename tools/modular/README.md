# Offline vehicle calibration prototype

Run from Game (Python standard library only):

```sh
python tools/modular/calibrate.py
python -m unittest discover -s tools/modular -v
```

`catalog.json` is an experimental research fixture, NOT engine YAML or the final
saved-design format. No gameplay rules or artwork are modified. Legacy targets
are manually transcribed from commit `7f5aeadf`; see
`docs/modular/vehicle-reference-study.md`. All mass, power, prices and catalog
tiers are proposals. Matching fitted targets does not establish general balance.

## Excel workbook

Open `tools/modular/vehicle-calibration.xlsx` in Excel. Seven sheets provide
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

## Branch freshness / merge preview

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

## One global CP value

```
hardware = chassis_cost * armor_cost_percent / 100 + other_component_costs
CP = sum(component CP contributions)
production_cost = hardware - CP * credits_per_cp
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
