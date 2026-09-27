"""Compile a strict GDI component subset into a standalone, pre-match test map.

No production rules are edited. No mid-match designs or arbitrary actor inheritance.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import re
import zipfile

from calibrate import calculate

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
SUPPORTED = frozenset(('gdi-battle-hull', 'diesel', 'baseline-generator', 'heavy',
                       'medium-cannon-mount', 'medium-cannon', 'medium-tank-shell'))
GEARS = {'tracks-standard', 'prototype-hover', 'gdi-stationary'}


def load_inputs():
    catalog = json.loads((HERE / 'catalog.json').read_text())
    prototype = json.loads((HERE / 'prototype.json').read_text())
    catalog = copy.deepcopy(catalog)
    for key, part in prototype['components'].items():
        if key in catalog['components']:
            raise ValueError('Prototype shadows catalog component: ' + key)
        catalog['components'][key] = part
    # Narrow lab-only adapter: stock tank artwork is explicitly a placeholder.
    catalog['components']['gdi-battle-hull']['allowed_running_gear'] = sorted(GEARS)
    world = (REPO / 'mods/ca/rules/world.yaml').read_text()
    match = re.search(r'\tLocomotor@HOVER:\n(.*?)(?=\n\t[^\t ]|\Z)', world, re.S)
    if not match:
        raise ValueError('Missing existing HOVER locomotor')
    block = match.group(1)
    catalog['locomotors']['hover'] = {
        'source': 'mods/ca/rules/world.yaml / Locomotor@HOVER',
        'terrain_speeds': {k: int(v) for k, v in re.findall(r'^\t{3}(\w+): (\d+)$', block, re.M)},
        'crushes': re.search(r'^\t\tCrushes: (.+)$', block, re.M).group(1).split(', '),
    }
    return catalog, prototype


def number(value):
    if not isinstance(value, (int, float)) or not math.isfinite(value) or value < 0:
        raise ValueError('Invalid emitted scalar')
    return str(math.floor(value + 0.5))


def distance(cells):
    units = int(number(cells * 1024))
    return f'{units // 1024}c{units % 1024}'


def compile_designs(catalog, designs):
    rules, weapons, summary = [], [], []
    seen = set()
    for design in designs:
        if set(design) - {'actor', 'name', 'faction', 'components', 'running_gear'}:
            raise ValueError('Unsupported design field (cargo, auxiliary, CP unlocks etc. not compiled)')
        actor = design['actor']
        if not re.fullmatch(r'modular\.[a-z][a-z0-9.-]*', actor) or actor in seen:
            raise ValueError('Invalid or duplicate generated actor ID')
        seen.add(actor)
        if not re.fullmatch(r'[A-Za-z0-9 ()/-]+', design['name']):
            raise ValueError('Unsupported display name characters')
        if design['faction'] != 'gdi' or set(design['components']) != SUPPORTED:
            raise ValueError('Unsupported component assembly: compiler currently binds GDI medium cannon only')
        result = calculate(catalog, design)
        gear = result['running_gear']
        if gear not in GEARS or result['cp'] != 0:
            raise ValueError('Unsupported running gear or commander unlock')
        expected_gear = {'tracks-standard': ('tracks', 'tracked'), 'prototype-hover': ('hover', 'hover'),
                         'gdi-stationary': ('stationary', 'wheeled')}[gear]
        part = catalog['components'][gear]
        if (part['kind'], part['locomotor']) != expected_gear or result['armor'] != 'Heavy':
            raise ValueError('Component behavior differs from compiler binding')
        if result['burst'] != 1 or result['burst_delay_ticks'] != 0:
            raise ValueError('Compiler binding only supports the single-shot cannon')
        weapon = actor + '.gun'
        rules += [f'{actor}:', '\tInherits: MTNK']
        if gear == 'prototype-hover':
            rules += ['\tInherits@MODULARHOVER: ^HoverVehicle']
        rules += ['\t-Buildable:', '\tRenderSprites:', '\t\tImage: mtnk',
                  '\tTooltip:', f'\t\tName: {design["name"]}',
                  '\tValued:', f'\t\tCost: {number(result["cost"])}',
                  '\tHealth:', f'\t\tHP: {number(result["hp"])}',
                  '\tArmor:', f'\t\tType: {result["armor"]}',
                  '\tMobile:', f'\t\tLocomotor: {result["locomotor"]}',
                  f'\t\tSpeed: {number(result["speed"])}', f'\t\tTurnSpeed: {number(result["turn_speed"])}']
        if gear == 'gdi-stationary':
            rules += ['\t\tImmovableCondition: modular-stationary',
                      '\t\tPauseOnCondition: being-captured || empdisable || being-warped || driver-dead || notmobile || modular-stationary',
                      '\tGrantCondition@MODULARSTATIONARY:', '\t\tCondition: modular-stationary',
                      '\t-ChronoshiftableWithSpriteEffect:', '\t-TeleportNetworkTransportable:']
        rules += ['\tArmament@PRIMARY:', f'\t\tWeapon: {weapon}',
                  '\tTurreted@PRIMARY:', '\t\tTurnSpeed: ' + number(catalog['components']['medium-cannon-mount']['turn_speed_reference']),
                  '\tCarryable:', '']
        # Derived from the complete original projectile/warhead template, not a damage-only weapon.
        weapons += [f'{weapon}:', '\tInherits: 120mm',
                    f'\tReloadDelay: {number(result["reload_ticks"])}',
                    f'\tBurst: {number(result["burst"])}',
                    f'\tRange: {distance(result["range_cells"])}',
                    '\tWarhead@1Dam: SpreadDamage', f'\t\tDamage: {number(result["damage"])}', '']
        summary.append(dict(actor=actor, name=design['name'], values=result))
    if not summary:
        raise ValueError('No designs selected')
    return '\n'.join(rules), '\n'.join(weapons), summary


def map_yaml(summary):
    if len(summary) > 6:
        raise ValueError('Lab layout supports at most six prototypes')
    text = '''MapFormat: 12
RequiresMod: ca
Title: Modular GDI Lab (prototype)
Author: YMCA modular tools
Tileset: RUBBERDUCK-TEMPERATE
MapSize: 130,146
Bounds: 1,17,128,128
Visibility: Lobby
Categories: Conquest
Players:
\tPlayerReference@Neutral:
\t\tName: Neutral
\t\tOwnsWorld: True
\t\tNonCombatant: True
\t\tFaction: eagle
\tPlayerReference@Creeps:
\t\tName: Creeps
\t\tNonCombatant: True
\t\tFaction: blackh
\t\tEnemies: Multi0, Multi1
\tPlayerReference@Multi0:
\t\tName: Multi0
\t\tPlayable: True
\t\tLockFaction: True
\t\tFaction: eagle
\t\tEnemies: Multi1, Creeps
\tPlayerReference@Multi1:
\t\tName: Multi1
\t\tPlayable: True
\t\tLockFaction: True
\t\tFaction: eagle
\t\tEnemies: Multi0, Creeps
Rules: modular-rules.yaml
\tPlayer:
\t\tDeveloperMode:
\t\t\tCheckboxEnabled: True
Weapons: modular-weapons.yaml
Actors:
\tSpawn0: mpspawn
\t\tOwner: Neutral
\t\tLocation: 104,-29
\tSpawn1: mpspawn
\t\tOwner: Neutral
\t\tLocation: 137,-39
\tTransport: ocar
\t\tOwner: Multi0
\t\tLocation: 100,-24
'''
    for i, design in enumerate(summary):
        text += f'\tPrototype{i}: {design["actor"]}\n\t\tOwner: Multi0\n\t\tLocation: {100 + i * 3},-29\n\t\tFacing: 384\n'
    text += '\tReference: mtnk\n\t\tOwner: Multi0\n\t\tLocation: 110,-29\n'
    text += '\tTarget: ltnk\n\t\tOwner: Creeps\n\t\tLocation: 120,-29\n'
    return text


def build(output):
    catalog, prototype = load_inputs()
    rules, weapons, summary = compile_designs(catalog, prototype['designs'])
    # Freeze the actual selected numerical catalog and source bindings for traceability.
    digest = hashlib.sha256(json.dumps(catalog, sort_keys=True).encode() +
                            json.dumps(prototype, sort_keys=True).encode() + Path(__file__).read_bytes()).hexdigest()
    source_paths = sorted((REPO / 'mods/ca/rules').rglob('*.yaml')) + sorted((REPO / 'mods/ca/weapons').rglob('*.yaml'))
    source_paths += [REPO / 'engine/VERSION', REPO / 'mods/ca/maps/testmap2.oramap']
    sources = {p.relative_to(REPO).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in source_paths}
    manifest = dict(schema=1, input_sha256=digest, source_sha256=sources,
                    scope='map-only prototype; no designer UI or general trait composition', designs=summary)
    files = {'map.yaml': map_yaml(summary).encode(), 'modular-rules.yaml': rules.encode(),
             'modular-weapons.yaml': weapons.encode(),
             'modular-manifest.json': (json.dumps(manifest, indent=2) + '\n').encode()}
    with zipfile.ZipFile(REPO / 'mods/ca/maps/testmap2.oramap') as terrain:
        for name in ('map.bin', 'map.png'):
            files[name] = terrain.read(name)
    # Reproducible map content. Refuse to replace user-modified maps.
    with Path(output).open('xb') as stream:
        with zipfile.ZipFile(stream, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
            for name, data in sorted(files.items()):
                info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(info, data)
    return manifest


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path, required=True, help='NEW .oramap file (never overwritten)')
    args = p.parse_args()
    build(args.output)
    print(args.output.resolve())
