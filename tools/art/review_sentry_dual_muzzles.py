"""Review both independently calibrated muzzle centers on frozen native turret frames.
Markers are diagnostics, not replacement muzzle artwork or a live screenshot.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import math,json
ROOT=Path(__file__).resolve().parents[2];O=ROOT/'art-source/soviet-sentry/dual-muzzles'
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/experimental/sentry-base-native.png').convert('RGBA')
sheet=Image.open(ROOT/'mods/ca/bits/soviet/buildings/experimental/sentry-v13-native32.png').convert('RGBA')
reference=json.loads((O/'muzzle-pixels.json').read_text())['points']
board=Image.new('RGBA',(160*8,160*4),(111,110,106,255));errors=[]
for i in range(32):
 ox=i%8*160;oy=i//8*160;ground=(ox+80,oy+118)
 board.alpha_composite(base,(round(ground[0]-base.width/2),round(ground[1]-base.height/2-3)))
 frame=sheet.crop((i%8*120,i//8*120,i%8*120+120,i//8*120+120))
 board.alpha_composite(frame,(ground[0]-64,round(ground[1]-60-24-112*64/1448)))
 draw=ImageDraw.Draw(board);draw.text((ox+3,oy+5),f'{i*11.25:g} deg',fill='white')
 angle=i*math.pi/16
 for b,lateral in enumerate((86,-86)):
  x=(-547*math.sin(angle)+lateral*math.cos(angle))*128/1448
  y=(-547*math.cos(angle)-lateral*math.sin(angle)-112-655)*64/1448
  expected=reference[i][b];errors.append(math.hypot(x-expected[0],y-expected[1]))
  px=ground[0]+x;py=ground[1]+y;color=('#55ff66','#55ddff')[b]
  draw.line((px-2,py,px+2,py),fill=color);draw.line((px,py-2,px,py+2),fill=color)
board.save(O/'all32-dual-muzzles.png')
assert max(errors)<.6,max(errors)
(O/'registration.json').write_text(json.dumps({'local_offsets':[[547,86,655],[547,-86,655]],'turret_offset':[0,0,112],'grid':[128,64],'maximum_float_projection_error':max(errors),'shared_phase':True,'muzzle_tick_ms':40,'muzzle_frames':2,'native_art_modified':False},indent=2)+'\n')
print('Two cluster centers /64 facing projections checked:',max(errors))
