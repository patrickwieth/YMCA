"""Pack reviewed native twin Gatling; direct1x1 Soviet bib remains a separate fixed overlay."""
from pathlib import Path
from PIL import Image,ImageDraw,PngImagePlugin
import numpy as np,json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';P=ART/'pass-03';OUT=ART/'integration';OUT.mkdir(exist_ok=True);RUNTIME=ROOT/'mods/ca/bits/china/buildings';RUNTIME.mkdir(exist_ok=True);g=json.loads((P/'geometry.json').read_text())
if (OUT/'binding.json').exists() and json.loads((OUT/'binding.json').read_text()).get('platform_source'):
 raise SystemExit('Historical pass03 exporter must not overwrite the supplied platform. Use install_chinese_gatling_platform.py for the current base.')
assert g['moving_min_z']-g['fixed_max_z']>.1
for name,sha in g['source_hashes'].items():assert hashlib.sha256((ART/name).read_bytes()).hexdigest()==sha
assert Image.open(P/'base-raw.png').tobytes()==Image.open(P/'base-control-raw.png').tobytes()
base=Image.open(P/'review/base.png').convert('RGBA');atlas=Image.new('RGBA',(2560,1280));order=[(4+i)%32 for i in range(32)]
for i,n in enumerate(order):
 im=Image.open(P/f'review/turret-{n:02d}.png').convert('RGBA');bbox=im.getbbox();assert min(bbox[:2])>0 and max(bbox[2:])<320;atlas.alpha_composite(im,(i%8*320,i//8*320))
for im,name,count in [(base,'gatling-base.png',1),(atlas,'gatling-turret32.png',32)]:
 info=PngImagePlugin.PngInfo();info.add_text('FrameSize','320,320');info.add_text('FrameAmount',str(count));im.save(RUNTIME/name,pnginfo=info)
scale=.65;body_offset=-75;turret_offset=-34;mount=round((g['pivot'][2]*28*math.sqrt(3)/2+11)*scale*1448/64)
local=[[round(y*28*scale*1448/128),round(x*28*scale*1448/128),round((z-g['pivot'][2])*28*scale*math.sqrt(3)/2*1448/64)] for x,y,z in g['muzzles_model']]
height=half=depth=0
for im,offset,m in [(base,body_offset,0)]+[(Image.open(P/f'review/turret-{n:02d}.png'),turret_offset,mount) for n in range(32)]:
 a=np.array(im);y,x=np.where(a[:,:,3]>0);x=(x+.5-160)*scale;y=(y+.5-160+offset)*scale-m*64/1448;half=max(half,float(abs(x).max()));depth=max(depth,float((y+.5*abs(x)).max()));height=max(height,float((.5*abs(x)-y).max()))
bounds=max(1448,math.ceil(max(half*2/128,depth*2/64)*1448/16)*16+32);select_height=math.ceil(height-bounds*64/1448/2)+4
b={'actor':'chgatling','image':'chgatling.native','pass':3,'source_hashes':g['source_hashes'],'frame_size':320,'scale':scale,'body_offset':[0,body_offset],'turret_offset':[0,turret_offset],'mount':[0,0,mount],'muzzles_model':g['muzzles_model'],'local_offsets':local,'frame_order':order,'decoration_bounds':bounds,'selection_height':select_height,'bib':'sovietbib1x1','assets':{n:hashlib.sha256((RUNTIME/n).read_bytes()).hexdigest() for n in ['gatling-base.png','gatling-turret32.png']},'live_review':False,'animated_barrel_spin':False}
(OUT/'binding.json').write_text(json.dumps(b,indent=2)+'\n')
bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA');board=Image.new('RGBA',(224*8,224*4),(90,97,80,255));frames=[]
for i,n in enumerate(order):
 frame=Image.new('RGBA',(224,224),(90,97,80,255));frame.alpha_composite(bib,(48,143))
 for im,off,dy in [(base,body_offset,0),(Image.open(P/f'review/turret-{n:02d}.png'),turret_offset,-mount*64/1448)]:
  frame.alpha_composite(im.resize((208,208),Image.Resampling.NEAREST),(round(112-160*scale),round(175-160*scale+off*scale+dy)))
 ImageDraw.Draw(frame).text((4,4),f'{i*11.25:g} deg',fill='white');board.alpha_composite(frame,(i%8*224,i//8*224));frames.append(frame.convert('RGB'))
board.save(OUT/'all32-native-1x1.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
print(json.dumps({k:v for k,v in b.items() if k!='source_hashes'},indent=2))
