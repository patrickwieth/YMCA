"""Offline design calculator. No engine rules are generated or modified."""
import argparse
import json
import math
from pathlib import Path

ROLES = {"chassis", "drive", "generator", "armor", "carrier", "weapon", "ammunition"}


def crew_totals(catalog, design, parts):
    loadout = design.get('crew', [])
    cargo = [p for p in parts if 'cargo_capacity' in p]
    if loadout and len(cargo) != 1:
        raise ValueError('Start crew requires exactly one cargo module')
    totals = dict(cost=0, mass=0, weight=0, count=0, tech=0)
    seen = set()
    for entry in loadout:
        actor, count = entry['actor'], entry['count']
        if actor in seen:
            raise ValueError('Duplicate crew type; use its count field')
        seen.add(actor)
        if isinstance(count, bool) or not isinstance(count, int) or count <= 0:
            raise ValueError('Crew count must be a positive integer')
        unit = catalog['infantry'][actor]
        if design['faction'] not in unit['factions'] or actor not in cargo[0]['allowed_infantry']:
            raise ValueError('Infantry not compatible with faction or cargo module')
        for key in ('cost', 'mass', 'weight'):
            if not math.isfinite(unit[key]) or unit[key] < 0:
                raise ValueError('Invalid infantry ' + key)
            totals[key] += count * unit[key]
        totals['count'] += count
        totals['tech'] = max(totals['tech'], unit['tech'])
    if cargo and totals['weight'] > cargo[0]['cargo_capacity']:
        raise ValueError('Crew exceeds cargo capacity')
    return totals


def auxiliary_summary(catalog, selection):
    if not selection:
        return None
    carrier, weapon, ammo = (catalog['components'][selection[k]] for k in ('carrier', 'weapon', 'ammunition'))
    return dict(carrier=selection['carrier'], weapon=selection['weapon'], ammunition=selection['ammunition'],
                range_cells=ammo.get('range_override_cells', weapon.get('range_cells')),
                min_range_cells=weapon.get('min_range_cells', 0),
                reload_ticks=carrier['reload_ticks'], burst=carrier['burst'],
                fire_delay_ticks=carrier.get('fire_delay_ticks', 0), damage=ammo.get('damage'))


