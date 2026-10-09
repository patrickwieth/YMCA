"""Downsample user-provided component art; never modify the 256px source PNGs."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/ca/bits/modular'
SOURCES = {
    'engine-basic': 'engines/basic diesel.png',
    'engine-improved': 'engines/improved diesel.png',
    'generator-basic': 'generators/basic generator.png',
}


def atlas():
    # Three 64px square sprites plus transparent padding, both dimensions power-of-two.
    sheet = Image.new('RGBA', (256, 64))
    for i, path in enumerate(SOURCES.values()):
        with Image.open(OUT / path) as source:
            if source.size != (256, 256) or source.mode != 'RGBA':
                raise ValueError(f'Expected 256x256 RGBA source: {path}')
            icon = source.resize((64, 64), Image.Resampling.LANCZOS)
            sheet.paste(icon, (i * 64, 0))
    return sheet


if __name__ == '__main__':
    target = OUT / 'component-icons.png'
    atlas().save(target)
    print(target)
