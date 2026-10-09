"""Prepare small/large component art; never modify the user-provided source PNGs."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/ca/bits/modular'
SOURCES = {
    'engine-basic': 'engines/basic diesel.png',
    'engine-improved': 'engines/improved diesel.png',
    'generator-basic': 'generators/basic generator.png',
}


def atlas(size=64):
    # Small prefiltered menu sprites and full-resolution grid sprites, both power-of-two.
    if size not in (64, 256):
        raise ValueError('Supported icon sizes: 64 and 256')
    sheet = Image.new('RGBA', (size * 4, size))
    for i, path in enumerate(SOURCES.values()):
        with Image.open(OUT / path) as source:
            if source.width != source.height or source.width < 64 or source.mode != 'RGBA':
                raise ValueError(f'Expected square RGBA source, at least 64px: {path}')
            icon = source.resize((size, size), Image.Resampling.LANCZOS)
            sheet.paste(icon, (i * size, 0))
    return sheet


if __name__ == '__main__':
    for size, name in [(64, 'component-icons.png'), (256, 'component-icons-large.png')]:
        target = OUT / name
        atlas(size).save(target)
        print(target)
