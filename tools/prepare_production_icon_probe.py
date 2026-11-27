"""Prepare an isolated native icon probe from a prepared building-grid test copy."""
import argparse
from pathlib import Path
import re
from zipfile import ZipFile, ZIP_DEFLATED

p = argparse.ArgumentParser()
p.add_argument('source', type=Path)
p.add_argument('destination', type=Path)
p.add_argument('--scroll', action='store_true')
p.add_argument('--no-radar', action='store_true')
a = p.parse_args()
if a.source.resolve() == a.destination.resolve():
    raise ValueError('Probe must be a separate copy')
with ZipFile(a.source) as z:
    files = {n: z.read(n) for n in z.namelist()}
t = files['map.yaml'].decode('utf-8-sig').replace('\r\n', '\n')
if 'Title: Building Grid Test' not in t or '\tWorld:\n' in t:
    raise ValueError('Use the ordinary prepared building-grid map, not another probe')
t = t.replace('Faction: blackh', 'Faction: england').replace('FastBuild: True', 'FastBuild: False')
actors = 'Actors:\n'
for i, (name, x, y) in enumerate([('mpspawn',88,48), ('fact',79,31), ('powr',85,25), ('apwr',91,19), ('dome',95,23), ('oil_platform',164,-64)]):
    if a.no_radar and name == 'dome':
        continue
    owner = 'Neutral' if name == 'mpspawn' else 'Multi0'
    actors += f'\tIconReview{i}: {name}\n\t\tOwner: {owner}\n\t\tLocation: {x},{y}\n'
    if name != 'mpspawn':
        actors += '\t\tFaction: england\n'
t = re.sub(r'^Actors:\n.*?(?=^[A-Za-z][^\n]*:|\Z)', actors+'\n', t, flags=re.M|re.S)
# The early-game 1% time multiplier is disabled only here to exercise a real
# progress clock. Production rules and the ordinary building-test map are untouched.
rules = ('\tPOWR:\n\t\t-ProductionTimeMultiplier@BuildUpBoost:\n\t\tBuildable:\n'
         '\t\t\tBuildDuration: 500\n\t\t\tBuildDurationModifier: 100\n'
         '\tWorld:\n\t\tProductionPaletteReviewProbe:\n\t\tTerrainCalibrationView:\n'
         '\t\t\tCenter: 166,-62\n\t\t\tZoomScale: 0.65\n'
         '\t\t\tMinimumZoomScale: 0.04\n\t\t\tScreenshotTicks: 1000000\n')
if a.scroll:
    rules += ''.join(f'\tUI.ICON.REVIEW{i}:\n\t\tInherits: POWR\n\t\tBuildable:\n\t\t\tBuildPaletteOrder: {2000+i}\n' for i in range(10))
t = t.replace('Rules:\n', 'Rules:\n'+rules, 1)
files['map.yaml'] = t.encode()
a.destination.parent.mkdir(parents=True, exist_ok=True)
with ZipFile(a.destination, 'w', ZIP_DEFLATED) as z:
    for name, data in files.items():
        z.writestr(name, data)
