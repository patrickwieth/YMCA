"""Compare old/new native voxel renders over the byte-identical base."""
from pathlib import Path
from PIL import Image,ImageDraw
import argparse
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);a=p.parse_args()
F=Path(__file__).resolve().parents[2]/'art-source/gdi missile tower';O=F/f'pass-{a.iteration:02d}';old=F/'pass-14'
def frame(folder,i):
 im=Image.new('RGBA',(224,224),(45,49,46,255));im.alpha_composite(Image.open(folder/'base.png').convert('RGBA'),(48,80));im.alpha_composite(Image.open(folder/f'engine-{i:02d}.png').convert('RGBA'));return im
sheet=Image.new('RGBA',(224*4,224*8),(45,49,46,255));frames=[]
for i in range(32):
 im=frame(O,i);sheet.alpha_composite(im,(i%4*224,i//4*224));frames.append(im.convert('RGB'))
sheet.convert('RGB').save(O/'engine-rotation-review.png')
frames[0].save(O/'engine-rotation.gif',save_all=True,append_images=frames[1:],duration=125,loop=0)
review=Image.new('RGBA',(224*4,224*2),(45,49,46,255))
for j,i in enumerate((0,8,16,24)):
 review.alpha_composite(frame(old,i),(j*224,0));review.alpha_composite(frame(O,i),(j*224,224))
review.convert('RGB').resize((1792,896),Image.Resampling.NEAREST).save(O/'before-after-four-views-2x.png')
