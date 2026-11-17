"""Publish the five approved-geometry ammo exports and synchronized previews."""
from pathlib import Path
import shutil,json
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/nod sam site';OUT=F/'ammo-variants'
def rgba(p):return Image.open(p).convert('RGBA')
assert rgba(F/'pass-12/turret-000-raw.png').tobytes()==rgba(OUT/'4/full-control-raw.png').tobytes()
base=F/'pass-12/review/base.png';shutil.copy2(base,OUT/'base.png')
for count in range(5):
 folder=OUT/str(count)/'review'
 assert rgba(folder/'base.png').tobytes()==rgba(base).tobytes()
 shutil.copy2(folder/'turret32.png',OUT/f'turret-{count}.png')
assert (OUT/'turret-4.png').read_bytes()==(F/'pass-12/review/turret32.png').read_bytes()
board=Image.new('RGBA',(1280,560),(119,115,110,255));draw=ImageDraw.Draw(board)
for row,facing in enumerate((0,16)):
 for count in range(5):
  board.alpha_composite(rgba(OUT/str(count)/'review'/f'assembly-{facing:03d}.png'),(count*256,row*280+24));draw.text((count*256+5,row*280+5),f'{count} missiles / {facing*11.25:g} degrees',fill='white')
board.save(OUT/'variants-native.png');board.resize((2560,1120),Image.Resampling.NEAREST).save(OUT/'variants-2x.png')
views=[]
for facing in range(32):
 im=Image.new('RGBA',(1280,280),(119,115,110,255));draw=ImageDraw.Draw(im)
 for count in range(5):
  im.alpha_composite(rgba(OUT/str(count)/'review'/f'assembly-{facing:03d}.png'),(count*256,24));draw.text((count*256+5,5),f'{count} missiles / {facing*11.25:g} degrees',fill='white')
 views.append(im.convert('RGB'))
colors=sorted({c for im in views for c in im.getdata()});palette=Image.fromarray(np.array([colors],dtype=np.uint8)).quantize(colors=256,dither=Image.Dither.NONE)
frames=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in views];frames[0].save(OUT/'all-five-rotation.gif',save_all=True,append_images=frames[1:],duration=110,loop=0)
print('NOD_SAM_FIVE_AMMO_STATES_READY')
