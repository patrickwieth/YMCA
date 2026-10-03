"""Reviewed remaining combat families: complete stock armaments stay on the chassis.
No damage-only emulation, no per-design price overrides, no freely mixable stock arsenals.
"""
import json
from pathlib import Path

SOURCE = Path(__file__).with_name('stock-combat-baselines.json')
ROWS = json.loads(SOURCE.read_text(encoding='utf-8'))
EXPECTED = {'1TNK', 'APC', 'BATF.Bunker', 'CHPR', 'CRYO', 'CTNK', 'IFV', 'RTNK', 'TNKD',
    'GDRN', 'MDRN', 'VULC', 'XO', 'ATMZ', 'Channeler', 'Hexapod', 'INTL', 'LACE', 'LCHR', 'RPTP', 'RUIN', 'STCR', 'TPOD',
    '2S3', 'BTR', 'Chem_Sprayer', 'DTRK', 'Devil_Tank', 'Gene_Splicer', 'HQ7', 'HTK5', 'Hyena', 'ISU', 'KATY', 'NonaSVK',
    'Peoples_Tank', 'QTNK', 'Rice_Cooker', 'Source_of_Pollution', 'TTNK', 'Tsar_Tank', 'V3RL', 'apoc', 'kims_wheel'}
assert {r['actor'] for r in ROWS} == EXPECTED and len(ROWS) == 44


def key(row):
    return 'stock-' + ('batf' if row['actor'] == 'BATF.Bunker' else row['actor'].lower().replace('_', '-').replace('.', '-'))


def gear(row):
    if row['actor'] in ('TPOD', 'RPTP'): return 'designer-mk2-legs'
    if row['actor'] == 'Hexapod': return 'walker-heavy'
    if row['actor'] == 'XO': return 'scrin-walker-gear'
    if row['actor'] in ('CRYO', 'TTNK'): return 'light-tracks'
    return dict(tracked='tracks-standard', heavytracked='designer-heavy-tracks', sheavytracked='tracks-superheavy',
        wheeled='wheels-light', heavywheeled='heavy-wheels', seal='amphibious-micro', hover='prototype-hover', lighthover='scrin-hover-gear')[row['locomotor']]


def motor(row):
    return 'scrin-drive' if row['faction'] == 'scrin' else 'diesel-light' if gear(row) in ('wheels-light', 'amphibious-micro') else 'diesel'


def generator(row):
    return 'scrin-converter' if row['faction'] == 'scrin' else 'baseline-generator'


def prerequisites(row):
    # Explicit custom-roster national admission. Keep technology/promotions/negative replacements.
    replacement = {'~zocom': '~gdi', '~talon': '~gdi', '~germany': '~allies',
        '~reaper': '~scrin', '~harvester': '~scrin', '~vehicles.nkorea': '~vehicles.soviet', '~vehicles.iraq': '~vehicles.soviet'}
    values = list(dict.fromkeys(replacement.get(p, p) for p in row['prerequisites']))
    if '~disabled' in values or '~botplayer' in values: raise ValueError('Disabled/helper actor is not a player design: ' + row['actor'])
    return ', '.join(values)


def carrier(row):
    return 'superheavy-bunker' if row['actor'] == 'BATF.Bunker' else 'integrated-mount'


def additional_assemblies():
    return [dict(faction=r['faction'], options=dict(chassis=[key(r)+'-hull'], running_gear=[gear(r)], drive=[motor(r)],
        generator=[generator(r)], armor=[r['armor'].lower()], carrier=[carrier(r)],
        weapon=['original-armament'], ammunition=['integral-stores']), built_in=[]) for r in ROWS]


