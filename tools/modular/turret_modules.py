"""Source-qualified turret modules. Existing numeric allocations remain unchanged.
A module family is NOT permission to mix weapon profiles or bolt artwork onto other hulls.
"""
import copy
from functools import lru_cache
from survey_loadouts import load, selected

MODULES = {
    'prism-turret': dict(label='Prism Turret', family='prism', actor='Prismtank', hull='designer-prism-hull', legacy='designer-prism-mount', weapon='designer-prism-weapon'),
    'artillery-turret': dict(label='Artillery Turret', family='artillery', actor='HOWI', hull='nod-combat-howitzer-hull', legacy='integrated-mount', weapon='nod-combat-howitzer-weapon'),
    'missile-turret': dict(label='Missile Turret', family='missile', actor='STNK', hull='nod-combat-stealth-hull', legacy='integrated-mount', weapon='nod-combat-stealth-weapon'),
    'sonic-turret': dict(label='Sonic Turret', family='sonic', actor='DISR', hull='designer-disruptor-hull', legacy='designer-disruptor-mount', weapon='designer-disruptor-weapons'),
}


def canonical(hull, carrier):
    return next((id for id, m in MODULES.items() if hull == m['hull'] and carrier in (m['legacy'], id)), carrier)


@lru_cache(maxsize=1)
def recipes():
    rows = {r['actor']: r for r in load()['actors']}
    result = {}
    for id, m in MODULES.items():
        row = rows[m['actor']]
        result[id] = dict(family=m['family'], source_actor=m['actor'], slots_required=1,
            native_hulls=[m['hull']], arbitrary_mounting=False,
            turrets=copy.deepcopy(selected(row, 'Turreted')),
            armaments=copy.deepcopy(selected(row, 'Armament')),
            graphics=copy.deepcopy([t for t in row['traits'] if t['type'].startswith(('RenderSprites','RenderVoxels','WithSpriteTurret','WithVoxelTurret','WithVoxelBarrel'))]))
    return result


def extend_parts(parts):
    for id, m in MODULES.items():
        part = copy.deepcopy(parts[m['legacy']])
        part.update(display_name=m['label'], module_family=m['family'], source=m['actor'], slots_required=1,
            weapons=[m['weapon']], factions=parts[m['hull']]['factions'][:],
            note='One turret slot. Source-qualified complete weapon/render package; existing pricing/mass/power allocation unchanged. Other chassis require explicit mounting/graphics bindings.')
        parts[id] = part
    for m in MODULES.values():
        if m['legacy'] != 'integrated-mount': del parts[m['legacy']]
