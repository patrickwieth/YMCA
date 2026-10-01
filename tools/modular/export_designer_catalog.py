"""Export supported native assemblies and independent calculator fixtures; no Python at runtime."""
import copy
import itertools
import json
import re
from pathlib import Path
from calibrate import calculate
from compile_prototype import load_inputs, REPO
from gdi_designer_expansion import additional_assemblies, extend_parts
import nod_designer_expansion as nod
import china_designer_expansion as china


def assemblies():
    def frame(hull, gear, drive, armor, carrier, weapon, ammo, built_in=None):
        return dict(options=dict(chassis=[hull], running_gear=gear, drive=drive,
                    generator=['baseline-generator', 'efficient-generator'], armor=[armor],
                    carrier=[carrier], weapon=[weapon], ammunition=ammo), built_in=built_in or [])
    return [
        frame('gdi-battle-hull', ['tracks-standard', 'prototype-hover', 'gdi-stationary'], ['diesel', 'diesel-large'],
              'heavy', 'medium-cannon-mount', 'medium-cannon', ['medium-tank-shell', 'designer-he-shell']),
        frame('humvee-hull', ['wheels-light', 'gdi-stationary'], ['diesel-light'],
              'light', 'scout-mg-mount', 'scout-mg', ['scout-mg-rounds'], ['scout-sensors']),
        frame('designer-mlrs-hull', ['designer-mlrs-gear'], ['diesel', 'diesel-large'],
              'light', 'designer-rocket-mount', 'designer-rockets', ['designer-rocket-payload']),
    ] + additional_assemblies() + nod.additional_assemblies() + china.additional_assemblies()


def data():
    catalog, _ = load_inputs()
    options = {}
    for assembly in assemblies():
        for role, ids in assembly['options'].items():
            options.setdefault(role, [])
            options[role].extend(i for i in ids if i not in options[role])
    parts = {key: copy.deepcopy(catalog['components'][key]) for ids in options.values() for key in ids if key in catalog['components']}
    parts['scout-sensors'] = copy.deepcopy(catalog['components']['scout-sensors'])
    for key, label in {'diesel': 'Standard-Dieselmotor', 'diesel-large': 'Leistungsgesteigerter Dieselmotor',
                       'baseline-generator': 'Basisgenerator', 'efficient-generator': 'Effizienter Generator',
                       'heavy': 'Schwere Panzerung'}.items():
        parts[key]['display_name'] = label
    parts['designer-he-shell'] = dict(display_name='Sprenggranaten (Test)', role='ammunition', factions=['gdi'],
        tier=0, cp=0, tech=1, mass=400, cost=75, electric_kw=0, damage=4000, source='120mmHEAT',
        note='Experimental 75-credit allocation; full existing HE projectile/versus package.')
    common = dict(factions=['gdi'], tier=0, cp=0, tech=2, electric_kw=0)
    parts['designer-mlrs-hull'] = dict(common, display_name='MLRS-Rumpf mit Raketenwerfer', role='chassis',
        tier=2, mass=8000, cost=350, hp=16000, electric_kw=10, carrier_slots=1, equipment_slots=0,
        default_running_gear='designer-mlrs-gear', allowed_running_gear=['designer-mlrs-gear'], turn_speed_limit=48,
        max_mass=20000, reference_mass=11600, reference_kw=460, reference_speed=82, max_speed=110,
        allowed=dict(carrier=['designer-rocket-mount'], drive=['diesel', 'diesel-large']), equipment=[], source='MLRS')
    parts['designer-mlrs-gear'] = dict(common, display_name='MLRS-Fahrwerk (bestehendes Fahrprofil)', role='running_gear',
        mass=1500, cost=100, locomotor='wheeled', kind='tracks', max_mass=20000, max_speed=150, turn_speed=48,
        note='MLRS inherits wheeled terrain/crushing from ^Vehicle despite its artwork. Preserve existing profile.')
    parts['designer-rocket-mount'] = dict(common, display_name='MLRS-Startlafette (grafisch gebunden)', role='carrier',
        mass=600, cost=150, weapons=['designer-rockets'], burst=2, reload_ticks=120, secondary_reload_ticks=80,
        burst_delay_ticks=4, fire_delay_ticks=10, turn_speed_reference=80, source='MLRS',
        note='Ground reference cadence; AA armament keeps its independent 80-tick reload. Full frontal attack and aiming animation inherited.')
    parts['designer-rockets'] = dict(common, display_name='MLRS Boden- und Flugabwehrraketen', role='weapon',
        mass=500, cost=175, ammunition=['designer-rocket-payload'], range_cells=10, min_range_cells=4, secondary_range_cells=10.5,
        source='227mm + 227mmAA', note='Two complete weapons, not one damage-only approximation. AA range 10.5.')
    parts['designer-rocket-payload'] = dict(common, display_name='MLRS Boden-/Luft-Sprengkoepfe', role='ammunition',
        mass=400, cost=75, damage=1300, secondary_damage=3000, source='227mm + 227mmAA',
        note='Ground raw damage 1300 with existing falloff; AA 3000. All projectile/versus/effect rules inherited.')
    extend_parts(parts, catalog)
    nod.extend_parts(parts, catalog)
    china.extend_parts(parts)
    # These are explicit designer adapters, not changes to the broad catalog/workbooks.
    for assembly in assemblies():
        choices = assembly['options']
        hull = parts[choices['chassis'][0]]
        hull['allowed_running_gear'] = choices['running_gear'][:]
        hull['allowed']['drive'] = choices['drive'][:]
        parts[choices['weapon'][0]]['ammunition'] = choices['ammunition'][:]
    return dict(schema=1, alpha=catalog['alpha'], credits_per_cp=catalog['credits_per_cp'], options=options, components=parts,
                assemblies=assemblies())


