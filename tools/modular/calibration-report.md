# Vehicle calibration report

EXPERIMENTAL scalar fit, not a complete gameplay-equivalence test.
Legacy target source blocks audited at 9d8a9dec. Rows are independent candidates.
Base states without isotope stability, horde, uranium or veterancy bonuses. Isotope stability now applies 90% received damage as well as death-explosion suppression; not modeled as extra HP.
CP, tech and catalog columns are NEW design values, not audited legacy targets.

Global credit value per CP: 300 (experimental).

## Existing vehicle vs configured vehicle

| Reference / configured candidate | Credits old -> new | HP old -> new | Speed old -> new | New CP / catalog / tech | Gross - discount = price |
|---|---|---|---|---|---|
| Battlemaster Nuclear Shells | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 1 / 2 / 1 | 1250 - 300 = 950 |
| Battlemaster Nuclear Shells PDL | 1600 -> 1600 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 5 / 3 | 2200 - 600 = 1600 |
| Battlemaster Nuclear Shells Reflector | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 5 / 3 | 1550 - 600 = 950 |
| Battlemaster Mass Production | 600 -> 600 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 1 / 1 / 1 | 900 - 300 = 600 |
| Battlemaster Mass Production PDL | 1000 -> 1250 (+25.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 4 / 3 | 1850 - 600 = 1250 |
| Battlemaster Mass Production Reflector | 600 -> 600 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 4 / 3 | 1200 - 600 = 600 |
| Battlemaster Autoloader | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 1 / 2 / 1 | 1250 - 300 = 950 |
| Battlemaster Autoloader PDL | 1600 -> 1600 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 5 / 3 | 2200 - 600 = 1600 |
| Battlemaster Autoloader Reflector | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 2 / 5 / 3 | 1550 - 600 = 950 |
| Battlemaster baseline | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 100 -> 100 (+0.0%) | 0 / 1 / 1 | 950 - 0 = 950 |
| Battlemaster nuclear drive | 950 -> 950 (+0.0%) | 40000 -> 40000 (+0.0%) | 125 -> 125 (+0.0%) | 1 / 2 / 2 | 1250 - 300 = 950 |
| Juggernaut baseline | 2000 -> 2000 (+0.0%) | 60000 -> 60000 (+0.0%) | 50 -> 50 (+0.0%) | 0 / 3 / 3 | 2000 - 0 = 2000 |
| Juggernaut EMP / baseline | 2000 -> 1800 (-10.0%) | 60000 -> 60000 (+0.0%) | 50 -> 45 (-10.0%) | 1 / 4 / 3 | 2100 - 300 = 1800 |
| Juggernaut EMP / efficient | 2000 -> 2000 (+0.0%) | 60000 -> 60000 (+0.0%) | 50 -> 50 (+0.0%) | 1 / 5 / 3 | 2300 - 300 = 2000 |
| Heavy Tesla Tank baseline | 1350 -> 1350 (+0.0%) | 48000 -> 48000 (+0.0%) | 100 -> 100 (+0.0%) | 0 / 3 / 2 | 1350 - 0 = 1350 |

## Hardware cost breakdown

Armor modifies chassis price before hardware is summed. Credits are not deducted per component.

| Reference candidate | Hardware contributions (credits) | Total - CP credit = price |
|---|---|---|
| Battlemaster Nuclear Shells | battlemaster: 500; diesel: 100; baseline-generator: 0; cannon-turret: 100; cannon: 200; nuclear-shell: 350 | 1250 - 1 x 300 = 950 |
| Battlemaster Nuclear Shells PDL | battlemaster: 500; diesel-large: 200; efficient-generator: 200; cannon-turret: 100; cannon: 200; nuclear-shell: 350; pdl: 650 | 2200 - 2 x 300 = 1600 |
| Battlemaster Nuclear Shells Reflector | battlemaster: 500; diesel-large: 200; efficient-generator: 200; cannon-turret: 100; cannon: 200; nuclear-shell: 350 | 1550 - 2 x 300 = 950 |
| Battlemaster Mass Production | battlemaster-mass-produced: 450; diesel: 100; baseline-generator: 0; cannon-turret: 100; cannon: 200; shell: 50 | 900 - 1 x 300 = 600 |
| Battlemaster Mass Production PDL | battlemaster-mass-produced: 450; diesel-large: 200; efficient-generator: 200; cannon-turret: 100; cannon: 200; shell: 50; pdl: 650 | 1850 - 2 x 300 = 1250 |
| Battlemaster Mass Production Reflector | battlemaster-mass-produced: 450; diesel-large: 200; efficient-generator: 200; cannon-turret: 100; cannon: 200; shell: 50 | 1200 - 2 x 300 = 600 |
| Battlemaster Autoloader | battlemaster: 500; diesel: 100; baseline-generator: 0; autoloader-turret: 400; cannon: 200; shell: 50 | 1250 - 1 x 300 = 950 |
| Battlemaster Autoloader PDL | battlemaster: 500; diesel-large: 200; efficient-generator: 200; autoloader-turret: 400; cannon: 200; shell: 50; pdl: 650 | 2200 - 2 x 300 = 1600 |
| Battlemaster Autoloader Reflector | battlemaster: 500; diesel-large: 200; efficient-generator: 200; autoloader-turret: 400; cannon: 200; shell: 50 | 1550 - 2 x 300 = 950 |
| Battlemaster baseline | battlemaster: 500; diesel: 100; baseline-generator: 0; cannon-turret: 100; cannon: 200; shell: 50 | 950 - 0 x 300 = 950 |
| Battlemaster nuclear drive | battlemaster: 500; nuclear: 400; baseline-generator: 0; cannon-turret: 100; cannon: 200; shell: 50 | 1250 - 1 x 300 = 950 |
| Juggernaut baseline | juggernaut: 1100; diesel: 100; baseline-generator: 0; fixed-triple: 300; artillery: 300; artillery-shell: 200 | 2000 - 0 x 300 = 2000 |
| Juggernaut EMP / baseline | juggernaut: 1100; diesel: 100; baseline-generator: 0; fixed-triple: 300; artillery: 300; emp-shell: 300 | 2100 - 1 x 300 = 1800 |
| Juggernaut EMP / efficient | juggernaut: 1100; diesel: 100; efficient-generator: 200; fixed-triple: 300; artillery: 300; emp-shell: 300 | 2300 - 1 x 300 = 2000 |
| Heavy Tesla Tank baseline | heavy-tesla: 600; diesel: 100; baseline-generator: 0; twin-coil: 200; tesla: 350; discharge: 100 | 1350 - 0 x 300 = 1350 |

## Audited Battlemaster variant fields (old -> configured)

Rule-data comparison only, not engine firing/armor simulation. Damage is the raw warhead value.
Tech 1 means no explicit tier2/tier3 requirement, not waived factory prerequisites.
Single-shot interval 0 denotes not applicable, not an audited engine default.

| Design | CP | Tech | Armor | Burst | Shot interval | Reload | Range (cells) | Raw damage |
|---|---|---|---|---|---|---|---|---|
| Battlemaster Nuclear Shells | 1 -> 1 | 1 -> 1 | Heavy -> Heavy | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 10000 -> 10000 |
| Battlemaster Nuclear Shells PDL | 2 -> 2 | 3 -> 3 | Heavy -> Heavy | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 10000 -> 10000 |
| Battlemaster Nuclear Shells Reflector | 2 -> 2 | 3 -> 3 | Reflector -> Reflector | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 10000 -> 10000 |
| Battlemaster Mass Production | 1 -> 1 | 1 -> 1 | Heavy -> Heavy | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |
| Battlemaster Mass Production PDL | 2 -> 2 | 3 -> 3 | Heavy -> Heavy | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |
| Battlemaster Mass Production Reflector | 2 -> 2 | 3 -> 3 | Reflector -> Reflector | 1 -> 1 | 0 -> 0 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |
| Battlemaster Autoloader | 1 -> 1 | 1 -> 1 | Heavy -> Heavy | 3 -> 3 | 5 -> 5 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |
| Battlemaster Autoloader PDL | 2 -> 2 | 3 -> 3 | Heavy -> Heavy | 3 -> 3 | 5 -> 5 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |
| Battlemaster Autoloader Reflector | 2 -> 2 | 3 -> 3 | Reflector -> Reflector | 3 -> 3 | 5 -> 5 | 50 -> 50 | 4 -> 4 | 5000 -> 5000 |

## Shared defense-package price increments

| Family | PDL increment old / new | Reflector increment old / new |
|---|---|---|
| Battlemaster Autoloader | +650 / +650 | +0 / +0 |
| Battlemaster Nuclear Shells | +650 / +650 | +0 / +0 |
| Battlemaster Mass Production | +400 / +650 | +0 / +0 |

With identical added hardware and CP, an additive model yields the same increment.
Changing the global CP value cannot make that same PDL package cost both +650 and +400.

## Experimental combinations without an exact legacy counterpart

These are not claims of matching existing PDL/autoloader/reflector variants.

| Design | Credits | HP | Speed | Armor | CP / catalog / tech |
|---|---:|---:|---:|---|---|
| Battlemaster PDL / baseline generator | 1300 | 40000 | 76 | Heavy | 1 / 2 / 3 |
| Battlemaster PDL / efficient generator | 1500 | 40000 | 95 | Heavy | 1 / 3 / 3 |
| Battlemaster reflector / efficient | 850 | 40000 | 94 | Reflector | 1 / 3 / 3 |
| Battlemaster PDL + reflector / baseline | INVALID: Generator output exceeded | | | | |
| Battlemaster PDL + reflector / efficient | 1200 | 40000 | 86 | Reflector | 2 / 4 / 3 |

All CP contributions use the same global credit value. No component or design rebates.
The discount applies to every configuration, including cheaper/slower alternatives.
