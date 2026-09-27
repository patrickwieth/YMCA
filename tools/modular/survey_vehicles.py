"""Inventory local vehicle blocks across six factions, NOT resolved engine rules."""
import argparse
import json
from pathlib import Path
from audit_reference_changes import blocks

FACTIONS = ('china', 'gdi', 'nod', 'allies', 'soviet', 'scrin')


def local_fields(block):
    """Read direct trait properties only; repeated Inherits are deliberately retained."""
    fields, inherits, traits = {}, [], []
    trait = None
    for line in block.splitlines()[1:]:
        text = line.expandtabs(4)
        depth = len(text) - len(text.lstrip())
        key, sep, value = text.strip().partition(':')
        if not sep:
            continue
        if depth == 4:
            trait = key
            traits.append(key)
            if key.split('@')[0] == 'Inherits':
                inherits.append(value.strip())
        elif depth == 8 and trait:
            fields[trait + '.' + key] = value.strip()
    return fields, inherits, traits


def survey(repo, catalog):
    models = {}
    for design in catalog['designs']:
        if not design.get('target'):
            continue
        hull = next(catalog['components'][k] for k in design['components'] if catalog['components'][k]['role'] == 'chassis')
        ref = design.get('reference_actor', hull.get('source'))
        if ref:
            models.setdefault((design['faction'], ref.lower()), []).append(design['name'])
    rows = []
    for faction in FACTIONS:
        path = f'mods/ca/rules/{faction}/vehicles.yaml'
        for actor, block in blocks((repo / path).read_text(encoding='utf-8-sig')).items():
            if actor.startswith('^'):
                continue
            fields, inherits, traits = local_fields(block)
            names = models.get((faction, actor.lower()), [])
            # All roots retained, including variants, helpers and disabled prototypes.
            # This avoids treating a missing local Buildable as proof of availability.
            hints = []
            for label, tokens in (
                ('cargo', ('Cargo', '^Transport', '^AttackingCargo')),
                ('transformation/deploy', ('Transforms', 'TransformOnCondition', 'GrantConditionOnDeploy')),
                ('hover/amphibious', ('^Hover', '^Amphib', 'Hovers')),
                ('walker', ('^GDIWalker',)),
                ('sensor', ('^SensorEquipment',)),
                ('special attacks', ('^ChinaGatling', '^Tesla', '^NodLaser', '^Propaganda', 'AttackCharge')),
            ):
                if any(any(t.startswith(token) for token in tokens) for t in traits + inherits):
                    hints.append(label)
            weapons = [v for k, v in fields.items() if k.startswith('Armament') and k.endswith('.Weapon')]
            if len(weapons) > 1:
                hints.append('multiple/conditional armaments')
            rows.append(dict(faction=faction, actor=actor, name=fields.get('Tooltip.Name', ''),
                source=path, cost=fields.get('Valued.Cost', ''), hp=fields.get('Health.HP', ''),
                speed=fields.get('Mobile.Speed', ''), locomotor=fields.get('Mobile.Locomotor', ''),
                prerequisites=fields.get('Buildable.Prerequisites', ''),
                buildable='removed locally' if '-Buildable' in traits else ('local declaration' if 'Buildable' in traits else 'inherited/unresolved'),
                inherits=inherits, weapons=weapons, hints=hints,
                status='scalar design; runtime pending' if names else 'not modeled; inspection required',
                designs=names))
    return rows


def report(rows):
    modeled = sum(bool(r['designs']) for r in rows)
    lines = ['# Cross-faction vehicle coverage inventory', '',
        f'{len(rows)} raw actor blocks across six vehicle files; {modeled} linked to scalar designs.',
        'This is NOT a count of buildable vehicles. Variants, helpers and obsolete prototypes remain visible.',
        'Values are LOCAL overrides only: blank means inherited/unresolved, never zero.',
        'Hints are non-exhaustive. Missing a hint does not prove absence of a feature.',
        'No row is declared gameplay-equivalent. See catalog design notes for excluded upgrades/behaviors.', '',
        '| Faction | Actor | Local credits / HP / speed | Local locomotor | Status | Inspect |',
        '|---|---|---|---|---|---|']
    for r in rows:
        scalar = ' / '.join(r[k] or '?' for k in ('cost', 'hp', 'speed'))
        hints = ', '.join(r['hints']) or 'inheritance, weapons, artwork'
        lines.append(f"| {r['faction']} | {r['actor']} | {scalar} | {r['locomotor'] or '?'} | {r['status']} | {hints} |")
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path)
    args = p.parse_args()
    here = Path(__file__).resolve().parent
    text = report(survey(here.parents[1], json.loads((here / 'catalog.json').read_text())))
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')
