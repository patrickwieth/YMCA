"""Export the deliberately limited native main-menu designer catalog (no Python at runtime)."""
import json
import itertools
from pathlib import Path
from calibrate import calculate
from compile_prototype import load_inputs, REPO


def data():
    catalog, _ = load_inputs()
    options = {
        'chassis': ['gdi-battle-hull'],
        'running_gear': ['tracks-standard', 'prototype-hover', 'gdi-stationary'],
        'drive': ['diesel', 'diesel-large'],
        'generator': ['baseline-generator', 'efficient-generator'],
        'armor': ['heavy'],
        'carrier': ['medium-cannon-mount'],
        'weapon': ['medium-cannon'],
        'ammunition': ['medium-tank-shell', 'designer-he-shell'],
    }
    parts = {key: catalog['components'][key] for values in options.values() for key in values if key != 'designer-he-shell'}
    for key, label in {'diesel': 'Standard-Dieselmotor', 'diesel-large': 'Leistungsgesteigerter Dieselmotor',
                       'baseline-generator': 'Basisgenerator', 'efficient-generator': 'Effizienter Generator',
                       'heavy': 'Schwere Panzerung'}.items():
        parts[key]['display_name'] = label
    parts['designer-he-shell'] = dict(display_name='Sprenggranaten (Test)', role='ammunition', factions=['gdi'],
        tier=0, cp=0, tech=1, mass=400, cost=75, electric_kw=0, damage=4000, source='120mmHEAT',
        note='Experimental 75-credit allocation; preserves full existing HE projectile/versus package.')
    return dict(schema=1, alpha=catalog['alpha'], credits_per_cp=catalog['credits_per_cp'], options=options, components=parts)


def cases():
    catalog, _ = load_inputs()
    exported = data()
    catalog['components']['designer-he-shell'] = exported['components']['designer-he-shell']
    catalog['components']['gdi-battle-hull']['allowed']['drive'].append('diesel-large')
    catalog['components']['medium-cannon']['ammunition'].append('designer-he-shell')
    rows = []
    for gear, motor, generator, ammo in itertools.product(*(exported['options'][r] for r in ('running_gear', 'drive', 'generator', 'ammunition'))):
        parts = dict(chassis='gdi-battle-hull', running_gear=gear, drive=motor, generator=generator,
                     armor='heavy', carrier='medium-cannon-mount', weapon='medium-cannon', ammunition=ammo)
        design = dict(name='Native comparison', faction='gdi', running_gear=gear,
                      components=[v for k, v in parts.items() if k != 'running_gear'])
        result = calculate(catalog, design)
        rows.append(dict(parts=parts, values={k: result[k] for k in ('cost', 'mass', 'hp', 'speed', 'turn_speed', 'electric_kw', 'reserve_kw', 'tech', 'catalog_points')}))
    return rows


if __name__ == '__main__':
    target = REPO / 'mods/ca/modular/designer-catalog.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(data(), indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    Path(__file__).with_name('designer-calculation-cases.json').write_text(json.dumps(cases(), indent=2) + '\n', encoding='utf-8')
    print(target)
