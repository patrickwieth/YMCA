# Weapon, supply and drivetrain design agreement

Status: design only. These mechanics are NOT implemented by this document or by the cannon
portrait bindings. Do not infer working supplies, battery simulation or interchangeable controllers.
All new item artwork is supplied by the user; do not generate item pictures or placeholders.

## Weapon assembly

- Cannon/launcher: establishes caliber, baseline damage, firing frequency and range.
- Gun Control System: combines loading, targeting and firing control into one component with
  explicit advantages and disadvantages. Loading System is the neutral term for a loading
  mechanism; Autoloader refers specifically to automatic loading, not every loading system.
- Ammunition: defines damage type, target effectiveness and special effects, subject to explicit
  weapon/controller compatibility. A controller does not turn ordinary ammunition into Tesla ammo.

Candidate controllers: Standard, Rapid-Fire, Precision, Burst and Airburst; specialized ammunition
support later. Burst means successive shots; airburst means detonation above the target.
Example only: +30% firing frequency and -20% range. +30% frequency means reload time / 1.3,
not reload time * 0.7. Exact modifiers and allocations have not been approved.
Frost missiles and Tesla impact arcs have native precedents, but are not yet freely interchangeable
modular ammunition. The future rapid-fire 30mm Autocannon is separate from the legacy slow-fire
cannons now designated 75mm.

## Supplies: vehicle equivalent of power capacity

Supplies is an abstract capacity, not a currency consumed per shot. Buildings provide Supply
Capacity; vehicles occupy Supply Usage. No supply trucks or manual delivery loop.

- More carried ammunition reduces Supply Usage at the expense of installation space.
- Higher firing frequency and more demanding ammunition increase supply demand.
- Ammunition packs can have different capacities and footprints.
- Under-supply is intended to prolong automatic magazine replenishment, not permanently disable
  combat by exhausting a finite total ammunition stock.
- Proposed starting relation: supply demand proportional to consumption * supply effort / carried
  reserve. This is NOT a finalized formula; floors, diminishing returns and balancing remain open.

Three corresponding reserve modules:

1. Ammunition packs for bullets, shells and missiles.
2. Universal tanks for fluid weapons.
3. Capacitor banks as the ammunition equivalent for energy weapons.

More reserve does not itself increase damage or firing frequency. Capacitors are not the vehicle's
traction battery; their agreed role is energy-weapon reserve and supply-demand reduction.

## Fluid weapons

A Universal Tank uses the same container artwork for fuel, toxin, chemical, acid, cryogenic fluid,
etc. Each installed tank has a selected filling, not every fluid simultaneously.
Tank capacity is relevant to Supplies. Filling determines the effect. A compatible Fluid Pump
feeds the weapon; flow, power consumption and compatibility are future design parameters.
A Fluid Pump for weapons is distinct from a hydraulic drivetrain pump.

## Real battery charge and drivetrain

A Battery Pack has actual capacity and current charge. Movement and electrical consumers draw
energy. A vehicle may omit an onboard combustion engine/generator and operate electrically from
its battery, but then needs external charging. It stops when it lacks energy for propulsion.
Initial charge, persistence details, charging sources/rates and consumer priority remain undecided.
A battery supplies energy; an electric drive converts it into movement.

The running-gear assembly determines drive type:

- Electric: powered by a generator and/or battery.
- Hydraulic: requires a powered hydraulic pump.
- Mechanical: requires an engine and mechanical transmission.

Do not confuse electrical energy with abstract Supplies. A larger capacitor reserve is not an
automatic substitute for a charged propulsion battery.

## Artwork and implementation backlog

User reports bullets, missiles and shells artwork in progress. Only files actually present in the
repository are versioned; do not claim absent images have been integrated.
Needed/planned families include controller modules, universal tanks, fluid pumps, capacitor banks,
batteries and (later) specialized ammunition, bombs, torpedoes, mines and alien ammunition.
The extra engine, generator and infantry PNGs committed with this snapshot are source assets only;
no compatibility or runtime simulation is implied by their presence.

Implement these systems in separate reviewed steps. Preserve native gameplay, saved IDs and frozen
compiled maps until explicit mechanical changes are approved and validated.
