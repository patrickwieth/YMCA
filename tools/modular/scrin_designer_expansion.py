"""Scrin native adapter. An alien drive/converter is an experimental calculator abstraction,
not a claim that stock Scrin vehicles consume diesel or simulate an electrical network."""
import copy

FAMILIES = [
    # key, actor, label, gear, hull cost, weapon cost, HP, speed, turn, tech
    ('gunwalker', 'GUNW', 'Gun Walker', 'scrin-walker-gear', 250, 100, 30000, 113, 8, 1),
    ('seeker', 'SEEK', 'Seeker', 'scrin-hover-gear', 250, 150, 20000, 135, 48, 1),
    ('corrupter', 'CORR', 'Corrupter', 'scrin-walker-gear', 250, 150, 45000, 82, 24, 2),
    ('devourer', 'DEVO', 'Devourer', 'scrin-hover-gear', 500, 350, 35000, 90, 1000, 2),
]


def additional_assemblies():
    return [dict(faction='scrin', options=dict(chassis=[f'scrin-{key}-hull'], running_gear=[gear],
                drive=['scrin-drive'], generator=['scrin-converter', 'scrin-converter-efficient'], armor=['light'],
                carrier=[f'scrin-{key}-mount'], weapon=[f'scrin-{key}-weapon'], ammunition=[f'scrin-{key}-payload']), built_in=[])
            for key, _, _, gear, *_ in FAMILIES]


def extend_parts(parts):
    parts['light']['factions'].append('scrin')
    for new, old, label in [('scrin-drive', 'diesel', 'Scrin-Antriebsmodul'),
                           ('scrin-converter', 'baseline-generator', 'Scrin-Grundkonverter'),
                           ('scrin-converter-efficient', 'efficient-generator', 'Scrin-Effizienzkonverter')]:
        parts[new] = copy.deepcopy(parts[old])
        parts[new].update(factions=['scrin'], display_name=label,
            note='Provisional equivalent mechanical/electrical budget. Same shared numeric ratings/prices; no in-game fuel/electrical simulation.')
    common = dict(factions=['scrin'], tier=0, cp=0, tech=1, electric_kw=0)
    parts['scrin-walker-gear'] = dict(common, role='running_gear', display_name='Scrin-Laufwerk', mass=1500, cost=100,
        kind='walker', locomotor='wheeled', max_mass=20000, max_speed=150, turn_speed=48,
        note='Stock GUNW/CORR use wheeled engine terrain/crushing despite walking animation.')
    parts['scrin-hover-gear'] = dict(common, role='running_gear', display_name='Scrin-Leichtschwebewerk', mass=1500, cost=200,
        electric_kw=10, kind='hover', locomotor='lighthover', max_mass=20000, max_speed=170, turn_speed=1024,
        note='Complete stock LightHoverVehicle inherited; Devourer keeps its 1000 body turn rate.')
    for key, actor, label, gear, hull_cost, weapon_cost, hp, speed, turn, tech in FAMILIES:
        mount, weapon, payload = (f'scrin-{key}-{suffix}' for suffix in ('mount', 'weapon', 'payload'))
        parts[mount] = dict(common, role='carrier', display_name=label + ' Waffentraeger', mass=400, cost=50,
            weapons=[weapon], burst=1, reload_ticks=1, note='Neutral calculator fields, not emitted weapon cadence.')
        parts[weapon] = dict(common, role='weapon', display_name=label + ' Waffensystem', mass=500, cost=weapon_cost,
            electric_kw=10, ammunition=[payload], template_locked=True, source=actor,
            note='Full original weapons/projectiles/warheads retained. Static demand is an experimental allocation.')
        parts[payload] = dict(common, role='ammunition', display_name=label + ' Waffenpaket', mass=200, cost=50, source=actor)
        other = [parts[p] for p in (gear, 'scrin-drive', mount, weapon, payload, 'scrin-converter')]
        mass = 5000 + sum(p.get('mass', 0) for p in other)
        demand = 10 + sum(p.get('electric_kw', 0) for p in other)
        parts[f'scrin-{key}-hull'] = dict(common, role='chassis', display_name=label + ' - Scrin-Einbaugruppe',
            tier=tech, tech=tech, mass=5000, cost=hull_cost, hp=hp, electric_kw=10, carrier_slots=1,
            equipment_slots=0, equipment=[], default_running_gear=gear, allowed_running_gear=[gear],
            turn_speed_limit=turn, max_mass=20000, reference_mass=mass,
            reference_kw=500 - demand / parts['scrin-converter']['efficiency'], reference_speed=speed, max_speed=170,
            allowed=dict(carrier=[mount], drive=['scrin-drive']), source=actor,
            note='Constructed baseline fit, not independently established economics or physical specifications. All four use stock Light armor.')
