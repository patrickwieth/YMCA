"""Compatibility classes are not engine locomotors. Keep legacy terrain behavior."""
import copy

ALIASES = dict.fromkeys(('designer-mlrs-gear', 'designer-ssm-gear', 'designer-prism-gear'), 'light-tracks')


def canonical(key):
    return ALIASES.get(key, key)


def consolidate(parts):
    shared = copy.deepcopy(parts['designer-mlrs-gear'])
    shared.update(display_name='Light Tracks', factions=['gdi', 'nod', 'allies'],
        note='Shared MLRS/SSM/Prism hardware. Light-tracks compatibility class; stock wheeled terrain/crushing retained. Old IDs migrate on load.')
    parts['light-tracks'] = shared
    for key in ALIASES: del parts[key]
    parts['tracks-light-artillery']['display_name'] = 'Light Tracks (Tracked Profile)'
    parts['designer-heavy-tracks']['display_name'] = 'Heavy Tracks'
    classes = {
        'tracks-standard': 'medium-tracks', 'light-tracks': 'light-tracks', 'tracks-light-artillery': 'light-tracks',
        'designer-heavy-tracks': 'heavy-tracks', 'tracks-superheavy': 'superheavy-tracks',
        'wheels-light': 'light-wheels', 'designer-flak-gear': 'light-wheels',
        'walker-heavy': 'heavy-walker', 'designer-mk2-legs': 'superheavy-walker', 'scrin-walker-gear': 'light-walker',
        'prototype-hover': 'medium-hover', 'gdi-light-hover': 'light-hover', 'scrin-hover-gear': 'light-hover',
        'gdi-stationary': 'stationary',
    }
    for key, value in parts.items():
        if value['role'] == 'running_gear': value['compatibility_class'] = classes[key]
