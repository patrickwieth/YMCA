"""English UI labels for the native designer only.
Applied last by the exporter; never rewrites broad calibration/Excel data, IDs or saved names.
"""
LABELS = {
    'gdi-battle-hull': 'GDI Battle Tank Hull',
    'humvee-hull': 'Humvee Hull',
    'juggernaut': 'Juggernaut - Artillery Walker',
    'nod-light-hull': 'Nod Light Tank Hull',
    'buggy-hull': 'Buggy Hull',
    'dragon-chassis': 'Dragon - Flame Tank',
    'gatling-chassis': 'Gatling - Ground and Anti-Air',
    'overlord-chassis': 'Overlord - Heavy Assembly',
    'allied-medium-hull': 'Challenger - Allied Battle Tank',
    'field-artillery-hull': 'Field Artillery - Allied Variant',
    'soviet-heavy-hull': 'Heavy Tank - Twin Cannon',
    't34-hull': 'T-34 - Light Battle Tank',
    'heavy-tesla': 'Tesla Tank - Heavy Twin Coil',
    'tracks-standard': 'Heavy Tank Treads',
    'prototype-hover': 'Hover Drive (Prototype)',
    'gdi-stationary': 'Stationary GDI Platform',
    'wheels-light': 'Light Wheels',
    'walker-heavy': 'Heavy Walker Legs',
    'tracks-superheavy': 'Superheavy Tracks',
    'diesel': 'Standard Diesel Engine',
    'diesel-large': 'High-Output Diesel Engine',
    'diesel-light': 'Light Diesel Engine',
    'diesel-heavy': 'Heavy Diesel Engine',
    'diesel-heavy-boost': 'High-Output Heavy Diesel Engine',
    'baseline-generator': 'Baseline Generator',
    'efficient-generator': 'Efficient Generator',
    'heavy': 'Heavy Armor',
    'light': 'Light Armor',
    'medium-cannon-mount': '105mm Smoothbore Cannon',
    'scout-mg-mount': 'Light Machine Gun Mount',
    'fixed-triple': 'Juggernaut Triple Mount',
    'light-cannon-mount': 'Light Cannon Turret',
    'field-artillery-mount': 'Fixed Artillery Mount',
    'cannon-turret': 'Battlemaster Cannon Turret',
    'flame-turret': 'Dragon Flame Turret',
    'gatling-turret': 'Gatling Turret',
    'heavy-twin-turret': 'Heavy Twin Cannon Turret',
    'soviet-twin-mount': 'Twin Tank Cannon Turret',
    'twin-coil': 'Twin Tesla Coil Turret',
    'medium-cannon': 'Medium Tank Cannon',
    'scout-mg': 'Scout Machine Gun',
    'artillery': 'Juggernaut Gun and Targeting System',
    'light-cannon': 'Light Tank Cannon',
    'field-artillery': 'Field Artillery',
    'cannon': 'Tank Cannon',
    'dragon-flamer': 'Dragon Flamethrower and Firewall',
    'gatling-gun': 'Ground / Anti-Air Gatling - All Spin-Up Stages',
    'overlord-cannon': 'Heavy Tank Cannon',
    'tesla': 'Tesla Discharge System',
    'medium-tank-shell': 'Anti-Armor Tank Shells',
    'scout-mg-rounds': 'Machine Gun Ammunition',
    'artillery-shell': 'Juggernaut High-Explosive Shells',
    'light-tank-shell': 'Light Tank Shells',
    'field-artillery-he': 'High-Explosive Artillery Shells',
    'shell': 'Tank Shells',
    'flame-fuel': 'Flamethrower Fuel',
    'gatling-rounds': 'Gatling Ammunition',
    'heavy-shell': 'Heavy Tank Shells',
    'discharge': 'Tesla Discharge Package',
    'scout-sensors': 'Scout Sensors',
    'designer-he-shell': 'High-Explosive Shells (Test)',
    'designer-mlrs-hull': 'MLRS Hull with Rocket Launcher',
    'designer-rocket-mount': 'MLRS Launcher Mount (Graphics-Bound)',
    'designer-rockets': 'MLRS Ground / Anti-Air Rockets',
    'designer-rocket-payload': 'MLRS Ground / Anti-Air Warheads',
    'designer-nod-artillery-hull': 'Nod Artillery - Bound Assembly',
    'designer-ssm-hull': 'SSM - Napalm Rocket Launcher',
    'designer-ssm-mount': 'SSM Twin Launcher Mount',
    'designer-ssm-weapons': 'SSM Napalm Rockets',
    'designer-ssm-payload': 'SSM Napalm Warhead',
    'designer-ranger-hull': 'Ranger - Machine Gun Scout',
    'designer-ranger-mount': 'Ranger Machine Gun Mount',
    'designer-prism-hull': 'Prism Tank - Bound Assembly',
    'designer-prism-weapon': 'Prism Beam System',
    'designer-prism-payload': 'Prism Energy Package',
    'designer-flak-hull': 'Flak Truck - Ground / Anti-Air',
    'designer-flak-gear': 'Flak Running Gear',
    'designer-flak-mount': 'Twin Flak Mount',
    'designer-flak-weapon': 'Ground / Anti-Air Flak',
    'designer-flak-payload': 'Flak Shells',
    'scrin-drive': 'Scrin Drive Module',
    'scrin-converter': 'Scrin Baseline Converter',
    'scrin-converter-efficient': 'Scrin Efficient Converter',
    'scrin-hover-gear': 'Light Scrin Hover Drive',
    'gdi-light-hover': 'Light GDI Hover Drive',
}

for key, name in [('mammoth', 'Mammoth'), ('hmlrs', 'Hover MLRS'), ('disruptor', 'Disruptor'), ('mk2', 'Mammoth Mk II')]:
    LABELS[f'designer-{key}-hull'] = name + ' - Bound Assembly'
    LABELS[f'designer-{key}-weapons'] = name + ' Complete Weapon System'
    LABELS[f'designer-{key}-payload'] = name + ' Weapon Package'
    if key != 'disruptor': LABELS[f'designer-{key}-mount'] = name + ' Weapon Mount'

for key, name in [('gunwalker', 'Gun Walker'), ('seeker', 'Seeker'), ('corrupter', 'Corrupter'), ('devourer', 'Devourer')]:
    for suffix, label in [('hull', ' - Scrin Assembly'), ('mount', ' Weapon Mount'), ('weapon', ' Weapon System'), ('payload', ' Weapon Package')]:
        LABELS[f'scrin-{key}-{suffix}'] = name + label

for key, name in [('titan', 'Titan'), ('slingshot', 'Slingshot'), ('marv', 'MARV')]:
    for suffix, label in [('hull', ' - Bound Assembly'), ('mount', ' Weapon Mount'), ('weapon', ' Weapon System'), ('payload', ' Ammunition')]:
        LABELS[f'designer-{key}-{suffix}'] = name + label


def apply(parts):
    missing = set(LABELS) - set(parts)
    if missing: raise ValueError(f'Stale English designer label IDs: {sorted(missing)}')
    for id, label in LABELS.items(): parts[id]['display_name'] = label
