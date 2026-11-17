"""Russia adapter. Experimental hardware allocations; complete Soviet weapon traits inherited."""


def additional_assemblies():
    def frame(hull, gear, drive, armor, mount, weapon, ammo):
        return dict(faction='soviet', options=dict(chassis=[hull], running_gear=[gear], drive=[drive],
            generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=[mount],
            weapon=[weapon], ammunition=[ammo]), built_in=[])
    return [
        frame('soviet-heavy-hull', 'tracks-standard', 'diesel', 'heavy', 'soviet-twin-mount', 'medium-cannon', 'medium-tank-shell'),
        frame('t34-hull', 'tracks-standard', 'diesel', 'heavy', 'light-cannon-mount', 'light-cannon', 'light-tank-shell'),
        frame('heavy-tesla', 'tracks-standard', 'diesel', 'heavy', 'twin-coil', 'tesla', 'discharge'),
        frame('designer-flak-hull', 'designer-flak-gear', 'diesel-light', 'light', 'designer-flak-mount', 'designer-flak-weapon', 'designer-flak-payload'),
    ]


def extend_parts(parts):
    parts['soviet-heavy-hull']['display_name'] = 'Schwerer Panzer - Zwillingskanone'
    parts['t34-hull']['display_name'] = 'T-34 - leichter Kampfpanzer'
    parts['t34-hull']['note'] = 'Custom Soviet roster admits this North Korean hull. Both standard and conditional cluster weapons inherited.'
    parts['heavy-tesla']['display_name'] = 'Tesla-Panzer - schwere Doppelspule'
    parts['twin-coil']['display_name'] = 'Tesla-Doppelspulenturm'
    parts['tesla'].update(display_name='Tesla-Strahlsystem', template_locked=True,
        note='Full TTankZapMK2 behavior retained, not damage-only substitution.')
    parts['discharge']['display_name'] = 'Tesla-Entladungspaket'
    common = dict(factions=['soviet'], tier=0, cp=0, tech=1, electric_kw=0)
    parts['designer-flak-hull'] = dict(common, role='chassis', display_name='Flak-Laster - Boden und Luft',
        tier=1, mass=3000, cost=200, hp=15000, electric_kw=5, carrier_slots=1, equipment_slots=0, equipment=[],
        default_running_gear='designer-flak-gear', allowed_running_gear=['designer-flak-gear'], turn_speed_limit=48,
        max_mass=8000, reference_mass=4350, reference_kw=230, reference_speed=118, max_speed=145,
        allowed=dict(carrier=['designer-flak-mount'], drive=['diesel-light']), source='FTRK',
        note='Constructed baseline fit; physical allocations experimental, no per-design rebate.')
    parts['designer-flak-gear'] = dict(common, role='running_gear', display_name='Flak-Fahrwerk', mass=500, cost=65,
        kind='wheels', locomotor='wheeled', max_mass=8000, max_speed=150, turn_speed=48, source='FTRK / ^Vehicle')
    parts['designer-flak-mount'] = dict(common, role='carrier', display_name='Flak-Zwillingslafette', mass=200, cost=50,
        weapons=['designer-flak-weapon'], burst=1, reload_ticks=1,
        note='Neutral calculator fields; original independent weapon cadences are not overridden.')
    parts['designer-flak-weapon'] = dict(common, role='weapon', display_name='Flak Boden und Luft', mass=250, cost=100,
        ammunition=['designer-flak-payload'], template_locked=True, source='FLAK-23-AA / FLAK-23-AG')
    parts['designer-flak-payload'] = dict(common, role='ammunition', display_name='Flak-Geschosse', mass=150, cost=25,
        source='FLAK-23-AA / FLAK-23-AG', note='Independent target masks, projectiles, spread and effects preserved.')
