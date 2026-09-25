# Heavy vehicle inspection: Overlord

Source: merged Game `9d8a9dec`, `mods/ca/rules/china/vehicles.yaml`,
`china/weapons.yaml`, `china/defaults.yaml`, `china/commander-tree.yaml` and
`rules/defaults.yaml`. These are legacy references, NOT configured designs.

| Family | Base family price | With PDL | PDL increment | With Reflector | CP family / defense |
|---|---:|---:|---:|---:|---|
| Plain Overlord | 2000 | no direct variant | - | no direct variant | 0 / - |
| Gatling | 2000 | 2800 | +800 | 2000 | 1 / 2 |
| Bunker | 3000 | 3600 | +600 | 3000 | 1 / 2 |
| Propaganda | 2200 | 2800 | +600 | 2200 | 1 / 2 |
| Nuclear shells | 2500 | 3000 | +500 | 2500 | 1 / 2 |
| Plasma | 2000 | 2500 | +500 | 2000 | 1 / 2 |

All inherit base HP 95000 and speed 56. These exclude nuclear-drive speed,
isotope stability, veterancy and other conditional effects. Emperor is the same
actor family with Tank-General conditions: received damage multiplier 80%, not
118750 base HP. The plain Overlord requires radar; upgrade actors require tier3
via actor and/or promotion access. Do not infer a universal tech level from price.

## What this tells us

All five defense variants use the standard `^PointLaserDefenseSystem`; Bunker
reaches it through `^AttackingCargoAndPDL`. No evidence here of a larger-capacity
PDL simply because the hull is large. Reflector replaces Heavy as before.

The SAME hull has legacy PDL increments +800, +600 and +500. Therefore neither a
single additive package price nor a hull-size multiplier alone explains these
prices. These may reflect whole-loadout balancing, different hypothetical support
hardware or historical inconsistency. Do not silently call them hardware facts.

For orientation only, our current +650 defense package would overshoot Bunker/
Propaganda by 50 and Nuclear/Plasma by 150, while undershooting Gatling by 150.
Those are NOT configured vehicle outputs: heavy motor/weight/module budgets have
not been fitted. No new scaling rule or exception is introduced.

## Real composition distinctions

- Gatling has an additional rotating carrier, retains its main cannon and also
  inherits `^SensorEquipment`. Use `^ChinaGatlingOverlord` for the existing weapon
  behavior; it is not merely the ground-only representative in the calculator.
- Bunker has capacity 4 and starts with four `che3` Tank Hunters. Its price cannot
  be assigned entirely to an empty transport module. Passenger firing uses the
  existing cargo/attack traits, including the PDL-compatible combination.
- Propaganda carries the existing speaker behavior and its upgrade interactions.
- Nuclear changes payload and range (5c512); it does not itself imply nuclear
  propulsion. Reuse complete weapon effects, not just the raw damage number.
- Plasma uses two `CHECMBeam.Overlord` armaments. The older
  `OverlordCannon.Plasma` definition is explicitly marked unused in weapon rules.
- Existing Overlord art uses different turret sequences and conditional Emperor
  hull graphics. Availability of sequences does not prove arbitrary attachments
  can be freely combined; retain graphic compatibility locks until verified.

## Component names: proposal for discussion, not a migration

Keep three separate concepts:
1. Stable technical ID for saved designs and references, e.g. `drive.diesel.medium`.
2. Player-facing/localizable name, e.g. "Mittlerer Dieselmotor".
3. Legacy source/template metadata, e.g. `^ChinaGatling` or `choverlord.Gatling`.

Do not bake balance numbers, CP values or tier into persistent IDs. Display stats
and faction beside the name. Existing prototype IDs remain unchanged until names
are agreed; renaming saved design identifiers later would require migration.

Examples:
| Category | Proposed display name |
|---|---|
| Hull | Schweres Kettenfahrgestell (Overlord) |
| Carrier | Schwerer Zwillingsgeschützturm |
| Additional carrier | Gatling-Aufsatzturm |
| Equipment | Besetzbarer Bunker; Propagandalautsprecher |
| Drive | Mittlerer Dieselmotor; Leistungsstarker Dieselmotor; Nuklearantrieb |
| Generator | Basisgenerator; Hocheffizienzgenerator |
| Defense | Punktlaserabwehr; Reflektorpanzerung |

Retain vehicle names where they identify real artwork/geometry. Use functional
names for reusable behavior and hardware. Do not invent an unverified caliber
(e.g. 180 mm) merely to make a weapon name sound precise.
