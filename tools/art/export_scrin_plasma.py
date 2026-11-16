"""Pack reviewed plasma pass05 without modifying the supplied image or earlier studies."""
from pathlib import Path
from PIL import Image,ImageDraw,PngImagePlugin
import json,hashlib,math,numpy as np
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin plasma';P=ART/'pass-05';OUT=ART/'integration-pass05';OUT.mkdir(exist_ok=True);RUNTIME=ROOT/'mods/ca/bits/scrin/buildings';g=json.loads((P/'geometry.json').read_text());assert g['rotation_clearance']>.1
assert hashlib.sha256((ART/'photon_cannon-1201896784-0.png').read_bytes()).hexdigest()==g['source_sha256']
assert Image.open(P/'base-raw.png').tobytes()==Image.open(P/'base-control-raw.png').tobytes()
base=Image.open(P/'review/base.png').convert('RGBA');atlas=Image.new('RGBA',(2560,1280));order=[(4+i)%32 for i in range(32)]
for i,n in enumerate(order):
 im=Image.open(P/f'review/turret-{n:02d}.png').convert('RGBA');bbox=im.getbbox();assert min(bbox[:2])>0 and max(bbox[2:])<320;atlas.alpha_composite(im,(i%8*320,i//8*320))
for im,name,count in [(base,'plasma-cannon-base.png',1),(atlas,'plasma-cannon-turret32.png',32)]:
 info=PngImagePlugin.PngInfo();info.add_text('FrameSize','320,320');info.add_text('FrameAmount',str(count));im.save(RUNTIME/name,pnginfo=info)
scale=.58;mount=round(2.14*28*scale*math.sqrt(3)/2*1448/64);muzzles=[[-.12,1.9725,4.42],[.12,1.9725,4.42]];local=[[round(y*28*scale*1448/128),round(x*28*scale*1448/128),round((z-2.14)*28*scale*math.sqrt(3)/2*1448/64)] for x,y,z in muzzles]
height=0;depth=0;half=0
for im,offset,m in [(base,-72,0)]+[(Image.open(P/f'review/turret-{n:02d}.png'),-20,mount) for n in range(32)]:
 arr=np.array(im);y,x=np.where(arr[:,:,3]>0);x=(x+.5-160)*scale;y=(y+.5-160+offset)*scale-m*64/1448;half=max(half,float(abs(x).max()));depth=max(depth,float((y+.5*abs(x)).max()));height=max(height,float((.5*abs(x)-y).max()))
bounds=math.ceil(max(half*2/128,depth*2/64)*1448/16)*16+32;select_height=math.ceil(height-bounds*64/1448/2)+4
b={'actor':'PTUR','image':'ptur.native','pass':5,'source_sha256':g['source_sha256'],'frame_size':320,'scale':scale,'body_offset':[0,-72],'turret_offset':[0,-20],'mount':[0,0,mount],'muzzles_model':muzzles,'local_offsets':local,'frame_order':order,'decoration_bounds':bounds,'selection_height':select_height,'assets':{name:hashlib.sha256((RUNTIME/name).read_bytes()).hexdigest() for name in ['plasma-cannon-base.png','plasma-cannon-turret32.png']},'live_review':False}
(OUT/'binding.json').write_text(json.dumps(b,indent=2)+'\n')
board=Image.new('RGBA',(224*8,224*4),(92,99,81,255));frames=[]
for i,n in enumerate(order):
 frame=Image.new('RGBA',(224,224),(92,99,81,255));draw=ImageDraw.Draw(frame);draw.line([(112,148),(176,180),(112,212),(48,180),(112,148)],fill=(160,170,120))
 for im,off,dy in [(base,-72,0),(Image.open(P/f'review/turret-{n:02d}.png'),-20,-mount*64/1448)]:
  frame.alpha_composite(im.resize((186,186),Image.Resampling.NEAREST),(round(112-160*scale),round(180-160*scale+off*scale+dy)))
 ImageDraw.Draw(frame).text((4,4),f'{i*11.25:g} deg',fill='white');board.alpha_composite(frame,(i%8*224,i//8*224));frames.append(frame.convert('RGB'))
board.save(OUT/'all32-native-1x1.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
print(json.dumps(b,indent=2))
