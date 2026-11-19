"""Create isolated native diagnostic maps, with no production map/state changes.
Usage: python tools/make-refinery-smudge-probes.py OUTPUT_DIRECTORY
Each generated CASE.oramap expects the launcher support directory OUTPUT_DIRECTORY/CASE.
"""
import struct
import sys
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=True)
width, height = 130, 146
n = width * height
header = '''MapFormat: 12
RequiresMod: ca
Title: Isolated refinery and smudge diagnostic
Author: YMCA
Tileset: TILESET
MapSize: 130,146
Bounds: 1,17,128,128
Visibility: Lobby
Categories: Test
Players:
\tPlayerReference@Neutral:
\t\tName: Neutral
\t\tOwnsWorld: True
\t\tNonCombatant: True
\t\tFaction: russia
\tPlayerReference@Multi0:
\t\tName: Multi0
\t\tPlayable: True
\t\tFaction: russia
Actors:
\tSpawn0: mpspawn
\t\tOwner: Neutral
\t\tLocation: 110,-25
Rules:
\tWorld:
\t\t-SpawnStartingUnits:
\t\t-NukularDoubleStartingUnits:
\t\tPROBE:
\t\t\tResultPath: RESULT
'''
player = '''\tPlayer:
\t\t-ConquestVictoryConditions:
\t\tShroud:
\t\t\tFogCheckboxEnabled: false
\t\t\tExploredMapCheckboxEnabled: true
'''
cases = [('refinery', 'RUBBERDUCK-TEMPERATE', 1000)]
# Only these two tilesets are enabled in the runtime manifest. Legacy palette
# definitions are covered by --check-scorch-palettes, not playable fixture maps.
for tileset in ['RUBBERDUCK-TEMPERATE', 'RUBBERDUCK-TEMPERATE-SHADED']:
    cases.append(('scorch-' + tileset.lower(), tileset, 1000))
for case, tileset, tile in cases:
    body = header.replace('TILESET', tileset).replace('PROBE', 'RefineryDockingProbe' if case == 'refinery' else 'SmudgePaletteProbe').replace('RESULT', (root / case / 'results.txt').as_posix())
    if case != 'refinery':
        for layer, seq, y in [('SCORCH', 'sc', -38), ('SCORCHNF', 'sc', -35), ('SCORCHBLACK', 'sc', -32), ('CRATER', 'cr', -29)]:
            body += f'\t\tSmudgeLayer@{layer}:\n\t\t\tInitialSmudges:\n'
            for i in range(6):
                for depth in range(5 if seq == 'cr' else 1):
                    body += f'\t\t\t\t{98+i*3},{y+depth*2}: {seq}{i+1},{depth}\n'
    binary = struct.pack('<BHHIII', 2, width, height, 17, 17+3*n, 17+4*n) + struct.pack('<HB', tile, 0)*n + bytes(3*n)
    path = root / (case + '.oramap')
    with ZipFile(path, 'w', ZIP_DEFLATED) as z:
        z.writestr('map.yaml', body + player)
        z.writestr('map.bin', binary)
    print(path)
