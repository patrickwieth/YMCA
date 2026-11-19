"""Derive shaded black flames from the RA effect palette; keep neutral smoke colors."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = root / 'mods/ca/bits/palettes/temperat.pal'
target = root / 'mods/ca/bits/palettes/scorch-black.pal'
data = bytearray(source.read_bytes())
assert len(data) == 768
for i in range(256):
    r, g, b = data[i*3:i*3+3]
    # Flame ramps are chromatic. Preserve the original neutral smoke ramp.
    if max(r, g, b) - min(r, g, b) > 3:
        shade = round((r * .299 + g * .587 + b * .114) * .3)
        data[i*3:i*3+3] = bytes([shade] * 3)
target.write_bytes(data)
print(target)