def cases():
    catalog, _ = load_inputs()
    exported = data()
    catalog['components'].update(exported['components'])
    world = (REPO / 'mods/ca/rules/world.yaml').read_text(encoding='utf-8')
    match = re.search(r'\tLocomotor@HEAVYTRACKED:\n(.*?)(?=\n\t[^\t ]|\Z)', world, re.S)
    if not match or '\t\tName: heavytracked' not in match.group(1):
        raise ValueError('Missing existing heavytracked locomotor')
    catalog['locomotors']['heavytracked'] = {
        'source': 'mods/ca/rules/world.yaml / Locomotor@HEAVYTRACKED',
        'terrain_speeds': {k: int(v) for k, v in re.findall(r'^\t{3}(\w+): (\d+)$', match.group(1), re.M)},
        'crushes': re.search(r'^\t\tCrushes: (.+)$', match.group(1), re.M).group(1).split(', '),
    }
    rows = []
    for assembly in assemblies():
        choices = assembly['options']
        for values in itertools.product(*choices.values()):
            parts = dict(zip(choices, values))
            design = dict(name='Native comparison', faction=assembly.get('faction', 'gdi'), running_gear=parts['running_gear'],
                          components=[v for k, v in parts.items() if k != 'running_gear'] + assembly['built_in'])
            result = calculate(catalog, design)
            rows.append(dict(base_faction={'gdi': 'eagle', 'nod': 'blackh', 'china': 'chinatnk'}[assembly.get('faction', 'gdi')], parts=parts, values={k: result[k] for k in
                ('cost', 'mass', 'hp', 'speed', 'turn_speed', 'electric_kw', 'reserve_kw', 'tech', 'catalog_points')}))
    return rows


if __name__ == '__main__':
    target = REPO / 'mods/ca/modular/designer-catalog.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(data(), indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    Path(__file__).with_name('designer-calculation-cases.json').write_text(json.dumps(cases(), indent=2) + '\n', encoding='utf-8')
    print(target)
