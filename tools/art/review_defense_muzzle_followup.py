"""Read-only composed registration checks, not live firing screenshots."""
from pathlib import Path
from PIL import Image,ImageDraw
import sys,re,math,json,hashlib
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools'))
from test_soviet_building_art import resolved,traits
O=ROOT/'art-source/defense-muzzle-followup';O.mkdir(exist_ok=True)
record={}
for actor,image in [('CRAM','cram.native'),('Ion_Defense','ion_defense.native'),('LTUR','ltur.native')]:
 rules=traits(resolved('--resolved-rules',actor));seq=traits(resolved('--resolved-sequences',image))
 def fields(text):return dict(re.findall(r'^\s+([^:\n]+): ([^\n]+)',text,re.M))
 mount=list(map(int,fields(rules['Turreted'])['Offset'].split(',')))
 arm=next(v for k,v in rules.items() if k.startswith('Armament'))
 forward,lateral,height=map(int,fields(arm)['LocalOffset'].split(','))
 board=Image.new('RGBA',(192*8,192*4),(111,110,106,255));sources={}
 for i in range(32):
  panel=Image.new('RGBA',(192,192),(111,110,106,255))
  for part in ('base','turret'):
   f=fields(seq[part]);path=ROOT/'mods/ca'/f['Filename'];sources[f['Filename']]=hashlib.sha256(path.read_bytes()).hexdigest()
   with Image.open(path) as src:
    w,h=map(int,src.info['FrameSize'].split(','));cols=src.width//w;idx=i if part=='turret' else 0
    frame=src.convert('RGBA').crop((idx%cols*w,idx//cols*h,idx%cols*w+w,idx//cols*h+h))
   scale=float(f['Scale']);frame=frame.resize((round(w*scale),round(h*scale)),Image.Resampling.NEAREST)
   dx,dy=map(float,f['Offset'].split(','));offsetY=(mount[1]-mount[2])*64/1448 if part=='turret' else 0
   panel.alpha_composite(frame,(round(96-frame.width/2+dx*scale),round(132-frame.height/2+dy*scale+offsetY)))
  a=i*math.pi/16;x=96+(-forward*math.sin(a)+lateral*math.cos(a))*128/1448
  y=132+(-forward*math.cos(a)-lateral*math.sin(a)+mount[1]-mount[2]-height)*64/1448
  d=ImageDraw.Draw(panel);d.text((4,4),f'{actor} {i*11.25:g}',fill='white')
  d.line((x-2,y,x+2,y),fill='#55ff66');d.line((x,y-2,x,y+2),fill='#55ff66')
  board.alpha_composite(panel,(i%8*192,i//8*192))
 board.save(O/(actor.lower()+'-all32.png'));record[actor]={'sources_sha256':sources,'local_offset':[forward,lateral,height],'mount':mount}
(O/'registration.json').write_text(json.dumps(record,indent=2)+'\n')
print('Composed32-facing muzzle markers for CRAM, Ion Defense, Nod Laser; original art untouched.')
