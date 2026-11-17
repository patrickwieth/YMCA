"""Complete base Nod ground-combat families; not promoted/national variants.
Allocations are constructed baseline fits, not independent physics/economics validation.
Shared mount/stores represent hardware only; complete weapon and ability behavior is inherited.
"""

# key, actor, label, gear, motor, armor, price, HP, speed, turn, tech, weapon allocation
FAMILIES = [
    ('apc', 'APC2', 'Armored Personnel Carrier', 'tracks-standard', 'diesel', 'heavy', 600, 30000, 135, 48, 1, 100),
    ('bike', 'BIKE', 'Recon Bike', 'wheels-light', 'diesel-light', 'light', 500, 11000, 180, 80, 1, 100),
    ('beam', 'Beam_Cannon', 'Beam Cannon', 'light-tracks', 'diesel', 'light', 1250, 24000, 100, 48, 3, 300),
    ('flame', 'FTNK', "Devil's Tongue", 'tracks-standard', 'diesel', 'heavy', 700, 40000, 82, 48, 2, 150),
    ('heavy-flame', 'HFTK', 'Heavy Flame Tank', 'designer-heavy-tracks', 'diesel', 'heavy', 1000, 75000, 68, 48, 2, 200),
    ('howitzer', 'HOWI', 'Howitzer', 'tracks-light-artillery', 'diesel', 'light', 550, 15000, 68, 16, 1, 100),
    ('specter', 'SPEC', 'Specter', 'tracks-light-artillery', 'diesel', 'light', 1100, 11000, 100, 48, 3, 200),
    ('stealth', 'STNK', 'Stealth Tank', 'light-tracks', 'diesel', 'light', 1200, 20000, 135, 48, 2, 250),
    ('chemical', 'TTRK', 'Chemical Truck', 'wheels-light', 'diesel-light', 'light', 1200, 10000, 92, 48, 3, 350),
    ('microwave', 'WTNK', 'Microwave Tank', 'light-tracks', 'diesel', 'light', 1250, 35000, 100, 48, 3, 300),
]


def additional_assemblies():
    return [dict(faction='nod', options=dict(chassis=[f'nod-combat-{key}-hull'], running_gear=[gear], drive=[motor],
        generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=['integrated-mount'],
        weapon=[f'nod-combat-{key}-weapon'], ammunition=['integral-stores']), built_in=[])
        for key, _, _, gear, motor, armor, *_ in FAMILIES]


def extend_parts(parts):
    common = dict(factions=['nod'], tier=0, cp=0, tech=1, electric_kw=0)
    parts['integrated-mount'] = dict(common, role='carrier', display_name='Integrated Mount', cost=50, mass=500,
        weapons=[f'nod-combat-{f[0]}-weapon' for f in FAMILIES], burst=1, reload_ticks=1,
        note='Shared installation hardware. Bound actor provides its actual mount, cadence, offsets and targeting; no scalar overrides.')
    parts['integral-stores'] = dict(common, role='ammunition', display_name='Integral Stores', cost=50, mass=400,
        note='Shared storage allocation, NOT interchangeable ammunition. Payload/warheads stay locked to the complete weapon package.')
    # Capacity headroom, not a turn-rate buff: every existing hull keeps its original turn limit.
    parts['designer-heavy-tracks']['turn_speed'] = 48
    parts['designer-heavy-tracks']['tech'] = 1
    add_families(parts, FAMILIES, 'nod', 'nod-combat')


def add_families(parts, families, faction, prefix):
    common = dict(factions=[faction], tier=0, cp=0, tech=1, electric_kw=0)
    for key, actor, label, gear, motor, armor, price, hp, speed, turn, tech, weapon_cost in families:
        for part in (gear, motor, armor, 'integrated-mount', 'integral-stores'):
            if faction not in parts[part]['factions']: parts[part]['factions'] = parts[part]['factions'] + [faction]
        weapon = f'{prefix}-{key}-weapon'
        if weapon not in parts['integrated-mount']['weapons']: parts['integrated-mount']['weapons'].append(weapon)
        parts[weapon] = dict(common, role='weapon', display_name=label + ' - Original Weapon Package', cost=weapon_cost,
            mass=100 if key == 'bike' else 600, ammunition=['integral-stores'], template_locked=True, source=actor)
        others = [parts[p] for p in (gear, motor, 'baseline-generator', 'integrated-mount', weapon, 'integral-stores')]
        hull_mass = 1500 if motor == 'diesel-light' else 8000
        mass = hull_mass + sum(p.get('mass', 0) for p in others)
        demand = 10 + sum(p.get('electric_kw', 0) for p in others)
        parts[f'{prefix}-{key}-hull'] = dict(common, role='chassis', display_name=label, tier=tech, tech=tech,
            cost=price - sum(p.get('cost', 0) for p in others), mass=hull_mass, hp=hp, electric_kw=10,
            carrier_slots=1, equipment_slots=0, equipment=[], default_running_gear=gear, allowed_running_gear=[gear],
            turn_speed_limit=turn, max_mass=parts[gear]['max_mass'], max_speed=parts[gear]['max_speed'],
            reference_mass=mass, reference_kw=parts[motor]['mechanical_kw'] - demand / parts['baseline-generator']['efficiency'],
            reference_speed=speed, allowed=dict(carrier=['integrated-mount'], drive=[motor]), source=actor,
            note='Constructed native baseline fit. Hull includes fixed cargo/cloak/deployment/special hardware as applicable. No arbitrary mounts or payloads enabled.')
