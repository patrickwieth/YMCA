"""Graphics-locked GDI families. Physical allocations are experimental, not new balance rules."""
import copy


FAMILIES = [
    # id, actor, name, gear, drive, armor, price, hp, speed, turn, hull mass, mount/weapon/payload prices
    ('mammoth', 'Mammoth', 'Mammut', 'designer-heavy-tracks', 'diesel-heavy', 'heavy', 1700, 78000, 52, 16, 18000, (200, 300, 100)),
    ('hmlrs', 'hmlrs', 'Hover-MLRS', 'prototype-hover', 'diesel', 'light', 1150, 18000, 113, 48, 8000, (150, 175, 75)),
    ('disruptor', 'DISR', 'Disruptor', 'tracks-standard', 'diesel', 'heavy', 1500, 75000, 56, 8, 12500, (200, 350, 75)),
    ('mk2', 'MAMMOTHMK2', 'Mammoth Mk II', 'designer-mk2-legs', 'diesel-heavy', 'heavy', 10000, 350000, 35, 8, 30000, (700, 4500, 800)),
]


def additional_assemblies():
    def frame(hull, gear, drive, armor, carrier, weapon, ammo):
        return dict(options=dict(chassis=[hull], running_gear=[gear], drive=[drive],
                    generator=['baseline-generator', 'efficient-generator'], armor=[armor],
                    carrier=[carrier], weapon=[weapon], ammunition=[ammo]), built_in=[])
    result = [frame('juggernaut', 'walker-heavy', 'diesel', 'heavy', 'fixed-triple', 'artillery', 'artillery-shell')]
    for key, _, _, gear, drive, armor, *_ in FAMILIES:
        result.append(frame(f'designer-{key}-hull', gear, drive, armor,
                            f'designer-{key}-mount', f'designer-{key}-weapons', f'designer-{key}-payload'))
    return result


def extend_parts(parts, catalog):
    for key in ('juggernaut', 'walker-heavy', 'fixed-triple', 'artillery', 'artillery-shell', 'diesel-heavy'):
        parts[key] = copy.deepcopy(catalog['components'][key])
    parts['juggernaut']['display_name'] = 'Juggernaut - Artillerielaeufer'
    parts['fixed-triple']['display_name'] = 'Juggernaut Dreifachlafette'
    parts['artillery']['display_name'] = 'Juggernaut Geschuetz- und Zielsystem'
    parts['artillery-shell']['display_name'] = 'Juggernaut Sprenggranaten'
    parts['artillery']['template_locked'] = True
    common = dict(factions=['gdi'], tier=0, cp=0, tech=3, electric_kw=0)
    parts['designer-heavy-tracks'] = dict(common, role='running_gear', display_name='Schwere Ketten (Mammut)',
        mass=3500, cost=200, kind='tracks', locomotor='heavytracked', max_mass=50000, max_speed=90, turn_speed=16)
    parts['designer-mk2-legs'] = dict(common, role='running_gear', display_name='Mk-II-Schreitwerk',
        mass=5000, cost=400, kind='walker', locomotor='heavytracked', max_mass=100000, max_speed=45, turn_speed=8)
    for key, actor, name, gear, drive, armor, price, hp, speed, turn, hull_mass, costs in FAMILIES:
        carrier, weapon, ammo = (f'designer-{key}-{suffix}' for suffix in ('mount', 'weapons', 'payload'))
        parts[carrier] = dict(common, role='carrier', display_name=name + ' Waffentraeger', mass=1000, cost=costs[0],
            weapons=[weapon], burst=1, reload_ticks=1, source=actor,
            note='Cadence is template-locked per channel; these neutral calculator fields do not override weapons.')
        parts[weapon] = dict(common, role='weapon', display_name=name + ' komplettes Waffensystem', mass=1000,
            cost=costs[1], ammunition=[ammo], template_locked=True, source=actor,
            electric_kw=50 if key == 'disruptor' else 0)
        parts[ammo] = dict(common, role='ammunition', display_name=name + ' Waffenpaket', mass=500, cost=costs[2], source=actor,
            note='Full existing payloads, helper weapons and upgrade channels retained. No damage-only substitution.')
        other = [parts[p] for p in (gear, drive, carrier, weapon, ammo, 'baseline-generator')]
        mass = hull_mass + sum(p.get('mass', 0) for p in other)
        demand = 10 + sum(p.get('electric_kw', 0) for p in other)
        parts[f'designer-{key}-hull'] = dict(common, role='chassis', display_name=name + ' - gebundene Einbaugruppe',
            tier=3, hp=hp, mass=hull_mass, cost=price - sum(p.get('cost', 0) for p in other), electric_kw=10,
            carrier_slots=1, equipment_slots=0, equipment=[], default_running_gear=gear, allowed_running_gear=[gear],
            turn_speed_limit=turn, max_mass=parts[gear]['max_mass'], reference_mass=mass,
            reference_kw=parts[drive]['mechanical_kw'] - demand / parts['baseline-generator']['efficiency'],
            reference_speed=speed, max_speed=max(speed, parts[gear]['max_speed']),
            allowed=dict(carrier=[carrier], drive=[drive]), source=actor,
            note='Constructed baseline fit. Hull allocation includes fixed stock abilities (e.g. regeneration); no per-design rebates.')
        if key == 'hmlrs':
            parts[f'designer-{key}-mount']['turret_class'] = 'medium'
            parts[f'designer-{key}-hull']['allowed_turret_classes'] = ['medium']
