"""Explicit Nod bindings; no edits to the broad calibration catalog or Excel snapshots."""
import copy


def additional_assemblies():
    def frame(hull, gear, drive, armor, mount, weapon, payload, built_in=None):
        return dict(faction='nod', options=dict(chassis=[hull], running_gear=[gear], drive=[drive],
            generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=[mount],
            weapon=[weapon], ammunition=[payload]), built_in=built_in or [])
    return [
        frame('nod-light-hull', 'tracks-standard', 'diesel', 'heavy', 'light-cannon-mount', 'light-cannon', 'light-tank-shell'),
        frame('buggy-hull', 'wheels-light', 'diesel-light', 'light', 'scout-mg-mount', 'scout-mg', 'scout-mg-rounds', ['scout-sensors']),
        frame('designer-nod-artillery-hull', 'tracks-light-artillery', 'diesel', 'light', 'field-artillery-mount', 'field-artillery', 'field-artillery-he'),
        frame('designer-ssm-hull', 'designer-ssm-gear', 'diesel', 'light', 'designer-ssm-mount', 'designer-ssm-weapons', 'designer-ssm-payload'),
    ]


def extend_parts(parts, catalog):
    # Same shared generator price/performance, now explicitly admitted for Nod in the native adapter.
    parts['efficient-generator']['factions'].append('nod')
    parts['light-cannon']['template_locked'] = True
    parts['designer-nod-artillery-hull'] = copy.deepcopy(catalog['components']['field-artillery-hull'])
    hull = parts['designer-nod-artillery-hull']
    hull.update(display_name='Nod-Artillerie - gebundene Einbaugruppe', factions=['nod'], source='ARTY.nod')
    for key in ('field-artillery-mount', 'field-artillery', 'field-artillery-he', 'tracks-light-artillery'):
        if 'nod' not in parts[key]['factions']:
            parts[key]['factions'].append('nod')
    parts['field-artillery']['template_locked'] = True
    parts['field-artillery']['source'] = '155mmTD (native Nod binding); complete weapon inherited'
    common = dict(factions=['nod'], tier=0, cp=0, tech=3, electric_kw=0)
    parts['designer-ssm-hull'] = dict(common, role='chassis', display_name='SSM - Napalmraketenwerfer',
        tier=3, mass=8000, cost=450, hp=15000, electric_kw=10, carrier_slots=1, equipment_slots=0,
        default_running_gear='designer-ssm-gear', allowed_running_gear=['designer-ssm-gear'], turn_speed_limit=48,
        max_mass=20000, reference_mass=11600, reference_kw=460, reference_speed=82, max_speed=110,
        allowed=dict(carrier=['designer-ssm-mount'], drive=['diesel']), equipment=[], source='SSM',
        note='Experimental allocation including fixed ammo-pool/reload equipment. Constructed baseline fit, not an independent balance proof.')
    parts['designer-ssm-gear'] = dict(common, role='running_gear', display_name='SSM-Fahrwerk', mass=1500, cost=100,
        locomotor='wheeled', kind='tracks', max_mass=20000, max_speed=150, turn_speed=48,
        note='Preserves stock SSM wheeled locomotor despite tracked artwork.')
    parts['designer-ssm-mount'] = dict(common, role='carrier', display_name='SSM-Doppelstartlafette', mass=600, cost=150,
        weapons=['designer-ssm-weapons'], burst=1, reload_ticks=1,
        note='Neutral calculator cadence fields, never emitted. Original ammo pool and reload traits are authoritative.')
    parts['designer-ssm-weapons'] = dict(common, role='weapon', display_name='SSM-Napalmraketen', mass=500, cost=175,
        ammunition=['designer-ssm-payload'], template_locked=True, source='HonestJohn')
    parts['designer-ssm-payload'] = dict(common, role='ammunition', display_name='SSM-Napalmgefechtskopf', mass=400, cost=75,
        source='HonestJohn', note='Full projectile, warheads and effects inherited; not scalar damage emulation.')
