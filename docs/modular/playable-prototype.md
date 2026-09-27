# First map-only modular prototype

## What is implemented

`tools/modular/compile_prototype.py` reads the normal numerical catalog plus the
isolated `prototype.json` definitions, validates three GDI designs and generates
normal OpenRA actor/weapon rules in a standalone map package:

**`mods/ca/maps/modular-gdi-lab.oramap`**

The package contains `map.yaml`, terrain/preview, `modular-rules.yaml`,
`modular-weapons.yaml` and a manifest recording computed values and source hashes.
The compiler is run OFFLINE before starting the game; normal map rules are then
loaded by the engine before the match. There is no JSON compiler hook in the
match-loading C# code yet, no designer menu and no mid-match mutation.

This is a deliberately narrow vertical slice, NOT arbitrary trait composition.
Only the GDI medium-cannon assembly is supported. It binds to the complete MTNK
actor and 120mm weapon templates, then applies calculated cost, HP, armor, speed,
turn rate, weapon damage/range/reload and selected mobility behavior. Stock
projectiles, versus tables, recoil, muzzle, rendering and driver behavior remain
inherited. Conditional legacy upgrades also remain inherited; compare BASE state.

Unsupported components, equipment, crew, auxiliary mounts, commander-point
unlocks, duplicate actor IDs and unknown design fields are rejected. The map does
not offer production: all prototypes are placed at match start. This avoids
pretending factory exits or tech/CP unlocks are already implemented.

| Actor | Price | HP | Base speed | CP |
|---|---:|---:|---:|---:|
| modular.tank | 900 | 52000 | 82 | 0 |
| modular.hover | 1100 | 52000 | 79 | 0 |
| modular.stationary | 850 | 52000 | 0 | 0 |

The first row matches the stock GDI Battle Tank's base scalar values. The other
two are NEW test combinations, not existing equivalent vehicles or validated
balance. Both use the stock tracked tank artwork as a conspicuous placeholder.
A real hover hull and stationary platform artwork still need to be bound.

## Hover

A prototype hover gear adds provisional mass/cost/electrical demand and selects
the existing `hover` locomotor plus full `^HoverVehicle` trait template. Water
movement, water targeting, bobbing and sinking when disabled/driver-dead come
from that template, not new numerical simulations. Hover availability is limited
to this GDI adapter in the prototype, not universally unlocked in the catalog.

## GDI-exclusive stationary gear

`gdi-stationary` is available only to base faction GDI. It has zero self speed and
zero hull turn rate, but leaves the turret's turn rate alone. Its generator/motor
still supplies the selected modules' static demand. The calculator allows exactly
zero remaining mechanical reserve for stationary gear, never a negative reserve;
moving gear still requires positive reserve.

The generated actor retains `Mobile` for occupancy/position and `Carryable` for
transport, with `ImmovableCondition` and a permanently paused movement condition.
It uses the existing **wheeled terrain profile** for valid land placement, NOT
`Locomotor: immobile`: that engine profile has no terrain entries. Carryall derives
its drop-off terrain list from the cargo's Mobile locomotor; the empty profile
would prevent valid land drop-offs. `DeliverUnit` sets position and facing
explicitly, without requiring self locomotion. This has been inspected in source,
not yet verified by playing a match.

The stationary prototype removes inherited chrono/network teleport relocation.
Manual Carryall transport is the intended relocation path. Auto-Carryall ordering,
production rollout/deployment and build footprint selection remain future work.
Carryall may orient the platform while dropping it; that is not self rotation.

## Try the local map

1. Restart the local `modular` game so the new system map is discovered.
2. Create a skirmish and select **Modular GDI Lab (prototype)**.
3. Occupy the FIRST player slot / start A; add a second player or bot in slot two.
4. The three prototypes are near start A alongside a stock MTNK reference and a
   manual **OCAR Carryall**. An enemy light tank is farther east.
5. Compare the normal prototype's movement/fire to stock MTNK. Select the
   stationary prototype: move orders should do nothing while the turret can aim
   and fire. Use the Carryall to pick it up, fly elsewhere and unload on clear land.
6. Test invalid drop-off cells, pickup while attacking, destroying the carrier,
   saving/loading with cargo, and turret operation after delivery.

Developer mode is enabled on this lab map only. Terrain comes from the existing
isometric `testmap2.oramap`; the current lab does not add a dedicated water course.
Hover water/EMP tests need suitable water terrain (e.g. an editor copy of the lab).
No original map, global rules, engine traits or user artwork was replaced.

## Rebuild / validate

From Game, using a NEW output filename:

```sh
python tools/modular/compile_prototype.py --output mods/ca/maps/modular-gdi-lab-v2.oramap
python -m unittest discover -s tools/modular -q
python tools/modular/check_prototype.py --map mods/ca/maps/modular-gdi-lab-v2.oramap
```

The compiler refuses to overwrite existing maps. Zip content is deterministic;
the checked-in map was compared byte-for-byte with a fresh compilation. Different
map filenames still have the same display title, so avoid keeping confusing old
lab versions installed. No old Excel snapshots were modified.

## Validation actually performed

- 61 Python tests pass, including deterministic packaging, component rejection,
  generated rule values, stationary power limits and lint-comparison safeguards.
- Engine Utility loaded the generated map and checked actor/weapon rules.
- Full engine lint is **NOT clean**: the stock control map has 1685 diagnostics,
  the prototype 1691. The six additional errors repeat the stock MTNK's missing
  condition and `playerts` palette diagnostics once for each derived actor.
  No other new error was found in the differential check. See
  `tools/modular/prototype-validation.md`.
- The control must contain custom Rules too: Utility skips full rule lint for
  maps without custom Rules/Weapons. Comparing against untouched testmap2 alone
  would misleadingly report all stock rule errors as new.
- No interactive gameplay, rendering, transport or save/load test has been run.
  This is ready for the first manual trial, not a claim that those checks passed.