def extend_parts(parts):
    factions = ['gdi', 'nod', 'china', 'allies', 'soviet', 'scrin']
    common = dict(tier=0, cp=0, tech=1, electric_kw=0)
    for id, label, locomotor, cls, mass, cost, turn in (
        ('heavy-wheels', 'Heavy Wheels', 'heavywheeled', 'heavy-wheels', 1500, 100, 64),
        ('amphibious-micro', 'Amphibious Micro Drive', 'seal', 'amphibious-micro', 300, 50, 255)):
        parts[id] = dict(common, factions=factions[:], role='running_gear', display_name=label, locomotor=locomotor,
            compatibility_class=cls, kind='wheels', mass=mass, cost=cost, max_mass=20000, max_speed=180, turn_speed=turn,
            note='Preserves stock terrain/crushing. Physical allocation provisional, not independently calibrated.')
    # Shared capacity headroom. Existing chassis caps and all previous calculated values stay unchanged.
    for id in ('designer-heavy-tracks', 'tracks-superheavy', 'walker-heavy', 'designer-mk2-legs'):
        parts[id]['turn_speed'] = 64
    parts['designer-mk2-legs']['max_speed'] = 90
    parts['designer-mk2-legs']['display_name'] = 'Superheavy Walker Legs'
    parts['scrin-walker-gear']['display_name'] = 'Light Walker Legs'
    parts['original-armament'] = dict(common, factions=factions[:], role='weapon', display_name='Original Armament (chassis-bound)',
        mass=0, cost=0, template_locked=True, ammunition=['integral-stores'],
        note='Whole stock arsenal/abilities are included in the chassis price. Not a freely interchangeable weapon or a free retrofit.')
    parts['integrated-mount']['weapons'].append('original-armament')
    parts['superheavy-bunker'] = dict(common, factions=factions[:], role='carrier', display_name='Bunker Module',
        cost=1000, mass=1000, slots_required=3, weapons=['original-armament'], burst=1, reload_ticks=1,
        compatible_chassis_classes=['superheavy'], source='BATF.Bunker',
        note='Super-heavy chassis only; explicit graphics/trait binding still required. Original attacking cargo, five initial rocket infantry, M60/ZSU and bunker artwork inherited. Provisional 1000-credit allocation based on the 3000 vs 2000 stock reference difference, not a validated retrofit price.')
    for r in ROWS:
        choices = additional_assemblies()[ROWS.index(r)]['options']
        for role, ids in choices.items():
            if role == 'chassis': continue
            for id in ids:
                parts[id]['factions'] = sorted(set(parts[id]['factions']) | {r['faction']})
        selected = [parts[ids[0]] for role, ids in choices.items() if role not in ('chassis', 'armor')]
        hull_mass = {'wheels-light': 1500, 'amphibious-micro': 1500, 'designer-heavy-tracks': 20000,
            'tracks-superheavy': 30000, 'designer-mk2-legs': 50000, 'walker-heavy': 18000}.get(gear(r), 8000)
        mass = hull_mass + sum(p.get('mass', 0) for p in selected)
        demand = 10 + sum(p.get('electric_kw', 0) for p in selected)
        price = r['cost'] - sum(p.get('cost', 0) for p in selected)
        if price < 0: raise ValueError('Shared hardware cannot fit stock price: '+r['actor'])
        g = parts[gear(r)]
        parts[key(r)+'-hull'] = dict(common, factions=[r['faction']], role='chassis', display_name='Battle Fortress' if r['actor'] == 'BATF.Bunker' else r.get('label', r['actor'].replace('_', ' ')),
            tier=1, mass=hull_mass, cost=price, hp=r['hp'], electric_kw=10, carrier_slots=3 if r['actor'] == 'BATF.Bunker' else 1, equipment_slots=0, equipment=[],
            default_running_gear=gear(r), allowed_running_gear=[gear(r)], turn_speed_limit=r['turn'],
            max_mass=g['max_mass'], max_speed=r['speed'], reference_speed=r['speed'], reference_mass=mass,
            reference_kw=parts[motor(r)]['mechanical_kw'] - demand / parts[generator(r)]['efficiency'],
            allowed=dict(carrier=[carrier(r)], drive=[motor(r)]), source=r['actor'],
            note='Fixed stock combat assembly; arsenal, cargo and special hardware included in chassis. Shared costs deducted once. Provisional allocation, NOT an independently calibrated modular weapon. Stock promotion/technology gates retained.')
