"""Remaining China combat base families, including actual attacking cargo and spawned missiles."""
from nod_combat_expansion import add_families

FAMILIES = [
    ('inferno', 'charty', 'Inferno Cannon', 'light-tracks', 'diesel', 'light', 900, 12000, 90, 48, 2, 200),
    ('crawler', 'chcrawl2', 'Heavy Troop Crawler', 'tracks-standard', 'diesel', 'heavy', 1000, 45000, 125, 48, 1, 150),
    ('nuke', 'chnukecann', 'Nuke Cannon', 'tracks-standard', 'diesel', 'heavy', 2400, 24000, 55, 8, 3, 600),
    ('bixi', 'Bixi', 'Bixi Dragon', 'light-tracks', 'diesel', 'light', 900, 16000, 56, 8, 3, 200),
]


def additional_assemblies():
    return [dict(faction='china', options=dict(chassis=[f'china-combat-{key}-hull'], running_gear=[gear], drive=[motor],
        generator=['baseline-generator', 'efficient-generator'], armor=[armor], carrier=['integrated-mount'],
        weapon=[f'china-combat-{key}-weapon'], ammunition=['integral-stores']), built_in=[])
        for key, _, _, gear, motor, armor, *_ in FAMILIES]


def extend_parts(parts):
    add_families(parts, FAMILIES, 'china', 'china-combat')
    parts['china-combat-crawler-weapon']['display_name'] = 'Original Attacking Cargo Package'
    parts['china-combat-crawler-weapon']['note'] = 'No intrinsic gun added. Original passengers, ports, passenger conditions and infantry weapons remain authoritative.'
