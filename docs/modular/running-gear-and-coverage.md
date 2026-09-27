# Running gear and cross-faction coverage

## Rumpf / Fahrwerk / Motor are separate

The `chassis` component now represents the hull: HP, structural mass/price, mounting
interfaces, carrying limit and reference mobility calibration. Existing IDs remain
stable (including the approved Type 59 name).

A `running_gear` component represents wheels, tracks or legs. It contributes cost,
mass, power demand, tier, CP and tech exactly once like other hardware. It also
selects an EXISTING engine Locomotor and limits total weight, top speed and body
turn rate. The motor still supplies mechanical power; no dynamic electricity or
track-damage simulation is introduced.

Selection is a design-level `running_gear` ID. Omission selects the hull's declared
`default_running_gear` (shown explicitly in Excel AQ). The hull must allow the
selected ID, and the component must have the right role and faction. It is NOT an
equipment slot or a second motor. Inserting it a second time into the old main
component list is rejected. Missing/unknown/invalid selections are rejected.

Current options:

| Component | Physical type | Existing engine locomotor |
|---|---|---|
| wheels-light | wheels | wheeled |
| tracks-standard | tracks | tracked |
| tracks-light-artillery | tracks | lighttracked |
| tracks-superheavy | tracks | sheavytracked |
| walker-heavy | legs | sheavytracked |

The last row is intentional: Juggernaut uses sheavytracked in the current rules.
Names/artwork are not reliable substitutes for reading Mobile/Locomotor. The
lighttracked profile does NOT crush infantry, unlike ordinary tracked. Four
profiles' terrain multipliers and crush classes are copied from world.yaml and
checked against that file by tests. Unlisted terrains are not invented.

Body turning is currently `min(hull turn limit, running gear turn rate)`; this is
a provisional component decomposition of existing Mobile.TurnSpeed, not a torque
simulation. Turret turning remains separate. Speed retains the existing static
power-to-mass curve, additionally capped by the gear's speed limit. Terrain
multipliers belong to the existing engine locomotor; the displayed base speed is
not road speed and is not multiplied a second time by the calculator.

No hull is silently allowed to exchange tracks for wheels. Graphics, footprint,
water behavior, locomotor and required linked traits need an approved adapter.
Hover/air-cushion, amphibious and other walking systems remain future families,
not aliases for wheeled. Traits such as Hovers, water targeting, sinking on EMP,
movement animations and crushing slowdowns cannot be replaced by the Locomotor
string alone.

## Migration without charging for existing tracks twice

The existing hull values included running gear. Shared allocations were therefore
moved OUT of the hull and INTO the selected component:

| Existing hulls | Gear mass / credits removed from hull |
|---|---|
| Type 59, Dragon, Gatling, Heavy Tesla | 1500 kg / 100 |
| Overlord | 5000 kg / 250 |
| Juggernaut | 3500 kg / 300 |

These are experimental balancing allocations, not real-world measurements or
individual design discounts. A snapshot from `06f6fa36` verifies all 48 prior
examples keep their price, HP, speed, mass, load/reserve, CP, catalog points and
tech, including the deliberately invalid generator example remaining invalid.

Armor percentages now affect only the remaining hull, NOT the separated running
gear. Existing neutral 100% armor fits therefore stay unchanged. Non-neutral
spreadsheet what-if percentages intentionally differ from pre-split calculations.

## First broad calibration pass

Twelve new reference designs use shared components (no design-specific rebate):

| Reference | Price old / new | HP old / new | Speed old / new | New CP |
|---|---|---|---|---:|
| Hum-Vee | 400 / 400 | 15000 / 15000 | 157 / 157 | 0 |
| Buggy | 350 / 350 | 14000 / 14000 | 157 / 157 | 0 |
| Nod Light Tank | 625 / 625 | 41250 / 41250 | 100 / 100 | 0 |
| T-34 | 600 / 600 | 42000 / 42000 | 100 / 100 | 0 |
| Devil Tank | 700 / 700 | 45000 / 45000 | 90 / 90 | 0 |
| GDI Battle Tank | 900 / 900 | 52000 / 52000 | 82 / 82 | 0 |
| Allied Medium Tank parent | 800 / 800 | 45000 / 45000 | 82 / 82 | 0 |
| Challenger | 800 / 800 | 45000 / 45000 | 82 / 82 | 0 |
| Leclerc | 800 / 800 | 45000 / 45000 | 82 / 82 | 0 |
| Leopard | 800 / 800 | 45000 / 45000 | 82 / 82 | 0 |
| Soviet Heavy Tank | 1100 / 1100 | 65000 / 65000 | 68 / 68 | 0 |
| Allied Artillery | 550 / 550 | 10000 / 10000 | 56 / 56 | 0 |

