"""Layout review using actual 128x64 map projection, not a live screenshot.
Preserves original PNGs; draws the resolved engine selection and mount over all32 directions.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import sys,re,json
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
from test_soviet_building_art import resolved,traits
D=ROOT/'art-source/nod sam site/integration/size-60-raised';D.mkdir(exist_ok=True)
a=traits(resolved('--resolved-rules','NSAM'))
seq=traits(resolved('--resolved-sequences','nsam.native'))
grid=(ROOT/'mods/ca/mod.yaml').read_text();W,H=map(int,re.search(r'TileSize: (\d+),(\d+)',grid).groups());S=1448
assert 'UseClassicPerspectiveFudge: false' in a['BodyOrientation']
mount=tuple(map(int,re.search(r'Offset: ([^\n]+)',a['Turreted'])[1].split(',')))
mx=mount[1]*W/S;my=(-mount[0]-mount[2])*H/S
base=Image.open(ROOT/'mods/ca/bits/nod/buildings/sam-base.png').convert('RGBA')
bib=Image.open(ROOT/'mods/ca/bits/nod/buildings/bib2x2.png').convert('RGBA')
sheet=Image.open(ROOT/'mods/ca/bits/nod/buildings/sam-turret-4.png').convert('RGBA')
def pair(t):return tuple(map(float,re.search(r'Offset: ([^\n]+)',t)[1].split(',')))
bo=pair(seq['base']);to=pair(seq['turret-4'])
scale=float(re.search(r'Scale: ([^\n]+)',seq['base'])[1])
smallbib=Image.open(ROOT/'mods/ca/bits/nod/buildings/bib1x1.png').convert('RGBA')
bounds=tuple(map(int,re.search(r'DecorationBounds: ([^\n]+)',a['IsometricSelectable'])[1].split(',')))
height=int(re.search(r'DecorationHeight: (\d+)',a['IsometricSelectable'])[1])
def view(i,old=False):
 im=Image.new('RGBA',(320,300),(105,105,102,255));ground=(160,219)
 sc=1 if old else scale
 bO=(-5,-53) if old else bo
 foundation=bib if old else smallbib
 im.alpha_composite(foundation,(ground[0]-foundation.width//2,ground[1]+foundation.width//4-foundation.height))
 scaledbase=base.resize((round(256*sc),round(256*sc)),Image.Resampling.NEAREST)
 im.alpha_composite(scaledbase,(round(ground[0]+sc*(-128+bO[0])),round(ground[1]+sc*(-128+bO[1]))))
 turret=sheet.crop((i%8*256,i//8*256,i%8*256+256,i//8*256+256))
 x,y=(-317*W/S,-1493*H/S) if old else (mx,my)
 turret=turret.resize((round(256*sc),round(256*sc)),Image.Resampling.NEAREST)
 im.alpha_composite(turret,(round(ground[0]+sc*(-128+to[0])+x),round(ground[1]+sc*(-128+to[1])+y)))
 b=(2896,1448,0,0) if old else bounds;h=160 if old else height
 cx=ground[0]+int(b[2]*W/S);cy=ground[1]+int(b[3]*H/S)
 left=(cx-int(b[0]*W/(2*S)),cy);right=(left[0]+int(b[0]*W/S),cy)
 top=(cx,cy-int(b[1]*H/(2*S)));bottom=(cx,top[1]+int(b[1]*H/S))
 poly=[(top[0],top[1]-h),(left[0],left[1]-h),left,bottom,right,(right[0],right[1]-h)]
 d=ImageDraw.Draw(im);d.line(poly+[poly[0]],fill='#6cff6c',width=1)
 # Cyan marks fixed bearing center; magenta marks rendered rotation origin.
 for px,py,col in [(ground[0]+sc*(120-128+bO[0]),ground[1]+sc*(115-128+bO[1]),'cyan'),(ground[0]+x,ground[1]+y,'magenta')]:
  px,py=round(px),round(py);d.line((px-3,py,px+3,py),fill=col);d.line((px,py-3,px,py+3),fill=col)
 d.text((5,5),('BEFORE' if old else 'CORRECTED')+f' / engine facing {i*11.25:g}',fill='white')
 return im
board=Image.new('RGBA',(640,600))
for row,i in enumerate((0,12)):
 for col,old in enumerate((True,False)):board.paste(view(i,old),(col*320,row*300))
board.save(D/'before-after.png')
all32=Image.new('RGBA',(320*8,300*4))
for i in range(32):all32.paste(view(i),(i%8*320,i//8*300))
all32.save(D/'all32-selection.png')
(D/'registration.json').write_text(json.dumps({'grid':[W,H],'tile_scale':S,'old_mount_pixel':[-317*W/S,-1493*H/S],'corrected_mount_pixel':[mx,my],'runtime_scale':scale,'fixed_bearing_pixel':[(120-128+bo[0])*scale,(115-128+bo[1])*scale],'selection_bounds':bounds,'selection_height':height,'images_are':'Engine-projection layout previews, not live screenshots'},indent=2)+'\n')
print('Nod SAM actual-grid registration review written:',D)
