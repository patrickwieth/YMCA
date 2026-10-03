"""Compare engine lint against an equivalent stock-actor control map.

A clean differential is NOT a clean full-mod lint or a live gameplay test.
"""
import argparse
from collections import Counter
import os
from pathlib import Path
import re
import subprocess
import tempfile
import zipfile

from compile_prototype import REPO
from list_missing_vehicles import native_actors


def differences(control, prototype, sources=None):
    if any('Failed with exception' in s for s in (control, prototype)):
        raise ValueError('Map loading failed; lint baseline cannot be compared')
    sources = sources if sources is not None else {f'modular.{kind}': 'mtnk' for kind in ('tank', 'hover', 'stationary')}
    errors = lambda text: Counter(line for line in text.splitlines() if 'Error:' in line)
    old, new = errors(control), errors(prototype)
    inherited, unexpected = [], []
    for line, count in (new - old).items():
        normalized = line
        for actor, parent in sources.items():
            # Cargo actor-reference diagnostics identify the actor as part of a qualified field path.
            # Do not normalize arbitrary '.w0' weapon IDs or longer actor IDs as if they were the parent.
            normalized = re.sub(r'(?<![\w.])' + re.escape(actor) +
                                r'(?:(?![\w.])|(?=\.CargoInfo\.PassengerConditions`))', parent, normalized)
        target = inherited if normalized != line and normalized in old else unexpected
        target.extend([line] * count)
    return sum(old.values()), sum(new.values()), inherited, unexpected


def actor_sources(rules):
    sources = dict((actor, parent.lower()) for actor, parent in re.findall(
        r'(?m)^(modular\.[a-z0-9.]+):\r?\n\tInherits: ([A-Za-z0-9_.-]+)\r?$', rules))
    if not sources or any(parent not in native_actors() for parent in sources.values()):
        raise ValueError('Unknown or missing prototype actor bindings')
    return sources


def check(path):
    env = dict(os.environ, ENGINE_DIR=str(REPO / 'engine'),
               MOD_SEARCH_PATHS=','.join(str(REPO / p) for p in ('mods', 'engine/mods')))
    command = ['dotnet', str(REPO / 'engine/bin/OpenRA.Utility.dll'), str(REPO / 'mods/ca'), '--check-yaml']
    with tempfile.TemporaryDirectory() as temp:
        control = Path(temp) / 'control.oramap'
        with zipfile.ZipFile(path) as src, zipfile.ZipFile(control, 'x') as dest:
            sources = actor_sources(src.read('modular-rules.yaml').decode())
            text = src.read('map.yaml').decode().replace('Rules: modular-rules.yaml', 'Rules:')
            text = text.replace('Weapons: modular-weapons.yaml\n', '')
            for actor, parent in sources.items():
                text = re.sub(r'(?m)(?<=: )' + re.escape(actor) + r'(?=\r?$)', parent, text)
            dest.writestr('map.yaml', text)
            for name in ('map.bin', 'map.png'):
                dest.writestr(name, src.read(name))
        logs = []
        for item in (control, path.resolve()):
            result = subprocess.run(command + [str(item)], cwd=REPO, env=env, capture_output=True, text=True)
            text = result.stdout + result.stderr
            if result.returncode not in (0, 1) or 'Testing map:' not in text:
                raise ValueError('Utility did not check map:\n' + text)
            logs.append(text)
    old, new, inherited, unexpected = differences(*logs, sources=sources)
    lines = ['# Modular prototype engine validation', '',
             'DIFFERENTIAL LINT ONLY. Not a full lint pass and not a live match test.', '',
             f'Control errors: {old}. Prototype errors: {new}.',
             f'Additional inherited actor diagnostics: {len(inherited)}.',
             f'Unexpected new diagnostics: {len(unexpected)}.', '',
             'The control has identical terrain, players and map-local Player rules, but the corresponding stock actors.',
             'Bindings: ' + ', '.join(f'{actor} -> {parent}' for actor, parent in sources.items()),
             'An unmodified map without Rules would skip the rule lint pass and is NOT a valid control.', '',
             '## Additional inherited diagnostics', ''] + inherited + ['', '## Unexpected diagnostics', ''] + unexpected
    return '\n'.join(lines) + '\n', bool(unexpected)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--map', type=Path, default=REPO / 'mods/ca/maps/modular-gdi-lab.oramap')
    p.add_argument('--output', type=Path)
    args = p.parse_args()
    text, failed = check(args.map)
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')
    raise SystemExit(1 if failed else 0)
