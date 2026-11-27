"""Recover byte-exact pre-isometric references, never install or modify runtime maps.

Run from any directory. Git object reads only; output is outside mods/ca/maps.
Existing differing files are rejected, not overwritten.
"""
import hashlib
import io
import json
from pathlib import Path
import re
import struct
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
REVISION = 'a8559948636b5b499b0f9c8a9a1364dc2c1e2066'
OUT = ROOT / 'art-source/handcrafted/originals'
SOURCES = {
    'all-that-glitters-barren': 'mods/ca/maps/All that Glitters B',
    'dune-patrol': 'mods/ca/maps/dunepatr (1).oramap',
    'two-sides-naval': 'mods/ca/maps/twosides',
}


def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args])


def field(text, name):
    match = re.search(r'^' + re.escape(name) + r':\s*([^\r\n]+)', text, re.M)
    if not match:
        raise ValueError('Missing map field: ' + name)
    return match[1].strip()


def actors(text):
    section = re.split(r'^Actors:\s*\n', text, flags=re.M)[1]
    section = re.split(r'^\S', section, maxsplit=1, flags=re.M)[0]
    result = []
    for match in re.finditer(r'^\t([^\s:]+):\s*([^\n]+)\n((?:\t\t[^\n]*\n|\n)*)', section + '\n', re.M):
        location = re.search(r'^\t\tLocation:\s*(-?\d+)\s*,\s*(-?\d+)', match[3], re.M)
        if location:
            result.append({'id': match[1], 'type': match[2].strip(), 'location': list(map(int, location.groups()))})
    return result


def binary_summary(data, expected_size, bounds):
    version, width, height = struct.unpack_from('<BHH', data)
    if [width, height] != expected_size:
        raise ValueError('Binary and YAML map sizes disagree')
    if version == 1:
        tiles, heights, resources = 5, 0, 5 + 3 * width * height
    elif version == 2:
        tiles, heights, resources = struct.unpack_from('<III', data, 5)
    else:
        raise ValueError('Unknown map.bin version: ' + str(version))
    for offset, stride in [(tiles, 3), (heights, 1), (resources, 2)]:
        if offset and offset + stride * width * height > len(data):
            raise ValueError('Truncated binary map layer')
    bx, by, bw, bh = bounds
    if bx < 0 or by < 0 or bx + bw > width or by + bh > height:
        raise ValueError('Bounds outside map')
    counts = {}
    terrain_tiles = set()
    elevations = set()
    for x in range(bx, bx + bw):
        for y in range(by, by + bh):
            index = x * height + y  # OpenRA serializes columns, not rows.
            if resources:
                kind, density = struct.unpack_from('<BB', data, resources + 2 * index)
                if kind:
                    record = counts.setdefault(str(kind), {'cells': 0, 'density_sum': 0})
                    record['cells'] += 1
                    record['density_sum'] += density
            if tiles:
                terrain_tiles.add(struct.unpack_from('<HB', data, tiles + 3 * index))
            elevations.add(data[heights + index] if heights else 0)
    return {'binary_format': version, 'resource_layers': counts, 'terrain_tile_variants': len(terrain_tiles),
            'stored_elevations': sorted(elevations)}


def load_source(source):
    if source.endswith('.oramap'):
        package = git('show', REVISION + ':' + source)
        with zipfile.ZipFile(io.BytesIO(package)) as archive:
            files = {n: archive.read(n) for n in archive.namelist() if not n.endswith('/')}
        return files, package
    prefix = source + '/'
    names = git('ls-tree', '-r', '--name-only', REVISION, '--', source).decode().splitlines()
    if not names:
        raise ValueError('No files for ' + source)
    return {n[len(prefix):]: git('show', REVISION + ':' + n) for n in names if n.startswith(prefix)}, None


def save_exact(path, data):
    if path.exists() and path.read_bytes() != data:
        raise ValueError('Refusing to overwrite changed reference: ' + str(path))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


def prepare():
    # This snapshot must really precede the grid conversion, not be the later archive.
    mod = git('show', REVISION + ':mods/ca/mod.yaml').decode('utf-8-sig')
    if not re.search(r'^MapGrid:\s*\n(?:\t[^\n]*\n)*\tType: Rectangular', mod, re.M):
        raise ValueError('Source mod is not the original rectangular grid')
    records = []
    for slug, source in SOURCES.items():
        files, package = load_source(source)
        for name, data in files.items():
            if Path(name).is_absolute() or '..' in Path(name).parts or '\\' in name:
                raise ValueError('Unsafe archive path')
            save_exact(OUT / slug / name, data)
        if package is not None:
            save_exact(OUT / (slug + '.original.oramap'), package)
        text = files['map.yaml'].decode('utf-8-sig').replace('\r\n', '\n')
        size = list(map(int, field(text, 'MapSize').split(',')))
        bounds = list(map(int, field(text, 'Bounds').split(',')))
        placed = actors(text)
        records.append({'slug': slug, 'source_path': source, 'source_revision': REVISION,
            'title': field(text, 'Title'), 'author': field(text, 'Author'), 'tileset': field(text, 'Tileset'),
            'grid': 'Rectangular', 'stored_size': size, 'playable_bounds': bounds,
            'playable_cells': bounds[2] * bounds[3],
            'starts': [a['location'] for a in placed if a['type'].lower() == 'mpspawn'],
            'oil_derricks': [a['location'] for a in placed if a['type'].lower() == 'oilb'],
            'actor_count': len(placed), **binary_summary(files['map.bin'], size, bounds),
            'files': {n: {'bytes': len(b), 'sha256': hashlib.sha256(b).hexdigest()} for n, b in sorted(files.items())},
            'original_package_sha256': hashlib.sha256(package).hexdigest() if package else None,
            'remake_status': 'reference recovered; not converted or runtime-enabled',
            'target_category': 'Handcrafted Maps'})
    # Case-only duplicates cannot safely be checked out together on Windows.
    lower, _ = load_source('mods/ca/maps/twosides')
    upper, _ = load_source('mods/ca/maps/TwoSides')
    autobattle, _ = load_source('mods/ca/maps/TwoSidesAB')
    manifest = {'source_revision': REVISION, 'maps': records,
        'two_sides_selection': {'chosen': 'mods/ca/maps/twosides', 'uppercase_duplicate_identical': lower == upper,
            'excluded_variant': 'mods/ca/maps/TwoSidesAB',
            'excluded_title': field(autobattle['map.yaml'].decode('utf-8-sig'), 'Title'),
            'reason': 'Original playable Naval map; not the separate Autobattle scenario.'}}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    for m in records:
        print(m['title'], 'stored', m['stored_size'], 'playable', m['playable_bounds'][2:], 'starts', len(m['starts']))
    return manifest


if __name__ == '__main__':
    prepare()
