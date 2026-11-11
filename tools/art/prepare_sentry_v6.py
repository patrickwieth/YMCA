"""Texture-only pass: named source cuts, no procedural/painted texture content."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/soviet-sentry/attempt-06';TEX=OUT/'textures';TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
im=Image.open(source).convert('RGBA')
# UL LL LR UR. Separate cuts exclude adjacent sensors/barrels where possible.
cuts={
 'roof':((28,42),(115,73,139,87,162,78,137,63)),
 'rear-roof':((14,56),(143,64,166,78,179,82,156,68)),
 'red-slope':((32,22),(113,82,110,89,121,95,128,87)),
 'sensor-metal':((20,8),(110,91,108,95,117,100,120,96)),
 'shoulder':((10,30),(127,89,120,100,128,104,138,93)),
 'front-edge':((7,34),(130,107,129,132,136,134,137,109)),
 'steel-tube':((48,3),(70,112,71,114,94,101,93,99)),
 'red-edge':((48,3),(99,94,99,96,124,109,124,106)),
 'red-scuff':((5,10),(126,112,126,121,128,122,128,114)),
 'red-bearing-front':((48,14),(104,137,105,148,138,155,138,145)),
 'red-bearing-side':((32,14),(140,145,140,153,158,143,158,135)),
 'red-bearing-back':((48,14),(106,138,107,148,136,154,136,145)),
}
patches={}
for name,(size,quad) in cuts.items():
 patches[name]=im.transform(size,Image.Transform.QUAD,quad,Image.Resampling.NEAREST)
 patches[name].save(TEX/(name+'.png'))
# Cut out neighboring-object silhouettes from the rectified crops. Copy only
# metal/paint pixels onto a source-pixel backing; do not paint synthetic wear.
# In particular, a copied bolt/barrel outline must not become another fake seam.
filters={
 'shoulder':((124,73),'metal'), 'front-edge':((124,73),'metal'),
 'red-slope':((123,107),'red'), 'rear-roof':((124,73),'metal-rust'),
 'red-bearing-front':((118,144),'red'),
 'red-bearing-side':((128,110),'red'),
 'red-bearing-back':((118,144),'red'),
}
for name,(sample,kind) in filters.items():
 patch=patches[name];backing=im.getpixel(sample)
 for y in range(patch.height):
  for x in range(patch.width):
   r,g,b,a=patch.getpixel((x,y))
   keep=(r>g*1.25 and r>b*1.15 and r>=58) if kind=='red' else (min(r,g,b)>=49 and (kind=='metal-rust' or max(r,g,b)-min(r,g,b)<25))
   if not keep:patch.putpixel((x,y),backing)
 patch.save(TEX/(name+'.png'))
# Continuous receiver paint with only copied edge/scuff details. No copied gun sockets.
receiver=Image.new('RGBA',(48,16),im.getpixel((123,107)))
receiver.paste(patches['red-edge'],(0,0));receiver.paste(patches['red-scuff'],(42,5))
receiver.save(TEX/'receiver.png')
# A continuous strip around the bearing, avoiding narrow repeated polygon patterns.
atlas=Image.new('RGBA',(176,14))
x=0
for name in ('red-bearing-front','red-bearing-side','red-bearing-back','red-bearing-front'):
 atlas.paste(patches[name],(x,0));x+=patches[name].width
atlas.save(TEX/'bearing.png')
source_colors=set(im.getdata())
for path in TEX.glob('*.png'):
 assert set(Image.open(path).convert('RGBA').getdata())<=source_colors,path
(OUT/'texture-provenance.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),
 'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'cuts':cuts,
 'cutout_filters':filters,'cutout_note':'Excluded neighboring contours replaced with the indicated verbatim source pixel; retained wear is unchanged source artwork.',
 'receiver_fill_pixel':[123,107], 'receiver_pastes':[['red-edge',0,0],['red-scuff',42,5]],
 'bearing_strip_order':['red-bearing-front','red-bearing-side','red-bearing-back','red-bearing-front'],
 'sampling':'NEAREST only; no painting, random/noise patterns or generated artwork'},indent=2)+'\n')
sheet=Image.new('RGB',(640,480),'#77736e');d=ImageDraw.Draw(sheet)
for i,path in enumerate(sorted(TEX.glob('*.png'))):
 tile=Image.open(path);tile.thumbnail((120,70),Image.Resampling.NEAREST)
 x,y=(i%4)*160,(i//4)*120;sheet.paste(tile,(x,y+18),tile);d.text((x,y),path.stem,fill='white')
sheet.resize((1280,960),Image.Resampling.NEAREST).save(OUT/'source-cuts-2x.png')
print('V6 source texture cuts verified.')
