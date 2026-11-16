"""Review actual building art against location-relative cell coordinates (not selection boxes)."""
import json, math, sys
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
after='--after' in sys.argv
OUT=ROOT/'art-source/bib-occupancy/ground-review'
if after:OUT=OUT/'after'
OUT=next((Path(a.split('=',1)[1]) for a in sys.argv if a.startswith('--output=')),OUT)
OUT.mkdir(parents=True,exist_ok=True)
source=next((Path(a.split('=',1)[1]) for a in sys.argv if a.startswith('--input=')),ROOT/'.art-validation/occupancy-completion'/('after.json' if after else 'before.json'))
entries=json.loads(source.read_text())
seen=set();groups={}
for e in entries:
 file=e['File'] or ''
 if not file.startswith('bits/') or not file.endswith('.png') or '/buildings/' not in file:continue
 key=(file,e['Offset'],e['Scale'],str(e['BuildingCenterOffset']))
 if key in seen:continue
 seen.add(key);groups.setdefault(file.split('/')[1],[]).append(e)
for faction,actors in groups.items():
 for start in range(0,len(actors),8):
  sheet=Image.new('RGB',(2048,1024),(45,48,45))
  for index,e in enumerate(actors[start:start+8]):
   canvas=Image.new('RGBA',(640,640),(55,59,55,255));cx,cy=320,400
   def paste(file,anchor,offset,scale,extra=(0,0)):
    im=Image.open(ROOT/'mods/ca'/file).convert('RGBA');s=float(scale);x,y=map(float,offset.split(',')[:2]);px=cx+extra[0]+(x-im.width/2)*s;py=cy+extra[1]+(y-(im.height-im.width/4 if anchor=='Bottom' else im.height/2))*s
    im=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.NEAREST);canvas.alpha_composite(im,(round(px),round(py)))
   for b in e['Bibs']:paste(b['File'],b['Anchor'],b['Offset'],b['Scale'],b['ScreenOffset'])
   dx,dy=e['BuildingDimensions'];lx,ly,lz=e['BuildingCenterOffset'];ax=724+362*(dx-dy)+lx;ay=362*(dx+dy)+ly-lz
   cells={(p['X'],p['Y']):p['Type'] for p in e['FootprintCells']};positions={};draw=ImageDraw.Draw(canvas)
   for y in range(min(p[1] for p in cells)-1,max(p[1] for p in cells)+2):
    for x in range(min(p[0] for p in cells)-1,max(p[0] for p in cells)+2):
     sx=cx+(724+724*(x-y)-ax)*128/1448;sy=cy+(724+724*(x+y)-ay)*64/1448;positions[x,y]=(sx,sy);color=(255,90,80) if cells.get((x,y)) in ('x','X','+') else (80,135,145) if (x,y) in cells else (75,78,75)
     draw.line([(sx,sy-32),(sx+64,sy),(sx,sy+32),(sx-64,sy),(sx,sy-32)],fill=color,width=1)
   paste(e['File'],e['Anchor'],e['Offset'],e['Scale'])
   draw=ImageDraw.Draw(canvas)
   for (x,y),(sx,sy) in positions.items():draw.text((sx-12,sy-5),f'{x},{y}',fill='white',stroke_width=1,stroke_fill='black')
   draw.text((12,12),e['Actor'],fill='white',stroke_width=1,stroke_fill='black')
   draw.text((12,30),'exit '+str([x['Cell'] for x in e['Exits']])+' dock '+str(e['Docks']),fill='white')
   for row,y in enumerate(range(min(p[1] for p in cells),max(p[1] for p in cells)+1)):
    text=' '.join(cells.get((x,y),'_') for x in range(min(p[0] for p in cells),max(p[0] for p in cells)+1))
    draw.text((460,20+16*row),text,fill='white')
   canvas.save(OUT/(e['Actor']+'.png'));sheet.paste(canvas.resize((512,512)),((index%4)*512,(index//4)*512))
  sheet.save(OUT/f'{faction}-{start//8}.png')
print({k:len(v) for k,v in groups.items()})