def calculate(catalog, design):
    ids = design["components"]
    if len(ids) != len(set(ids)):
        raise ValueError("Duplicate component selection")
    parts = [catalog["components"][key] for key in ids]
    by_role = {}
    for part in parts:
        if design["faction"] not in part["factions"]:
            raise ValueError("Component not available to faction")
        if part["role"] != "equipment":
            if part["role"] in by_role:
                raise ValueError("Multiple components for a required role")
            by_role[part["role"]] = part
        if part["tier"] not in range(4) or part["cp"] < 0:
            raise ValueError("Invalid component point cost")
    if set(by_role) - {"manufacturing"} != ROLES:
        raise ValueError("Select exactly one component for every required role")
    chassis = by_role["chassis"]
    if chassis["tier"] < 1:
        raise ValueError("Chassis cannot be tier zero")
    if chassis["carrier_slots"] not in (1, 2):
        raise ValueError("Prototype supports one main and at most one auxiliary carrier")
    for role, allowed in chassis["allowed"].items():
        if not any(key in allowed and catalog["components"][key]["role"] == role for key in ids):
            raise ValueError("Incompatible chassis " + role)
    gear_id = design.get('running_gear', chassis.get('default_running_gear'))
    if not gear_id or gear_id not in catalog['components']:
        raise ValueError('Select a running gear component')
    gear = catalog['components'][gear_id]
    if (gear['role'] != 'running_gear' or design['faction'] not in gear['factions']
            or gear_id not in chassis.get('allowed_running_gear', [])):
        raise ValueError('Incompatible running gear')
    if gear['tier'] not in range(4) or gear['cp'] < 0:
        raise ValueError('Invalid running gear point cost')
    stationary = gear.get('kind') == 'stationary'
    for field in ('max_mass', 'max_speed', 'turn_speed'):
        value = gear[field]
        if not math.isfinite(value) or (value <= 0 if field == 'max_mass' or not stationary else value != 0):
            raise ValueError('Invalid running gear limit')
    for field in ('mass', 'cost', 'electric_kw'):
        if not math.isfinite(gear[field]) or gear[field] < 0:
            raise ValueError('Invalid running gear rating')
    if gear['locomotor'] not in catalog['locomotors']:
        raise ValueError('Unknown running gear locomotor')
    parts.append(gear)
    carrier = by_role["carrier"]
    weapon_id = next(key for key in ids if catalog["components"][key]["role"] == "weapon")
    ammo_id = next(key for key in ids if catalog["components"][key]["role"] == "ammunition")
    if weapon_id not in carrier["weapons"] or ammo_id not in by_role["weapon"]["ammunition"]:
        raise ValueError("Incompatible carrier, weapon or ammunition")
    equipment = [key for key in ids if catalog["components"][key]["role"] == "equipment"]
    if len(equipment) > chassis.get("equipment_slots", chassis["tier"]):
        raise ValueError("Equipment slot capacity exceeded")
    if any(key not in chassis["equipment"] for key in equipment):
        raise ValueError("Incompatible equipment")
    occupied = [p['attachment_slot'] for p in parts if 'attachment_slot' in p]
    auxiliary = design.get('auxiliary_mount')
    if auxiliary:
        if set(auxiliary) != {'carrier', 'weapon', 'ammunition'}:
            raise ValueError('Auxiliary mount needs carrier, weapon and ammunition')
        mount = chassis.get('auxiliary_mount')
        approved = mount and auxiliary['carrier'] in mount['carriers']
        draft = (mount and design.get('experimental_graphics') is True
                 and auxiliary['carrier'] in mount.get('experimental_carriers', []))
        if chassis['carrier_slots'] < 2 or not (approved or draft):
            raise ValueError('Incompatible auxiliary mount (unverified graphics require explicit draft opt-in)')
        occupied.append(mount['slot'])
        extra = [catalog['components'][auxiliary[role]] for role in ('carrier', 'weapon', 'ammunition')]
        for role, part in zip(('carrier', 'weapon', 'ammunition'), extra):
            if part['role'] != role or design['faction'] not in part['factions']:
                raise ValueError('Invalid auxiliary role or faction')
            if part['tier'] not in range(4) or part['cp'] < 0:
                raise ValueError('Invalid auxiliary point cost')
        if auxiliary['weapon'] not in extra[0]['weapons'] or auxiliary['ammunition'] not in extra[1]['ammunition']:
            raise ValueError('Incompatible auxiliary weapon or ammunition')
        parts += extra
    if len(occupied) != len(set(occupied)):
        raise ValueError('Attachment slot is already occupied')
    all_ids = ids + [gear_id] + (list(auxiliary.values()) if auxiliary else [])
    for part in parts:
        if 'included_units' in part:
            raise ValueError('Fixed module crew is obsolete; use design crew selection')
        if any(key in all_ids for key in part.get('excludes', [])):
            raise ValueError('Mutually exclusive components')
    crew = crew_totals(catalog, design, parts)
    armor = by_role["armor"]
    others = [p for p in parts if p["role"] not in ("chassis", "armor")]
    mass = chassis["mass"] * armor["mass_percent"] / 100 + sum(p["mass"] for p in others) + crew['mass']
    cost = chassis["cost"] * armor["cost_percent"] / 100 + sum(p["cost"] for p in others) + crew['cost']
    gross_cost = cost
    manufacturing_factor = by_role.get("manufacturing", {}).get("hardware_percent", 100) / 100
    if not math.isfinite(manufacturing_factor) or not 0 < manufacturing_factor <= 1:
        raise ValueError("Manufacturing factor must be in (0, 1]")
    cost *= manufacturing_factor
    manufactured_cost = cost
    if "design_credit_discount" in design or any("design_credit_discount" in p for p in parts):
        raise ValueError("Individual discounts are not supported")
    cp_value = catalog["credits_per_cp"]
    if not math.isfinite(cp_value) or cp_value < 0:
        raise ValueError("Global CP credit value must be finite and nonnegative")
    discount = sum(p["cp"] for p in parts) * cp_value
    cost -= discount
    if cost <= 0:
        raise ValueError("Design discounts must leave a positive production cost")
    hp = chassis["hp"] * armor["hp_percent"] / 100
    demand = sum(p["electric_kw"] for p in parts)
    gen = by_role["generator"]
    efficiency = gen["efficiency"]
    if not 0 < efficiency <= 1:
        raise ValueError("Generator efficiency must be in (0, 1]")
    if demand > gen["max_electric_kw"]:
        raise ValueError("Generator output exceeded")
    reserve = by_role["drive"]["mechanical_kw"] - demand / efficiency
    if reserve < 0 or (reserve == 0 and not stationary):
        raise ValueError("No mechanical driving reserve")
    if mass <= 0 or mass > min(chassis["max_mass"], gear['max_mass']):
        raise ValueError("Invalid mass or chassis load exceeded")
    if stationary:
        speed = 0
    else:
        ratio = (reserve / mass) / (chassis["reference_kw"] / chassis["reference_mass"])
        speed = min(chassis["max_speed"], gear['max_speed'], chassis["reference_speed"] * ratio ** catalog["alpha"])
    return dict(mass=mass, hp=hp, cost=cost, gross_cost=gross_cost, discount=discount,
                manufacturing_factor=manufacturing_factor, manufactured_cost=manufactured_cost,
                included_crew_cost=crew['cost'], crew_mass=crew['mass'], crew_count=crew['count'],
                auxiliary=auxiliary_summary(catalog, auxiliary),
                running_gear=gear_id, locomotor=gear['locomotor'],
                turn_speed=min(chassis['turn_speed_limit'], gear['turn_speed']),
                electric_kw=demand, reserve_kw=reserve,
                speed=math.floor(speed + 0.5), armor=armor["armor_type"],
                catalog_points=sum(p["tier"] for p in parts), cp=sum(p["cp"] for p in parts),
                tech=max(crew['tech'], max(p["tech"] for p in parts)),
                burst=carrier["burst"], burst_delay_ticks=carrier.get("burst_delay_ticks", 0),
                reload_ticks=carrier["reload_ticks"],
                range_cells=by_role["ammunition"].get("range_override_cells", by_role["weapon"].get("range_cells")),
                damage=by_role["ammunition"].get("damage"))


