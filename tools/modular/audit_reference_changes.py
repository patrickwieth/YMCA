"""Compare selected source blocks across commits; not a YAML inheritance resolver."""
import argparse
import difflib
import subprocess
from pathlib import Path

SOURCES = {
    'mods/ca/rules/china/vehicles.yaml': ['chbattle', 'chbattle.Autoloader', 'chbattle.Autoloader.PDL',
        'chbattle.Autoloader.Reflector', 'chbattle.Nuclear_Shells', 'chbattle.Nuclear_Shells.PDL',
        'chbattle.Nuclear_Shells.Reflector', 'chbattle.Mass_Production', 'chbattle.Mass_Production.PDL',
        'chbattle.Mass_Production.Reflector', 'chdragon', 'chdragon.PDL', 'chdragon.Reflector',
        'chgtnk', 'chgtnk.PDL', 'chgtnk.Reflector', 'choverlord', 'choverlord.Nuke_Shells',
        'choverlord.Nuke_Shells.PDL', 'choverlord.Nuke_Shells.Reflector', 'choverlord.Propaganda',
        'choverlord.Propaganda.PDL', 'choverlord.Propaganda.Reflector'],
    'mods/ca/rules/china/weapons.yaml': ['CHBattlemasterCannon', 'CHBattlemasterCannon.Autoloader',
        'CHBattlemasterCannon.Nuclear_Shells', 'CHAtomicTankExplode', 'CHDragonFlamer',
        'CHDragonFlamer.Black_Napalm', 'CHDragonFirestorm', 'CHDragonFirestorm2',
        'ChinaMGatt.0G', 'ChinaMGatt.1G', 'ChinaMGatt.2G', 'ChinaMGatt.3G',
        'ChinaMGatt.0', 'ChinaMGatt.1', 'ChinaMGatt.2', 'ChinaMGatt.3', 'OverlordCannon', 'OverlordCannonNuclear'],
    'mods/ca/rules/china/defaults.yaml': ['^AtomicTank', '^UranShells', '^HordeBonus', '^ChinaGatling', '^PropagandaSpeaker'],
    'mods/ca/rules/china/commander-tree.yaml': ['promotion.Battlemaster.Autoloader',
        'promotion.Battlemaster.Nuclear_Shells', 'promotion.Battlemaster.Mass_Production',
        'promotion.Battlemaster.PDL', 'promotion.Battlemaster.Reflector',
        'promotion.Dragon_Tank.PDL', 'promotion.Dragon_Tank.Reflector',
        'promotion.Gatling.PDL', 'promotion.Gatling.Reflector', 'promotion.Overlord.Nuclear_Shells',
        'promotion.Overlord.Propaganda', 'promotion.Overlord.PDL', 'promotion.Overlord.Reflector'],
    'mods/ca/rules/gdi/vehicles.yaml': ['Juggernaut', 'Juggernaut.Emp'],
    'mods/ca/rules/gdi/weapons.yaml': ['JuggernautGun', 'JuggernautGun.Emp', 'JuggernautDummyAim'],
    'mods/ca/rules/gdi/defaults.yaml': ['^GDIWalkerUpgrades'],
    'mods/ca/rules/soviet/vehicles.yaml': ['TTNK.RA2'],
    'mods/ca/rules/soviet/weapons.yaml': ['TTankZap', 'TTankZapMK2'],
    'mods/ca/rules/soviet/defaults.yaml': ['^TeslaUnit'],
    'mods/ca/rules/defaults.yaml': ['^Vehicle', '^VehicleVision', '^Tank', '^FightingTank',
        '^FightingTankTurreted', '^PointLaserDefenseSystem', '^ReflectorArmor', '^HeavyArmor', '^BigVehicle'],
    'mods/ca/weapons/other.yaml': ['AdvancedPointLaser'],
}


def blocks(text):
    result = {}
    key = None
    for line in text.lstrip('\ufeff').splitlines():
        if line and not line[0].isspace() and not line.startswith('#') and ':' in line:
            key = line.split(':', 1)[0]
            # Preserve repeated definitions in source order; do not emulate MiniYaml merging.
            result.setdefault(key, [])
        if key is not None and line.strip() and not line.lstrip().startswith('#'):
            result[key].append(line.rstrip())
    return {key: '\n'.join(lines) for key, lines in result.items()}


def audit(repo, old, new):
    lines = ['# Reference-source change audit', '', f'Previous: `{old}`; integrated: `{new}`.',
             'Selected raw root blocks only; not a complete resolved-rules dependency or runtime audit.',
             'Missing blocks stop the audit. Comments and blank lines are ignored.', '',
             '| File / block | Status |', '|---|---|']
    changes = []
    count = 0
    for path, keys in SOURCES.items():
        versions = [blocks(subprocess.check_output(['git', 'show', f'{revision}:{path}'], cwd=repo).decode('utf-8'))
                    for revision in (old, new)]
        for key in keys:
            a, b = (version[key] for version in versions)
            changed = a != b
            count += 1
            lines.append(f'| `{path}` / `{key}` | {"CHANGED" if changed else "unchanged"} |')
            if changed:
                changes.append(f'## {key}\n\n```diff\n' + '\n'.join(difflib.unified_diff(
                    a.expandtabs(4).splitlines(), b.expandtabs(4).splitlines(), fromfile=old, tofile=new, lineterm='')) + '\n```')
    lines += ['', f'{count} blocks compared; {len(changes)} changed.', ''] + changes
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--old', default='7f5aeadf')
    parser.add_argument('--new', default='9d8a9dec')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    text = audit(Path(__file__).resolve().parents[2], args.old, args.new)
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')
