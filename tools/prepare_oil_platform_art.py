"""Cut out the supplied oil platform without repainting its source pixels."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib,json,shutil,math
root=Path(__file__).resolve().parents[1]
source=Path('E:/SharedShitzens/src/openRA/retrodiffusion/misc/soviet_oil_platform_...-358960086-0.png')
archive=root/'art-source/oil-platform';archive.mkdir(parents=True,exist_ok=True)
original=archive/'original.png'
if original.exists() and original.read_bytes()!=source.read_bytes():raise SystemExit('Archived original differs.')
shutil.copy2(source,original)
im=Image.open(source).convert('RGBA');mask=Image.new('L',im.size);draw=ImageDraw.Draw(mask)
draw.rectangle((38,86,218,225),fill=255)
draw.rectangle((152,68,164,105),fill=255)
draw.rectangle((99,76,114,104),fill=255)
draw.polygon([(73,64),(102,79),(106,101),(91,110),(72,85)],fill=255)
draw.polygon([(164,90),(209,57),(211,101),(200,109),(177,119),(164,115)],fill=255)
draw.polygon([(110,63),(117,28),(118,21),(124,17),(125,9),(131,9),(132,17),(139,24),(143,58),(147,62),(147,76),(153,98),(148,113),(105,113),(108,80)],fill=255)
# Source uses disjoint cool blue/grey sky and water colors. Retain all warm
# structural colors, including near-black trusses, pipes and muted highlights.
pixels=im.load()
for y in range(im.height):
 for x in range(im.width):
  r,g,b,a=pixels[x,y]
  if not mask.getpixel((x,y)) or b>r and g>r:pixels[x,y]=(r,g,b,0)
# Foam occluded the original pier ends. Preserve this first cut for comparison,
# then close only those three bounded lower faces using clean source columns.
previous=archive/'cutout-before-pier-repair.png'
if not previous.exists():im.save(previous)
clean=im.copy()
for x0,x1,top,edge,depth,sample0,sample1,padbottom in [
 (54,76,181,190,4,169,180,201),
 (114,141,211,217,4,196,207,226),
 (179,201,190,190,4,177,188,201)]:
 center=(x0+x1)/2;radius=(x1-x0)/2
 for x in range(x0-3,x1+4):
  bottom=edge+round(depth*math.sqrt(max(0,1-((x-center)/radius)**2))) if x0<=x<=x1 else top-1
  for y in range(top,padbottom+1):
   r,g,b,a=pixels[x,y]
   if y>bottom:pixels[x,y]=(r,g,b,0)
   elif not a:
    sample=sample0+(y-top)%(sample1-sample0+1)
    color=clean.getpixel((x,sample))
    if color[3]!=255:raise ValueError('Pier sample must be solid source material')
    pixels[x,y]=color
   if y==bottom and x0<=x<=x1:
    # A single muted source-color lower rim, not a black cut-off or loose teeth.
    pixels[x,y]=(64,54,58,255)
out=root/'mods/ca/bits/terrain/environment/oil-platform.png';im.save(out)
review=Image.new('RGBA',im.size,(70,110,70,255));review.alpha_composite(im)
review.resize((768,768),Image.Resampling.NEAREST).save(archive/'cutout-review.png')
(archive/'provenance.json').write_text(json.dumps({'source':source.name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'operation':'Bounded component masks and cool-background removal. Three foam-occluded lower pier ends closed with same-column clean source cuts and a source-color rim. All other source pixels, 256x256 canvas and native resolution preserved.'},indent=2)+'\n')
