"""Further GDI combat bindings, with explicit experimental hardware allocations."""

FAMILIES = [
    # id, actor, label, gear, motor, armor, hull cost/mass, mount cost/mass, weapon cost/mass, payload cost/mass, HP/speed/turn
    ('titan', 'TITN', 'Titan', 'walker-heavy', 'diesel', 'heavy', (800, 13000), (300, 1000), (300, 1200), (200, 1200), 100000, 50, 16),
    ('slingshot', 'SLNG', 'Slingshot', 'gdi-light-hover', 'diesel-light', 'light', (165, 1500), (50, 100), (100, 100), (25, 100), 13500, 133, 48),
    ('marv', 'MARV', 'MARV', 'tracks-superheavy', 'diesel-heavy', 'heavy', (4250, 25000), (700, 2000), (3500, 2000), (1000, 1000), 200000, 40, 9),
]


def additional_assemblies():
    return [dict(options=dict(chassis=[f'designer-{key}-hull'], running_gear=[gear], drive=[motor],
                generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=[f'designer-{key}-mount'],
                weapon=[f'designer-{key}-weapon'], ammunition=[f'designer-{key}-payload']), built_in=[])
            for key, _, _, gear, motor, armor, *_ in FAMILIES]


def extend_parts(parts):
    common = dict(factions=['gdi'], tier=0, cp=0, tech=3, electric_kw=0)
    parts['gdi-light-hover'] = dict(common, role='running_gear', display_name='Leichtes GDI-Schwebewerk', mass=500, cost=150,
        electric_kw=10, kind='hover', locomotor='lighthover', max_mass=6000, max_speed=180, turn_speed=48)
    for key, actor, label, gear, motor, armor, hull, mount, weapon, payload, hp, speed, turn in FAMILIES:
        ids = [f'designer-{key}-{s}' for s in ('mount', 'weapon', 'payload')]
        for part_id, role, allocation in zip(ids, ('carrier', 'weapon', 'ammunition'), (mount, weapon, payload)):
            parts[part_id] = dict(common, role=role, display_name=label + ' ' + role, cost=allocation[0], mass=allocation[1], source=actor)
        parts[ids[0]].update(weapons=[ids[1]], burst=1, reload_ticks=1,
            note='Neutral calculator fields. Per-channel stock cadence/delay is inherited without overrides.')
        parts[ids[1]].update(ammunition=[ids[2]], template_locked=True, electric_kw=50 if key == 'marv' else 0)
        other = [parts[p] for p in [gear, motor, 'baseline-generator'] + ids]
        demand = 10 + sum(p.get('electric_kw', 0) for p in other)
        mass = hull[1] + sum(p.get('mass', 0) for p in other)
        parts[f'designer-{key}-hull'] = dict(common, role='chassis', display_name=label + ' - gebundene Einbaugruppe',
            tier=3, cost=hull[0], mass=hull[1], hp=hp, electric_kw=10, carrier_slots=1, equipment_slots=0, equipment=[],
            default_running_gear=gear, allowed_running_gear=[gear], turn_speed_limit=turn, max_mass=parts[gear]['max_mass'],
            reference_mass=mass, reference_kw=parts[motor]['mechanical_kw'] - demand / parts['baseline-generator']['efficiency'],
            reference_speed=speed, max_speed=parts[gear]['max_speed'], allowed=dict(carrier=[ids[0]], drive=[motor]), source=actor,
            note='Constructed baseline fit. Includes bound stock abilities; mass/power/cost split remains experimental. No per-design rebates.')
        if key in ('titan', 'marv'):
            turret_class = 'heavy-walker' if key == 'titan' else 'super-heavy'
            parts[ids[0]]['turret_class'] = turret_class
            parts[f'designer-{key}-hull']['allowed_turret_classes'] = [turret_class]
