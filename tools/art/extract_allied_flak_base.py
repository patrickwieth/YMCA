"""Extract original visible pedestal pixels; reconstruct ONLY the occluded top.
The supplied composite PNG is never modified. No resize or palette conversion.
"""
from pathlib import Path
from PIL import Image,ImageDraw
from collections import deque
import json,hashlib,argparse
parser=argparse.ArgumentParser();parser.add_argument('--pass',dest='iteration',type=int,default=14);args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'art-source/allies flak';source=next(folder.glob('*.png'));out=folder/f'pass-{args.iteration:02d}';out.mkdir(exist_ok=True)
im=Image.open(source).convert('RGBA');pixels=im.load();w,h=im.size
queue=deque([(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]+[(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]);seen=set()
while queue:
 x,y=queue.popleft()
 if (x,y) in seen:continue
 seen.add((x,y))
 if min(pixels[x,y][:3])<243:continue
 pixels[x,y]=(*pixels[x,y][:3],0)
 for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
  if 0<=xx<w and 0<=yy<h:queue.append((xx,yy))
mask=Image.new('L',im.size);d=ImageDraw.Draw(mask)
d.polygon([(0,54),(30,54),(39,49),(39,59),(46,65),(56,69),(72,69),(86,64),(93,58),(93,53),(98,56),(128,56),(128,128),(0,128)],fill=255)
visible=im.copy();visible.putalpha(Image.frombytes('L',im.size,bytes(a*b//255 for a,b in zip(im.getchannel('A').tobytes(),mask.tobytes()))))
base=Image.new('RGBA',im.size);d=ImageDraw.Draw(base)
# The original turret obscures this top surface; no original pixels exist there.
d.polygon([(31,55),(41,49),(88,49),(97,56),(97,65),(76,76),(53,76),(31,65)],fill=im.getpixel((33,59)))
base.alpha_composite(visible)
canvas=Image.new('RGBA',(256,256));canvas.alpha_composite(base,(64,70));canvas.save(out/'original-base-native.png')
visible.save(out/'original-base-preserved-pixels.png');base.save(out/'original-base-cutout.png')
(out/'original-base.json').write_text(json.dumps({'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'offset':[64,70],'scale':1,'preserved_visible_pixels':sum(a>0 for a in visible.getchannel('A').getdata()),'hidden_top':'small occluded top polygon filled with adjacent source metal color; not claimed as original pixels','runtime_integrated':False},indent=2)+'\n')
