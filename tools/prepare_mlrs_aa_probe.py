"""Review-only MLRS AA fixture from a fresh Mountain Valleys 2P seed42 export.
Usage: python tools/prepare_mlrs_aa_probe.py INPUT.oramap OUTPUT.oramap [--baseline]
Run with YMCA_MLRS_AA_PROBE=1 and wait for mlrs-aa-flight.json. Baseline is expected
to fail acceptance. Never install these diagnostic packages as playable maps.
"""
import re
import sys
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from convert_handcrafted_layouts import miniyaml

if __name__ == '__main__':
    source, destination = map(Path, sys.argv[1:3])
    if destination.exists() or source.resolve() == destination.resolve():
        raise SystemExit('Use a fresh output package; originals must not be overwritten.')
    with ZipFile(source) as archive:
        entries = {n: archive.read(n) for n in archive.namelist()}
    text = entries['map.yaml'].decode().replace('\r\n', '\n')
    data = miniyaml(text)
    starts = [tuple(map(int, a.child('Location').value.split(',')))
              for a in data.child('Actors').children if a.value == 'mpspawn']
    if len(starts) != 2 or text.count('\tWorld:\n') != 1:
        raise SystemExit('Expected a fresh 2P mountain export with biome World rules.')
    cx = sum(s[0] for s in starts) // 2
    cy = sum(s[1] for s in starts) // 2
    text = re.sub(r'(?m)^(Title: .+)$', r'\1 [REVIEW ONLY]', text)
    text = text.replace('\tWorld:\n', f'''\tWorld:
\t\tMlrsAaFlightProbe:
\t\tTerrainCalibrationView:
\t\t\tCenter: {cx},{cy}
\t\t\tZoomScale: 0.12
\t\t\tMinimumZoomScale: 0.04
\t\t\tScreenshotTicks: 100
''', 1)
    lines = text.splitlines(keepends=True)
    start = next(i for i, line in enumerate(lines) if line.startswith('Rules:'))
    end = next((i for i in range(start + 1, len(lines)) if lines[i].strip()
                and not lines[i].startswith(('\t', ' ', '#'))), len(lines))
    extra = '''\t^Autobattler:
\t\tAutobattler:
\t\t\tRequiresCondition: false
\t\tArmyFollower:
\t\t\tRequiresCondition: false
\tPlayer:
\t\tShroud:
\t\t\tFogCheckboxEnabled: false
\t\t\tFogCheckboxLocked: true
\t\t\tExploredMapCheckboxEnabled: true
\t\t\tExploredMapCheckboxLocked: true
'''
    text = ''.join(lines[:end]) + extra + ''.join(lines[end:])
    if '--baseline' in sys.argv[3:]:
        if re.search(r'(?m)^Weapons:', text):
            raise SystemExit('Input already has custom weapon rules.')
        text += '\nWeapons:\n\t227mmAA:\n\t\tProjectile: MissileCA\n\t\t\tTerrainHeightAware: false\n\t\t\tCruiseRelativeToTerrain: false\n'
    entries['map.yaml'] = text.encode()
    destination.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(destination, 'w', ZIP_DEFLATED) as archive:
        for name, content in entries.items():
            archive.writestr(name, content)
