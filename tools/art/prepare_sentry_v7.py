"""Hand-selected source cuts at small texel scale, not procedural weathering."""
from pathlib import Path
import hashlib,json,shutil
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-07';TEX=OUT/'textures';TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
im=Image.open(source).convert('RGBA')
for path in (BASE/'attempt-06/textures').glob('*.png'):shutil.copyfile(path,TEX/path.name)
records=[]
def quad(name,size,points):
 patch=im.transform(size,Image.Transform.QUAD,points,Image.Resampling.NEAREST)
 records.append({'name':name,'size':size,'quad':points})
 return patch
# Previous cut missed the upper-right portion of the red plate and enlarged only
# its left-hand scratch cluster. Use the entire plate, not a close-up of the wear.
red=quad('red-slope',(32,22),(116,80,110,89,122,95,133,87))
for y in range(red.height):
 for x in range(red.width):
  r,g,b,a=red.getpixel((x,y))
  if not(r>g*1.25 and r>b*1.15 and r>=58):red.putpixel((x,y),im.getpixel((123,107)))
red.save(TEX/'red-slope.png')
# Source metal cover: omit the black recessed lower surround already in geometry.
cover=quad('sensor-metal',(20,8),(108,90,107,93,118,98,120,95))
for y in range(cover.height):
 for x in range(cover.width):
  r,g,b,a=cover.getpixel((x,y))
  if min(r,g,b)<49 or max(r,g,b)-min(r,g,b)>25:cover.putpixel((x,y),im.getpixel((124,73)))
cover.save(TEX/'sensor-metal.png')
# The solid frame's top face occludes the recessed glass mesh. Put the metal
# source cover on that visible face with a narrow, source-dark border.
frame=Image.new('RGBA',(24,10),im.getpixel((146,118)))
frame.paste(cover,(2,1));frame.save(TEX/'sensor-frame.png')
# Quiet metal backing plus a few intact, small source wear clusters. 1:1 pasted
# pixels, not the whole sloping frame with its neighboring black/red silhouettes.
compositions={
 'shoulder':{'size':(10,30),'fill':(124,73),'cuts':[
  ((126,92,131,95),(2,2)),((130,104,135,109),(3,12)),((131,126,135,130),(5,24))]},
 'front-edge':{'size':(7,34),'fill':(124,73),'cuts':[
  ((131,108,137,114),(0,5)),((131,126,135,130),(1,25))]},
}
for name,spec in compositions.items():
 panel=Image.new('RGBA',spec['size'],im.getpixel(spec['fill']))
 for rect,dest in spec['cuts']:
  patch=im.crop(rect);mask=Image.new('L',patch.size)
  for y in range(patch.height):
   for x in range(patch.width):
    r,g,b,a=patch.getpixel((x,y))
    if min(r,g,b)>=49 and max(r,g,b)-min(r,g,b)<25:mask.putpixel((x,y),255)
  panel.paste(patch,dest,mask)
 panel.save(TEX/(name+'.png'))
# Better rear-roof extraction: it is a narrow strip parallel to the main roof,
# not a rectangular crop across the main hatch's black edge.
rear=quad('rear-roof',(14,56),(141,63,164,77,179,81,155,67))
for y in range(rear.height):
 for x in range(rear.width):
  r,g,b,a=rear.getpixel((x,y))
  if min(r,g,b)<45:rear.putpixel((x,y),im.getpixel((124,73)))
rear.save(TEX/'rear-roof.png')
# Verify actual colors, including all source-cut backing pixels.
colors=set(im.getdata())
for path in TEX.glob('*.png'):
 assert set(Image.open(path).convert('RGBA').getdata())<=colors,path
(OUT/'texture-provenance.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),
 'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
 'inherits':'../attempt-06/texture-provenance.json','rectified_cuts':records,
 'sensor_frame':{'size':[24,10],'backing_pixel':[146,118],'paste':['sensor-metal',2,1]},
 'metal_compositions':compositions,'metal_cutout':'neutral pixels >=49, channel range <25; source-pixel backing',
 'red_backing_pixel':[123,107],'metal_backing_pixel':[124,73],
 'sampling':'NEAREST; small pasted wear clusters retain source pixel size'},indent=2)+'\n')
review=Image.new('RGB',(480,192),'#77736e');d=ImageDraw.Draw(review)
for i,name in enumerate(('red-slope','sensor-metal','shoulder','front-edge','rear-roof','roof')):
 patch=Image.open(TEX/(name+'.png'))
 review.paste(patch,(i*80+8,22),patch);d.text((i*80,3),name,fill='white')
review.resize((1440,576),Image.Resampling.NEAREST).save(OUT/'selected-source-cuts-3x.png')
print('V7: source-only texture composition verified.')
