"""Reconstruct clipped contours using source colors and bounded native structural cuts.
Originals and previous passes remain untouched. Hidden lower surfaces are inferred.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json, hashlib
R=Path(__file__).resolve().parents[2];F=R/'art-source/soviet chem tower';O=F/'pass-05';O.mkdir(exist_ok=True)
src=Image.open(F/'chemtower.png').convert('RGBA');a=np.array(src)
colors=np.unique(a[a[:,:,3]>0,:3],axis=0).astype(int)
def tone(rgb):
 c=colors[np.argmin(((colors-np.array(rgb))**2).sum(axis=1))]
 return (*map(int,c),255)
out=Image.new('RGBA',(128,208));d=ImageDraw.Draw(out)
# Complete only the missing dome cap; the original starts at a horizontal cut y48.
for y in range(43,49):
 for x in range(53,76):
  if ((x-64)/11)**2+((y-54)/11)**2<=1:
   light=1-((x-60)**2+(y-47)**2)**.5/23
   d.point((x,y),fill=tone((56+46*light,54+43*light,47+39*light)))
# Preserve the original diagonal rear studs; floating reconstructed cap strips
# were rejected during review. Only the central dome requires a new contour.
out.alpha_composite(src)
# The original has a hard cut through the lower wall at y175. Extend its actual
# panel layout with restrained tonal steps, not another copy of vents/doorways.
# The curved floor follows the tower's existing native circumference.
curve=[(17,168),(27,173),(40,178),(53,181),(65,182),(79,181),(94,177),(106,172),(113,168)]
def edge(x):return int(round(np.interp(x,[p[0] for p in curve],[p[1] for p in curve])))
for x in range(17,114):
 bottom=edge(x)
 # Source colors are sampled from the same column immediately above the cut.
 strip=a[171:176,x];valid=strip[strip[:,3]>0]
 if not len(valid):continue
 base=valid[-1,:3].astype(float)
 for y in range(176,bottom+1):
  factor=1-.035*(y-176)
  d.point((x,y),fill=tone(base*factor))
# Native ring cut supplies continuous, source-detailed bevel and lower rim.
# Its existing curvature is retained rather than warped into a rectangle.
poly=[(23,140),(44,146),(65,149),(82,146),(106,139),(106,142),(83,149),(65,152),(43,149),(23,143)]
mask=Image.new('L',src.size);ImageDraw.Draw(mask).polygon(poly,fill=255)
ring=src.copy();ring.putalpha(Image.composite(src.getchannel('A'),Image.new('L',src.size),mask))
# Shift to y172..185, close the wall behind it; side piers terminate separately.
out.alpha_composite(ring,(0,33))
d=ImageDraw.Draw(out)
# Preserve the supplied outer pier ends and the right-hand cylindrical fittings.
# Their existing native bevels are preferable to inferred solid cap polygons.
for rect in [(6,160,23,176),(107,160,122,176)]:
 out.alpha_composite(src.crop(rect),(rect[0],rect[1]))
# Remove only isolated one/two-pixel extraction debris, not connected mechanisms.
from collections import deque
arr=np.array(out);seen=set();removed=[]
for y,x in zip(*np.where(arr[:,:,3]>0)):
 if (x,y) in seen:continue
 q=deque([(x,y)]);seen.add((x,y));component=[]
 while q:
  xx,yy=q.popleft();component.append((xx,yy))
  for nx,ny in ((xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)):
   if 0<=nx<128 and 0<=ny<208 and arr[ny,nx,3] and (nx,ny) not in seen:seen.add((nx,ny));q.append((nx,ny))
 if len(component)<=2:
  for xx,yy in component:arr[yy,xx]=0
  removed+=component
out=Image.fromarray(arr);out.save(O/'chemical-tower.png')
for bg,name in [((42,46,42,255),'dark'),((135,132,112,255),'ground')]:
 canvas=Image.new('RGBA',(384,208),bg)
 for i,im in enumerate((src,Image.open(F/'pass-04/chemical-tower.png').convert('RGBA'),out)):canvas.alpha_composite(im,(128*i,0))
 canvas.convert('RGB').resize((1152,624),Image.Resampling.NEAREST).save(O/f'comparison-{name}-3x.png')
(O/'reconstruction.json').write_text(json.dumps({'source_sha256':hashlib.sha256((F/'chemtower.png').read_bytes()).hexdigest(),'ring_source_polygon':poly,'ring_translation':[0,33],'removed_isolated_pixels':[(int(x),int(y)) for x,y in removed],'policy':'Native source colors; bounded ring cut; inferred dome cap and clipped lower wall. Original outer piers/fittings retained. No repeated facade or vents.'},indent=2)+'\n')
print(O)
