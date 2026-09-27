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
                inherits=inherits, traits=traits, queue=fields.get('Buildable.Queue', ''), weapons=weapons, hints=hints,
                status='scalar design; runtime pending' if names else 'not modeled; inspection required',
                designs=names))
    classify(rows)
    return rows


def classify(rows, policy=None):
    """Editorial grouping, not MiniYaml evaluation. Every raw row keeps its identity."""
    if policy is None:
        policy = json.loads(Path(__file__).with_name('vehicle-family-policy.json').read_text())
    indexed = {(r['faction'], r['actor'].lower()): r for r in rows}
    helpers = {}

    def helper(key, seen=()):
        if key in seen:
            raise ValueError('Cyclic local helper inheritance')
        if key in helpers:
            return helpers[key]
        r = indexed[key]
        explicit = policy.get('categories', {}).get(r['faction'], {}).get(r['actor'])
        if explicit:
            result = explicit
        elif any('husk' in p.lower() for p in r['inherits']):
            result = 'wreck'
        elif any(p in ('^ShootableMissile', '^VoxelShootableMissile') for p in r['inherits']):
            result = 'projectile'
        else:
            inherited = {helper((r['faction'], p.lower()), seen + (key,)) for p in r['inherits']
                         if (r['faction'], p.lower()) in indexed}
            inherited.discard(None)
            if len(inherited) > 1:
                result = 'review'
            else:
                result = next(iter(inherited), None)
        helpers[key] = result
        return result

    # Check exceptions against source to catch stale actor names instead of silently ignoring them.
    for section in ('anchors', 'aliases', 'categories'):
        for faction, entries in policy.get(section, {}).items():
            for actor in entries:
                if (faction, actor.lower()) not in indexed:
                    raise ValueError('Stale family policy: ' + faction + '/' + actor)
    for faction, aliases in policy.get('aliases', {}).items():
        for target in aliases.values():
            if (faction, target.lower()) not in indexed:
                raise ValueError('Unknown family alias target: ' + faction + '/' + target)

    anchors = {(r['faction'], r['actor'].lower()) for r in rows
               if '.' not in r['actor'] and helper((r['faction'], r['actor'].lower())) is None}
    anchors.update((f, a.lower()) for f, entries in policy.get('anchors', {}).items() for a in entries)

    for r in rows:
        faction, actor = r['faction'], r['actor']
        category = helper((faction, actor.lower()))
        family = None
        reason = 'helper ancestry / explicit review policy' if category else ''
        if category is None:
            aliases = policy.get('aliases', {}).get(faction, {})
            if actor in aliases:
                family = indexed[faction, aliases[actor].lower()]['actor']
                reason = 'explicit family alias'
            else:
                candidates = [a for f, a in anchors if f == faction
                              and (actor.lower() == a or actor.lower().startswith(a + '.'))]
                if candidates:
                    family = indexed[faction, max(candidates, key=len)]['actor']
                    family = aliases.get(family, family)
                    reason = 'longest declared family anchor'
            if family is None:
                category, reason = 'review', 'no unambiguous family anchor'
            elif '~disabled' in [p.strip() for p in r['prerequisites'].split(',')]:
                category = 'prototype'
            elif r['buildable'] == 'removed locally':
                # Nuclear/chrono/spawn forms are not necessarily obsolete prototypes.
                category = 'non-production form'
            else:
                category = 'base' if actor == family else 'variant'
        r.update(category=category, family=family or '', classification=reason,
                 family_note=policy.get('notes', {}).get(faction + '/' + actor, ''))
    return rows


def family_summary(rows):
    groups = {}
    for r in rows:
        if not r['family']:
            continue
        key = (r['faction'], r['family'])
        groups.setdefault(key, []).append(r)
    result = []
    for (faction, family), members in sorted(groups.items()):
        anchor = next((r for r in members if r['actor'] == family), members[0])
        # This tests only a LOCAL declaration, not inherited/faction-specific availability.
        candidates = [r for r in members if r['buildable'] == 'local declaration'
                      and r['category'] not in ('prototype', 'non-production form')]
        covered = [r['actor'] for r in members if r['designs']]
        pending = [r['actor'] for r in members if not r['designs']]
        result.append(dict(faction=faction, family=family, name=anchor['name'] or family,
            cost=anchor['cost'], hp=anchor['hp'], speed=anchor['speed'],
            status='production candidate' if candidates else 'prototype / availability review',
            members=[r['actor'] for r in members], covered=covered, pending=pending,
            variants=[r['actor'] for r in members if r['category'] == 'variant'],
            excluded=[r['actor'] for r in members if r['category'] in ('prototype', 'non-production form')],
            hints=sorted({h for r in members for h in r['hints']}),
            notes=[r['family_note'] for r in members if r['family_note']]))
    return result


def report(rows):
    families = family_summary(rows)
    main = [f for f in families if f['status'] == 'production candidate']
    lines = ['# Vehicle families and coverage', '',
        f'{len(main)} provisional vehicle families with at least one local production declaration.',
        f'{sum(bool(f["covered"]) for f in main)} of those families have at least one scalar reference design; NOT full family coverage.',
        f'{len(families) - len(main)} further groups require prototype/availability review.',
        f'{len(rows)} raw actor blocks remain in the audit appendix, NOT {len(rows)} different vehicles.',
        'Grouping policy: vehicle-family-policy.json; named specializations remain visible within families.',
        'Production candidate does NOT mean engine-validated availability. This tool does not resolve MiniYaml inheritance.',
        'Wrecks, projectiles and attached helpers are excluded from family counts.',
        'Faction means source file, not resolved availability. Base credits/HP/speed are local anchor values, not all variant values.',
        'No row is declared gameplay-equivalent. Hints are incomplete, never proof of missing/present inherited behavior.', '',
        '## Main family overview', '',
        '| Faction | Family | Base local credits / HP / speed | Scalar-linked actors / members | Unmodeled actors | Inspect |',
        '|---|---|---|---|---|---|']
    for f in main:
        scalar = ' / '.join(f[k] or '?' for k in ('cost', 'hp', 'speed'))
        lines.append(f"| {f['faction']} | {f['name']} (`{f['family']}`) | {scalar} | {len(f['covered'])} / {len(f['members'])} | {', '.join(f['pending']) or '-'} | {', '.join(f['hints']) or 'inheritance, artwork and behavior'} |")
    lines += ['', '## Prototypes / availability review', '']
    for f in families:
        if f not in main:
            lines.append(f"- {f['faction']} / {f['family']}: {', '.join(f['members'])}")
    lines += ['', '## Separated raw actor categories', '']
    for category in ('base', 'variant', 'non-production form', 'prototype', 'wreck', 'projectile', 'attachment', 'review'):
        selected = [r for r in rows if r['category'] == category]
        lines.append(f'- {category}: {len(selected)}')
        if category not in ('base', 'variant'):
            lines.extend(f"  - {r['faction']} / {r['actor']}" for r in selected)
    lines += ['', '## Raw audit appendix', '',
        'Values are LOCAL overrides only: blank/? means inherited/unresolved, never zero.', '',
        '| Faction | Actor | Category / family | Local credits / HP / speed | Local locomotor | Status | Inspect |',
        '|---|---|---|---|---|---|---|']
    for r in rows:
        scalar = ' / '.join(r[k] or '?' for k in ('cost', 'hp', 'speed'))
        hints = ', '.join(r['hints']) or 'inheritance, weapons, artwork'
        lines.append(f"| {r['faction']} | {r['actor']} | {r['category']} / {r['family'] or '-'} | {scalar} | {r['locomotor'] or '?'} | {r['status']} | {hints} |")
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
