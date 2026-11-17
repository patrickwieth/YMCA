"""Composed native grid/actor registration review; not a live screenshot."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/bib-grid-alignment'
old={e['Actor']:e for e in json.loads((OUT/'resolved-before.json').read_text())};new={e['Actor']:e for e in json.loads((OUT/'resolved-after.json').read_text())}
for e in json.loads((OUT/'bunkers-after.json').read_text()):
 new[e['Actor']]=e;old[e['Actor']]=dict(e,BuildingCenterOffset=[0,-724,0])
names=['chbunker','chbunker2','chpower','apwr.soviet','afac.nod','reac'];sheet=Image.new('RGBA',(1200,480*len(names)),(56,64,57,255));draw=ImageDraw.Draw(sheet)
def origin(e):
 dx,dy=e['BuildingDimensions'];x,y,z=e['BuildingCenterOffset'];return ((724+362*(dx-dy)+x)*128/1448,(724+362*(dx+dy-2)+y-z)*64/1448)
def blit(file,scale,anchor,offset,x,y):
 im=Image.open(ROOT/'mods/ca'/file).convert('RGBA');ox,oy=map(float,offset.split(',')[:2]);px=-im.width/2+ox;py=(-im.height+im.width/4 if anchor=='Bottom' else -im.height/2)+oy
 if scale!=1:im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.NEAREST)
 sheet.alpha_composite(im,(round(x+px*scale),round(y+py*scale)))
for row,name in enumerate(names):
 for col,entries in enumerate([old,new]):
  e=entries[name];ox=col*600+245;oy=row*480+270
  for u in range(-3,7):
   for v in range(-3,7):
    x=ox+64*(u-v+1);y=oy+32*(u+v+1)
    if col*600+5<x<(col+1)*600-5 and row*480+80<y<(row+1)*480-45:
     draw.line([(x,y-32),(x+64,y),(x,y+32),(x-64,y),(x,y-32)],fill=(85,99,87))
  x,y=origin(e);x+=ox;y+=oy
  for bib in e['Bibs']:
   sx,sy=bib['ScreenOffset'];blit(bib['File'],float(bib['Scale']),bib['Anchor'],bib['Offset'],x+sx,y+sy)
  blit(e['File'],float(e['Scale']),e['Anchor'],e['Offset'],x,y)
  if name.startswith('chbunker'):
   for u,v in [(0,-1),(1,-1),(-1,0),(-1,1),(2,0),(2,1),(0,2),(1,2)]:
    mx=ox+64*(u-v+1);my=oy+32*(u+v+1);draw.ellipse((mx-3,my-3,mx+3,my+3),fill='#ffdc60')
  draw.text((col*600+10,row*480+10),name+' / '+('BEFORE' if col==0 else 'GRID ALIGNED'),fill='white')
  if name.startswith('chbunker'):draw.text((col*600+10,row*480+28),'Yellow: unchanged mine-cell centers',fill='#ffdc60')
sheet.save(OUT/'before-after.png')