def validate_roster(catalog, designs, level=50):
    if level < 1:
        raise ValueError("General level must be positive")
    if len({d["faction"] for d in designs}) > 1:
        raise ValueError("A roster must use one base faction")
    total = sum(calculate(catalog, design)["catalog_points"] for design in designs)
    if total > level:
        raise ValueError("Catalog budget exceeded")
    return total


def comparison(old, new):
    delta = f"{(new / old - 1) * 100:+.1f}%" if old else ("0%" if new == 0 else "n/a")
    return f"{old:g} -> {new:g} ({delta})"


def report(catalog):
    lines = ["# Vehicle calibration report", "",
             "EXPERIMENTAL scalar fit, not a complete gameplay-equivalence test.",
             f"Legacy target source blocks audited at {catalog['reference_commit']}. Rows are independent candidates.",
             catalog['reference_conditions'],
             "CP, tech and catalog columns are NEW design values, not audited legacy targets.", "",
             f"Global credit value per CP: {catalog['credits_per_cp']:g} (experimental).", "",
             "## Existing vehicle vs configured vehicle", "",
             "| Reference / configured candidate | Credits old -> new | HP old -> new | Speed old -> new | New CP / catalog / tech | Hardware x manufacturing - CP = price |",
             "|---|---|---|---|---|---|"]
    for design in catalog["designs"]:
        if not design.get("target"):
            continue
        r = calculate(catalog, design)
        target = design["target"]
        cells = " | ".join(comparison(target[k], r[k]) for k in ("cost", "hp", "speed"))
        lines.append(f"| {design['name']} | {cells} | {r['cp']} / {r['catalog_points']} / {r['tech']} | {r['gross_cost']:g} x {r['manufacturing_factor']:g} - {r['discount']:g} = {r['cost']:g} |")
    lines += ["", "## Running gear (separate component)", "",
              "Hull defaults are selected unless the design explicitly overrides running_gear.",
              "No automatic tracked/wheeled conversion: compatibility and artwork must be approved first.",
              "Turn rate is the minimum of hull and running-gear limits. Terrain profiles reuse world.yaml.", "",
              "| Design | Gear | Engine locomotor | Body turn rate |", "|---|---|---|---:|"]
    for design in catalog['designs']:
        try:
            r = calculate(catalog, design)
        except ValueError:
            continue  # Deliberately invalid teaching examples are reported below.
        lines.append(f"| {design['name']} | {r['running_gear']} | {r['locomotor']} | {r['turn_speed']} |")
    lines += ["", "## Hardware cost breakdown", "",
              "Armor modifies chassis price before hardware is summed. Credits are not deducted per component.", "",
              "| Reference candidate | Hardware contributions (credits) | Total - CP credit = price |",
              "|---|---|---|"]
    for design in catalog["designs"]:
        if not design.get("target"):
            continue
        r = calculate(catalog, design)
        selected = design['components'] + [r['running_gear']] + list(design.get('auxiliary_mount', {}).values())
        parts = [(key, catalog["components"][key]) for key in selected]
        armor = next(p for _, p in parts if p["role"] == "armor")
        contributions = []
        for key, p in parts:
            if p["role"] == "armor":
                continue
            cost = p["cost"] * armor["cost_percent"] / 100 if p["role"] == "chassis" else p["cost"]
            contributions.append(f"{key}: {cost:g}")
        if design.get('crew'):
            contributions.append(f"start crew: {r['included_crew_cost']:g}")
        lines.append(f"| {design['name']} | {'; '.join(contributions)} | {r['gross_cost']:g} x {r['manufacturing_factor']:g} - {r['cp']} x {catalog['credits_per_cp']:g} = {r['cost']:g} |")
    lines += ["", "## Selectable starting crew", "",
              "Empty bunker and selected infantry are charged separately, exactly once.",
              "Manufacturing currently scales loaded package cost too; personnel treatment remains provisional.", "",
              "| Design | Starting units | Crew cost | Crew mass kg |",
              "|---|---|---:|---:|"]
    for design in catalog['designs']:
        if 'crew' in design:
            result = calculate(catalog, design)
            units = ', '.join(f"{u['count']} x {u['actor']}" for u in design['crew']) or 'empty'
            lines.append(f"| {design['name']} | {units} | {result['included_crew_cost']:g} | {result['crew_mass']:g} |")
    lines += ["", "## Audited Battlemaster variant fields (old -> configured)", "",
              "Rule-data comparison only, not engine firing/armor simulation. Damage is the raw warhead value.",
              "Tech 1 means no explicit tier2/tier3 requirement, not waived factory prerequisites.",
              "Single-shot interval 0 denotes not applicable, not an audited engine default.", "",
              "| Design | CP | Tech | Armor | Burst | Shot interval | Reload | Range (cells) | Raw damage |",
              "|---|---|---|---|---|---|---|---|---|"]
    for design in catalog["designs"]:
        target = design.get("target", {})
        if "burst" not in target:
            continue
        r = calculate(catalog, design)
        cells = " | ".join(f"{target[k]} -> {r[k]}" for k in
                           ("cp", "tech", "armor", "burst", "burst_delay_ticks", "reload_ticks", "range_cells", "damage"))
        lines.append(f"| {design['name']} | {cells} |")
    lines += ["", "## Shared defense-package price increments", "",
              "| Family | PDL increment old / new | Reflector increment old / new |",
              "|---|---|---|"]
    indexed = {d["name"]: d for d in catalog["designs"]}
    for family in ("Battlemaster Autoloader", "Battlemaster Nuclear Shells", "Battlemaster Mass Production", "Dragon Tank", "Gatling Tank", "Overlord Nuclear Shells", "Overlord Propaganda", "Overlord Bunker", "Overlord Gatling"):
        base = indexed[family]
        base_cost = calculate(catalog, base)["cost"]
        cells = []
        for suffix in (" PDL", " Reflector"):
            variant = indexed[family + suffix]
            old_delta = variant["target"]["cost"] - base["target"]["cost"]
            new_delta = calculate(catalog, variant)["cost"] - base_cost
            cells.append(f"{old_delta:+g} / {new_delta:+g}")
        lines.append(f"| {family} | {' | '.join(cells)} |")
    lines += ["", "Manufacturing scales hardware increments, never the global CP deduction.",
              "One shared manufacturing factor still leaves visible residuals against legacy prices."]
    lines += ["", "## Experimental combinations without an exact legacy counterpart", "",
              "No legacy-equivalence claim. Rows labeled draft explicitly opt into unverified mounting graphics.", "",
              "| Design | Credits | HP | Speed | Armor | CP / catalog / tech |",
              "|---|---:|---:|---:|---|---|"]
    for design in catalog["designs"]:
        if design.get("target"):
            continue
        try:
            r = calculate(catalog, design)
        except ValueError as error:
            lines.append(f"| {design['name']} | INVALID: {error} | | | | |")
            continue
        lines.append(f"| {design['name']} | {r['cost']:g} | {r['hp']:g} | {r['speed']} | {r['armor']} | {r['cp']} / {r['catalog_points']} / {r['tech']} |")
    lines += ["", "## Auxiliary weapon data (separate from retained main gun)", "",
              "Configured template fields, not simulated DPS or an in-game compositing test.",
              "Gatling columns represent cold ground fire only; its full template also has AA/spin-up.", "",
              "| Design | Weapon / ammunition | Range / minimum cells | Reload ticks | Burst | Fire delay ticks | Raw damage |",
              "|---|---|---|---:|---:|---:|---:|"]
    for design in catalog['designs']:
        a = auxiliary_summary(catalog, design.get('auxiliary_mount'))
        if a:
            lines.append(f"| {design['name']} | {a['weapon']} / {a['ammunition']} | {a['range_cells']} / {a['min_range_cells']} | {a['reload_ticks']} | {a['burst']} | {a['fire_delay_ticks']} | {a['damage']} |")
    lines += ["", "All CP contributions use the same global credit value. No component or design rebates.",
              "The discount applies to every configuration, including cheaper/slower alternatives."]
    return "\n".join(lines) + "\n"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("catalog", type=Path, nargs="?", default=Path(__file__).with_name("catalog.json"))
    args = parser.parse_args()
    print(report(json.loads(args.catalog.read_text(encoding="utf-8"))), end="")
