"""Side-by-side geometry study; crop for presentation only, original frames untouched."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art-source/soviet-sentry/attempt-03'
reference = Image.open(ROOT / 'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA').crop((43,58,187,161))
items = [(reference, 'Reference crop')]
for name,label in [('geometry-source-colors.png','V3 geometry - no textures'),('geometry-neutral.png','V3 neutral')]:
    im=Image.open(OUT/name).convert('RGBA')
    bbox=im.getbbox()
    assert bbox and bbox[0]>0 and bbox[1]>0 and bbox[2]<im.width and bbox[3]<im.height
    items.append((im.crop(bbox),label))
image=Image.new('RGB',(576,150),'#77736e')
draw=ImageDraw.Draw(image)
for i,(im,label) in enumerate(items):
    image.paste(im,(i*192+(192-im.width)//2,12),im)
    draw.text((i*192+5,132),label,fill='white')
image.save(OUT/'comparison-native.png')
image.resize((1728,450),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
