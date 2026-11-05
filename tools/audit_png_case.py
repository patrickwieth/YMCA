#!/usr/bin/env python3
"""Audit supplied building PNGs and changed/new PNGs, including Linux path casing.

--fix performs collision-checked, byte-preserving case-only renames, using git mv
for tracked files, and updates exact filename references in repository text files.
No commits are created. Run without --fix in preflight/CI.
"""
import argparse
import hashlib
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PROTECTED = 'mods/ca/bits/nod/buildings/conyard.png'
TEXT = {'.yaml', '.yml', '.py', '.cs', '.md', '.html', '.json', '.ps1', '.cmd', '.txt'}


def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args]).decode('utf-8').split('\0')


def candidates():
    paths = set(git('ls-files', '--others', '--exclude-standard', '-z'))
    paths.update(git('diff', 'HEAD', '--name-only', '-z'))
    paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT / 'mods/ca/bits').glob('*/buildings/*'))
    return sorted(s for s in paths if s and Path(s).suffix.lower() == '.png' and exact_path(s))


def exact_path(path):
    current = ROOT
    for part in Path(path).parts:
        if not current.is_dir() or part not in {p.name for p in current.iterdir()}:
            return False
        current /= part
    return current.is_file()


def audit():
    errors = [f'Uppercase PNG path: {p}' for p in candidates() if p != p.lower()]
    for p in (ROOT / 'mods/ca/sequences').glob('*.yaml'):
        for number, line in enumerate(p.read_text(encoding='utf-8').splitlines(), 1):
            match = re.match(r'\s*Filename:\s*(bits/.*\.png)\s*$', line, re.I)
            if match and not exact_path('mods/ca/' + match[1]):
                errors.append(f'{p.name}:{number}: missing or incorrect case: {match[1]}')
    return errors


def fix():
    changes = [(s, str(Path(s).with_name(Path(s).name.lower())).replace('\\', '/'))
               for s in candidates() if Path(s).name != Path(s).name.lower()]
    tracked = set(git('ls-files', '-z'))
    protected_hash = hashlib.sha256((ROOT / PROTECTED).read_bytes()).hexdigest()
    # Preflight all collisions before performing any filesystem operations.
    for old, new in changes:
        assert old != PROTECTED
        siblings = [p.name for p in (ROOT / old).parent.iterdir() if p.name.lower() == Path(new).name]
        if len(siblings) != 1:
            raise ValueError(f'Case-insensitive collision: {old}: {siblings}')
    for old, new in changes:
        before = hashlib.sha256((ROOT / old).read_bytes()).hexdigest()
        temp = old + '.case-rename-tmp'
        if (ROOT / temp).exists():
            raise ValueError(f'Temporary path already exists: {temp}')
        if old in tracked:
            subprocess.run(['git', '-C', str(ROOT), 'mv', '--', old, temp], check=True)
            subprocess.run(['git', '-C', str(ROOT), 'mv', '--', temp, new], check=True)
        else:
            (ROOT / old).rename(ROOT / temp)
            (ROOT / temp).rename(ROOT / new)
        assert hashlib.sha256((ROOT / new).read_bytes()).hexdigest() == before
        print(f'{old} -> {new}')
    files = set(git('ls-files', '-z')) | set(git('ls-files', '--others', '--exclude-standard', '-z'))
    for name in sorted(files):
        p = ROOT / name
        if not name or not p.is_file() or p.suffix.lower() not in TEXT or name.startswith('engine/'):
            continue
        try:
            original = p.read_bytes()
            text = original.decode('utf-8')
        except UnicodeError:
            continue
        updated = text
        for old, new in changes:
            updated = updated.replace(Path(old).name, Path(new).name)
        if updated != text:
            p.write_bytes(updated.encode('utf-8'))
            print(f'Updated references: {name}')
    assert hashlib.sha256((ROOT / PROTECTED).read_bytes()).hexdigest() == protected_hash


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fix', action='store_true')
    args = parser.parse_args()
    if args.fix:
        fix()
    errors = audit()
    for error in errors:
        print(error)
    print(f'PNG case audit: {len(candidates())} assets, {len(errors)} errors.')
    raise SystemExit(bool(errors))
