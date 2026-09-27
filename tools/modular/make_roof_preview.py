"""Create a raw-sprite contact sheet from Utility --png exports (requires Pillow)."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw


def build(source, output):
    image = Image.new('RGB', (8 * 140, 2 * 160 + 50), '#30343b')
    draw = ImageDraw.Draw(image)
    draw.text((12, 10), 'Raw turret sprites: native size x2, NOT mounted on Overlord; export palette temperattd.', fill='white')
    for row, prefix in enumerate(('chgatlingt', 'chdragontur')):
        for column, frame in enumerate(range(0, 32, 4)):
            sprite = Image.open(source / f'{prefix}-{frame:04}.png').convert('RGBA')
            sprite = sprite.crop(sprite.getbbox()).resize(tuple(v * 2 for v in sprite.crop(sprite.getbbox()).size), Image.Resampling.NEAREST)
            x, y = column * 140, row * 160 + 50
            image.paste(sprite, (x + (140-sprite.width)//2, y + 25), sprite)
            draw.text((x + 5, y + 5), f'{prefix} {frame}', fill='white')
    image.save(output)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('exports', type=Path)
    p.add_argument('output', type=Path)
    args = p.parse_args()
    build(args.exports, args.output)
