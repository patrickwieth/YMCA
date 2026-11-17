"""England bindings. Stock weapons/doctrines are retained, not generic damage substitutes."""
import copy


def additional_assemblies():
    def frame(hull, gear, drive, armor, mount, weapon, ammo, built_in=None):
        return dict(faction='allies', options=dict(chassis=[hull], running_gear=[gear], drive=[drive],
            generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=[mount],
            weapon=[weapon], ammunition=[ammo]), built_in=built_in or [])
    return [
        frame('allied-medium-hull', 'tracks-standard', 'diesel', 'heavy', 'medium-cannon-mount', 'medium-cannon', 'medium-tank-shell'),
        frame('designer-ranger-hull', 'wheels-light', 'diesel-light', 'light', 'designer-ranger-mount', 'scout-mg', 'scout-mg-rounds', ['scout-sensors']),
        frame('field-artillery-hull', 'tracks-light-artillery', 'diesel', 'light', 'field-artillery-mount', 'field-artillery', 'field-artillery-he'),
        frame('designer-prism-hull', 'designer-prism-gear', 'diesel', 'light', 'designer-prism-mount', 'designer-prism-weapon', 'designer-prism-payload'),
    ]


def extend_parts(parts):
    for key in ('efficient-generator', 'scout-sensors', 'scout-mg', 'scout-mg-rounds'):
        if 'allies' not in parts[key]['factions']:
            parts[key]['factions'].append('allies')
    parts['allied-medium-hull'].update(display_name='Challenger - alliierter Kampfpanzer', source='Challenger_Tank / 2TNK',
        note='Raw baseline; inherited Armored Doctrine production discount remains active in-game.')
    parts['field-artillery-hull']['display_name'] = 'Feldartillerie - alliierte Ausfuehrung'
    parts['field-artillery']['source'] = '155mm / 155mmTD; complete faction-specific weapon inherited'
    hull = parts['designer-ranger-hull'] = copy.deepcopy(parts['humvee-hull'])
    hull.update(display_name='Ranger - MG-Spaehwagen', factions=['allies'], turn_speed_limit=80, source='JEEP',
        allowed=dict(carrier=['designer-ranger-mount'], drive=['diesel-light']), allowed_running_gear=['wheels-light'],
        note='Shared scout allocations, mandatory sensors counted once. Ranger retains its own MG report and 48-turn turret.')
    mount = parts['designer-ranger-mount'] = copy.deepcopy(parts['scout-mg-mount'])
    mount.update(display_name='Ranger-MG-Lafette', factions=['allies'], turn_speed_reference=48, source='JEEP / M60mg')
    common = dict(factions=['allies'], tier=0, cp=0, tech=2, electric_kw=0)
    parts['designer-prism-hull'] = dict(common, role='chassis', display_name='Prism-Tank - gebundene Einbaugruppe',
        tier=2, mass=8000, cost=575, hp=22000, electric_kw=10, carrier_slots=1, equipment_slots=0, equipment=[],
        default_running_gear='designer-prism-gear', allowed_running_gear=['designer-prism-gear'], turn_speed_limit=48,
        max_mass=20000, reference_mass=11600, reference_kw=320, reference_speed=82, max_speed=110,
        allowed=dict(carrier=['designer-prism-mount'], drive=['diesel']), source='Prismtank',
        note='Constructed baseline fit, experimental mass/power allocations. Doctrine discount and Prism Tech are inherited, not simulated here.')
    parts['designer-prism-gear'] = dict(common, role='running_gear', display_name='Prism-Fahrwerk', mass=1500, cost=100,
        kind='tracks', locomotor='wheeled', max_mass=20000, max_speed=150, turn_speed=48,
        note='Preserves stock wheeled locomotor and Light armor despite tank artwork.')
    parts['designer-prism-mount'] = dict(common, role='carrier', display_name='Prism-Strahlenturm', mass=500, cost=150,
        weapons=['designer-prism-weapon'], burst=1, reload_ticks=45, source='Prismtank')
    parts['designer-prism-weapon'] = dict(common, role='weapon', display_name='Prism-Strahlsystem', mass=700, cost=350,
        electric_kw=35, ammunition=['designer-prism-payload'], template_locked=True, source='PrisTLaser',
        note='Full LaserZap and FireCluster secondary-beam package. No damage-only rewrite.')
    parts['designer-prism-payload'] = dict(common, role='ammunition', display_name='Prism-Energiepaket', mass=300, cost=75,
        source='PrisTLaser / PrisTBurst', note='Complete effects inherited; allocation does not emulate ammunition consumption.')
