"""Offline design calculator. No engine rules are generated or modified."""
import argparse
import json
import math
from pathlib import Path

ROLES = {"chassis", "drive", "generator", "armor", "carrier", "weapon", "ammunition"}


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
    if set(by_role) != ROLES:
        raise ValueError("Select exactly one component for every required role")
    chassis = by_role["chassis"]
    if chassis["tier"] < 1:
        raise ValueError("Chassis cannot be tier zero")
    if chassis["carrier_slots"] != 1:
        raise ValueError("Prototype supports one carrier only")
    for role, allowed in chassis["allowed"].items():
        if not any(key in allowed and catalog["components"][key]["role"] == role for key in ids):
            raise ValueError("Incompatible chassis " + role)
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
    for part in parts:
        if any(key in ids for key in part.get("excludes", [])):
            raise ValueError("Mutually exclusive components")
    armor = by_role["armor"]
    others = [p for p in parts if p["role"] not in ("chassis", "armor")]
    mass = chassis["mass"] * armor["mass_percent"] / 100 + sum(p["mass"] for p in others)
    cost = chassis["cost"] * armor["cost_percent"] / 100 + sum(p["cost"] for p in others)
    gross_cost = cost
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
    if reserve <= 0:
        raise ValueError("No mechanical driving reserve")
    if mass <= 0 or mass > chassis["max_mass"]:
        raise ValueError("Invalid mass or chassis load exceeded")
    ratio = (reserve / mass) / (chassis["reference_kw"] / chassis["reference_mass"])
    speed = min(chassis["max_speed"], chassis["reference_speed"] * ratio ** catalog["alpha"])
    return dict(mass=mass, hp=hp, cost=cost, gross_cost=gross_cost, discount=discount,
                electric_kw=demand, reserve_kw=reserve,
                speed=math.floor(speed + 0.5), armor=armor["armor_type"],
                catalog_points=sum(p["tier"] for p in parts), cp=sum(p["cp"] for p in parts),
                tech=max(p["tech"] for p in parts),
                burst=carrier["burst"], burst_delay_ticks=carrier.get("burst_delay_ticks", 0),
                reload_ticks=carrier["reload_ticks"],
                range_cells=by_role["weapon"].get("range_cells"),
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
             "| Reference / configured candidate | Credits old -> new | HP old -> new | Speed old -> new | New CP / catalog / tech | Gross - discount = price |",
             "|---|---|---|---|---|---|"]
    for design in catalog["designs"]:
        if not design.get("target"):
            continue
        r = calculate(catalog, design)
        target = design["target"]
        cells = " | ".join(comparison(target[k], r[k]) for k in ("cost", "hp", "speed"))
        lines.append(f"| {design['name']} | {cells} | {r['cp']} / {r['catalog_points']} / {r['tech']} | {r['gross_cost']:g} - {r['discount']:g} = {r['cost']:g} |")
    lines += ["", "## Hardware cost breakdown", "",
              "Armor modifies chassis price before hardware is summed. Credits are not deducted per component.", "",
              "| Reference candidate | Hardware contributions (credits) | Total - CP credit = price |",
              "|---|---|---|"]
    for design in catalog["designs"]:
        if not design.get("target"):
            continue
        r = calculate(catalog, design)
        parts = [(key, catalog["components"][key]) for key in design["components"]]
        armor = next(p for _, p in parts if p["role"] == "armor")
        contributions = []
        for key, p in parts:
            if p["role"] == "armor":
                continue
            cost = p["cost"] * armor["cost_percent"] / 100 if p["role"] == "chassis" else p["cost"]
            contributions.append(f"{key}: {cost:g}")
        lines.append(f"| {design['name']} | {'; '.join(contributions)} | {r['gross_cost']:g} - {r['cp']} x {catalog['credits_per_cp']:g} = {r['cost']:g} |")
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
    for family in ("Battlemaster Autoloader", "Battlemaster Nuclear Shells", "Battlemaster Mass Production"):
        base = indexed[family]
        base_cost = calculate(catalog, base)["cost"]
        cells = []
        for suffix in (" PDL", " Reflector"):
            variant = indexed[family + suffix]
            old_delta = variant["target"]["cost"] - base["target"]["cost"]
            new_delta = calculate(catalog, variant)["cost"] - base_cost
            cells.append(f"{old_delta:+g} / {new_delta:+g}")
        lines.append(f"| {family} | {' | '.join(cells)} |")
    lines += ["", "With identical added hardware and CP, an additive model yields the same increment.",
              "Changing the global CP value cannot make that same PDL package cost both +650 and +400."]
    lines += ["", "## Experimental combinations without an exact legacy counterpart", "",
              "These are not claims of matching existing PDL/autoloader/reflector variants.", "",
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
    lines += ["", "All CP contributions use the same global credit value. No component or design rebates.",
              "The discount applies to every configuration, including cheaper/slower alternatives."]
    return "\n".join(lines) + "\n"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("catalog", type=Path, nargs="?", default=Path(__file__).with_name("catalog.json"))
    args = parser.parse_args()
    print(report(json.loads(args.catalog.read_text(encoding="utf-8"))), end="")
