"""Explicit China assemblies; full legacy behaviors remain in inherited engine traits."""


def additional_assemblies():
    def frame(hull, gear, drives, mount, weapon, payload):
        return dict(faction='china', options=dict(chassis=[hull], running_gear=[gear], drive=drives,
            generator=['baseline-generator', 'efficient-generator'], armor=['heavy'], carrier=[mount],
            weapon=[weapon], ammunition=[payload]), built_in=[])
    return [
        frame('battlemaster', 'tracks-standard', ['diesel', 'diesel-large'], 'cannon-turret', 'cannon', 'shell'),
        frame('dragon-chassis', 'tracks-standard', ['diesel', 'diesel-large'], 'flame-turret', 'dragon-flamer', 'flame-fuel'),
        frame('gatling-chassis', 'tracks-standard', ['diesel', 'diesel-large'], 'gatling-turret', 'gatling-gun', 'gatling-rounds'),
        frame('overlord-chassis', 'tracks-superheavy', ['diesel-heavy', 'diesel-heavy-boost'], 'heavy-twin-turret', 'overlord-cannon', 'heavy-shell'),
    ]


def extend_parts(parts):
    for key, label in {
        'battlemaster': 'Battlemaster - Type 59',
        'dragon-chassis': 'Dragon - Flammenpanzer',
        'gatling-chassis': 'Gatling - Boden und Flugabwehr',
        'overlord-chassis': 'Overlord - schwere Einbaugruppe',
        'cannon-turret': 'Battlemaster-Kanonenturm',
        'flame-turret': 'Dragon-Flammenturm',
        'dragon-flamer': 'Dragon-Flammenwerfer und Firewall',
        'flame-fuel': 'Flammenwerfer-Brennstoff',
        'gatling-turret': 'Gatling-Turm',
        'gatling-gun': 'Gatling Boden und Luft - alle Drehzahlstufen',
        'gatling-rounds': 'Gatling-Munition',
    }.items():
        parts[key]['display_name'] = label
    for key in ('cannon', 'dragon-flamer', 'gatling-gun', 'overlord-cannon'):
        parts[key]['template_locked'] = True
        parts[key]['note'] = ('Full native weapon channels inherited, including conditional stages/upgrades. '
                             'Scalar weapon fields are reference metadata only, not emitted overrides.')
    parts['dragon-chassis']['note'] = 'Native binding retains Firewall deploy/cycle and Black Napalm conditions.'
    parts['gatling-chassis']['note'] = 'Native binding retains independent ground/AA spin-up and inherited sight.'
    parts['overlord-chassis']['note'] = ('Base scalar values; China Tank General activates the inherited Emperor artwork and '
        '80% received-damage modifier. Effective durability is not displayed raw HP. Roof modules are not admitted here.')
    # Do not advertise unimplemented roof/equipment composition in the native-only snapshot.
    for assembly in additional_assemblies():
        hull = parts[assembly['options']['chassis'][0]]
        hull.pop('auxiliary_mount', None)
        hull['carrier_slots'] = 1
        hull['equipment_slots'] = 0
        hull['equipment'] = []
