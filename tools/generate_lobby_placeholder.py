"""Build the hidden, non-playable dedicated-lobby placeholder (no supplied art changed)."""
from pathlib import Path
import struct
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'mods/ca/maps/please-select-map'


def generate():
    OUT.mkdir(parents=True, exist_ok=True)
    width, height = 18, 34
    count = width * height
    data = struct.pack('<BHHIII', 2, width, height, 17, 17 + 3 * count, 17 + 4 * count)
    data += struct.pack('<HB', 1000, 0) * count + bytes(count) + bytes(2 * count)
    (OUT / 'map.bin').write_bytes(data)
    Image.new('RGBA', (64, 64)).save(OUT / 'map.png')
    text = '''MapFormat: 12
RequiresMod: ca
Title: Please select a map
Author: YMCA
Tileset: RUBBERDUCK-TEMPERATE
MapSize: 18,34
Bounds: 1,17,16,16
Visibility: 0
Categories: LobbyPlaceholder
Players:
\tPlayerReference@Neutral:
\t\tName: Neutral
\t\tOwnsWorld: True
\t\tNonCombatant: True
\t\tFaction: england
'''
    for i in range(8):
        text += f'\tPlayerReference@Multi{i}:\n\t\tName: Multi{i}\n\t\tPlayable: True\n\t\tAllowBots: False\n\t\tFaction: Random\n'
    text += 'Actors:\n'
    for i, (u, v) in enumerate([(4,20),(8,20),(12,20),(4,24),(12,24),(4,28),(8,28),(12,28)]):
        y = v // 2 - u
        x = v - y
        text += f'\tSpawn{i}: mpspawn\n\t\tOwner: Neutral\n\t\tLocation: {x},{y}\n'
    (OUT / 'map.yaml').write_text(text, encoding='utf-8', newline='\n')


if __name__ == '__main__':
    generate()
