"""Bounded cutout of the supplied Soviet nuclear plant; preserve source RGB."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
import shutil,hashlib,json
from collections import deque
root=Path(__file__).resolve().parents[1]
source=Path('E:/SharedShitzens/src/openRA/retrodiffusion/soviets/nuclear_power_plant-1114443554-0.png')
archive=root/'art-source/soviet-nuclear-plant';archive.mkdir(parents=True,exist_ok=True)
original=archive/'original.png'
if original.exists() and original.read_bytes()!=source.read_bytes():raise SystemExit('Archived original differs.')
shutil.copy2(source,original)
im=Image.open(source).convert('RGBA');mask=Image.new('L',im.size);d=ImageDraw.Draw(mask)
outline=[(20,147),(22,114),(27,98),(30,94),(36,94),(48,98),(48,87),(54,84),(55,75),(64,67),(78,62),(78,57),(82,52),(89,50),(94,50),(122,67),(138,37),(137,20),(141,12),(152,7),(167,8),(177,14),(181,20),(179,31),(180,60),(185,84),(192,90),(195,75),(195,52),(192,44),(193,37),(200,31),(214,29),(225,32),(233,38),(237,46),(234,52),(233,66),(236,90),(244,115),(247,128),(245,136),(235,139),(235,144),(250,147),(255,170),(255,210),(242,216),(221,223),(198,226),(176,218),(160,212),(149,206),(149,193),(142,187),(134,185),(136,178),(128,177),(125,173),(120,173),(117,177),(108,171),(100,168),(95,167),(96,157),(89,159),(88,163),(75,164),(73,166),(64,170),(57,172),(53,169),(45,166),(44,160),(44,155),(39,155),(31,159),(25,159),(21,155)]
d.polygon(outline,fill=255)
d.rectangle((248,100,255,146),fill=0)
im.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
# All structural parts form one connected silhouette. Small disconnected
# components here are courtyard scraps clipped around the removed figures.
alpha=im.getchannel('A');remaining={(x,y) for y in range(im.height) for x in range(im.width) if alpha.getpixel((x,y))};components=[]
while remaining:
 seed=remaining.pop();component=[seed];pending=deque([seed])
 while pending:
  x,y=pending.popleft()
  for dx in (-1,0,1):
   for dy in (-1,0,1):
    p=(x+dx,y+dy)
    if p in remaining:remaining.remove(p);pending.append(p);component.append(p)
 components.append(component)
for component in sorted(components,key=len,reverse=True)[1:]:
 for p in component:alpha.putpixel(p,0)
im.putalpha(alpha)
im.save(root/'mods/ca/bits/soviet/buildings/nuclear-power-plant.png')
review=Image.new('RGBA',im.size,(75,120,75,255));review.alpha_composite(im)
review.resize((1024,1024),Image.Resampling.NEAREST).save(archive/'cutout-review.png')
(archive/'provenance.json').write_text(json.dumps({'source':source.name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'operation':'Bounded alpha cutout. Main buildings, cooling towers and attached fenced substation retained; foreground compound, troops, vehicles and trees excluded. Source RGB and 256x256 canvas preserved.'},indent=2)+'\n')