All listed base scalar deviations are zero BY INITIAL CALIBRATION. Hull prices
and reference mass/power/speed were fitted; this is not independent evidence that
arbitrary combinations are balanced. The three Allied national tanks share their
2TNK parent values and are not three independent economic samples. 2TNK itself is
not buildable. Doctrine, drone pairing, conditional armor/cluster/laser upgrades,
driver behavior and death effects remain source-template integration work.

Actual reuse includes the Hum-Vee/Buggy MG, sensors, motor and wheels; the
LTNK/T-34/Devil 30mm family; and medium-shell behavior for 90mm/120mm/125mm with
single versus twin carriers. Sound/recoil/muzzle differences still need explicit
legacy bindings; scalar reuse is not permission to overwrite a complete weapon
with a damage-only definition. ARTY has fixed frontal artwork and Light armor.

## Family grouping supersedes raw actor counts

Use the new `vehicle-calibration-families.xlsx` / `FahrzeugFamilien` sheet.
The editorial grouping currently yields **108 provisional production-candidate
families**, plus **one prototype-only/availability-review family**. The original
388 raw blocks include upgrades, projectiles, wrecks and other helpers; that was
never a count of distinct vehicles. **15 families** have at least one scalar
reference design; neither all their variants nor runtime behavior are complete.

`vehicle-family-policy.json` records explicit anchors and aliases. Grouping does
not follow every inheritance link: Nona is not merely a BTR transport variant,
Heavy Tesla remains separate from light Tesla, while national Allied Medium Tank
skins and named Ranger upgrades belong together. Helpers are classified by local
ancestry plus reviewed exceptions; unknown cases stay visible for review.

A disabled base does not remove its active descendants: BATF and 2TNK are useful
family anchors. Non-production transformed/spawned forms are kept distinct from
locally `~disabled` prototypes. Raw appendix categories account for every actor
exactly once. Family member lists expose which specific actors have designs and
which do not; no family-wide gameplay completion claim is made.

The source-file faction is not a resolved ownership list. A local Buildable is
only evidence of a production candidate, not proof all prerequisites are
satisfiable. The original raw inventory remains available for source auditing.

## Sweep over six factions

`tools/modular/survey_vehicles.py` visits ALL roots of the China, GDI, Nod, Allies,
Soviet and Scrin vehicle files. `vehicle-coverage.md` and the Excel
`FahrzeugInventar` sheet list local price/HP/speed, raw inheritance, weapon IDs,
feature hints and linked designs. Variants, helpers, husks and disabled prototypes
are retained rather than miscounted as buildable units. Blank means inherited or
unresolved, NEVER zero. This is intentionally NOT another partial engine rules
resolver. Feature hints are incomplete and do not certify that a unit lacks an
inherited ability. No entry is marked fully gameplay-equivalent.

## Next passes: cover missing capabilities, not just more tank skins

| Family / examples to inspect | Missing reusable representation |
|---|---|
| BIKE, MLRS, FTRK, SLNG, Hyena, SEEK | Projectile launchers and AA/ground targeting packages, burst cadence; keep Hyena fragile/long-range |
| APC, APC2, BTR; choutpost, chcrawl2; BATF.Bunker | General transport module, capacity/doors, passenger fire variants, selected crew; not every cargo bay occupies roof |
| Mammoth, apoc, MAMMOTHMK2, TPOD, Hexapod | Multiple ordinary weapon mounts and shared attack controllers, not only one roof auxiliary |
| charty, 2S3, KATY, V3RL | Fixed/turreted artillery, rocket salvos, deployed/actor-missile behavior |
| Prismtank, Beam_Cannon, DISR, checm, STCR | Existing beam/chain/pulse templates and necessary support traits |
| Chem_Sprayer, Source_of_Pollution, FTNK, CORR | Persistent fields, chemical/corrosion effects and fixed linked gun assemblies; no pollution speed buff |
| Mammoth.Hover, hmlrs, LTNK.Laser, Scrin hover units | Hover hardware plus linked movement/water/targeting/death/EMP behavior |
| TITN, Juggernaut, GUNW, TPOD | Distinct leg assemblies; movement and attack animations often lock hull and gun together |
| STNK, SPEC; MGG, MRJ, MEMP | Cloak, bombardment, shroud/jamming and support modules; cost without fabricated guns |
| HARV, HAR2, chharv, Soviet_Miner, HARV.Scrin; MCVs | Unarmed designs (zero weapons), harvesting/docking/transform modules and heavy wheels |
| IFV and faction-specific promotions | Pre-match frozen loadouts versus actual passenger-dependent weapon behavior; avoid conflating the two |

This table is an investigation plan, not a claim these all have components now.
Aircraft/naval families and rules outside the six vehicle files are not covered
by this inventory. Existing release gameplay, Nuke MiG and Bunker Emperor balance
are unchanged.
