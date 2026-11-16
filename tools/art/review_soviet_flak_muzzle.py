"""Actual-grid32-facing flak muzzle layout; markers are diagnostics, not a live shot."""
from pathlib import Path
from PIL import Image,ImageDraw
import math,json
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art-source/soviet flak/muzzle-registration';O.mkdir(exist_ok=True)
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/flak-base.png').convert('RGBA')
sheet=Image.open(ROOT/'mods/ca/bits/soviet/buildings/flak-turret32.png').convert('RGBA')
board=Image.new('RGBA',(192*8,216*4),(111,110,106,255));rows=[]
forward=.10+math.cos(math.radians(42))*3.84;height=2.14+math.sin(math.radians(42))*3.84
for i in range(32):
 a=i*math.pi/16;angle=(20+i)*math.pi/16
 x=forward*math.sin(angle);y=-forward*math.cos(angle)
 target=(20*(x+y)/math.sqrt(2),20*(x-y)/math.sqrt(8)-20*height*math.sqrt(3)/2)
 # WAngle rotation sends positive local X toward negative world X as yaw rises.
 new=(-669*math.sin(a)*128/1448,(-669*math.cos(a)-243-1603)*64/1448)
 old=(-1337*math.sin(a)*128/1448,(-1337*math.cos(a)*math.sin(85*math.tau/1024)-243-1603)*64/1448)
 frame=Image.alpha_composite(base,sheet.crop((i%8*192,i//8*192,i%8*192+192,i//8*192+192)))
 ox=i%8*192;oy=i//8*216+24;board.alpha_composite(frame,(ox,oy));d=ImageDraw.Draw(board)
 d.text((ox+3,oy-20),f'{i*11.25:g} deg / red old',fill='white')
 for p,color in [(old,'#ff5555'),(new,'#55ff66')]:
  px=ox+96+p[0];py=oy+136+p[1];d.line((px-3,py,px+3,py),fill=color);d.line((px,py-3,px,py+3),fill=color)
 rows.append({'facing':i,'reference_mouth':target,'new_projected':new,'old_projected':old})
board.save(O/'all32-muzzle-markers.png');(O/'projection.json').write_text(json.dumps({'tile_size':[128,64],'tile_scale':1448,'rows':rows,'live_screenshot':False},indent=2)+'\n')
print('Flak muzzle marker comparison written; source sprites unchanged.')
