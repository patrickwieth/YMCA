"""Pack reviewed native renders (default pass30); preserve sources and history."""
from pathlib import Path
from PIL import Image,PngImagePlugin,ImageDraw
import json,math,hashlib,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';STAGE=int(sys.argv[1]) if len(sys.argv)>1 else 30;STUDY=ART/f'pass-{STAGE:02d}';OUT=ART/f'integration-pass{STAGE}';OUT.mkdir(exist_ok=True)
RUNTIME=ROOT/'mods/ca/bits/scrin/buildings';RUNTIME.mkdir(exist_ok=True)
meta=json.loads((STUDY/'geometry.json').read_text())
if STAGE>=30:
 saved=json.loads((STUDY/'saved-scene-audit.json').read_text())
 assert saved['fixed_vertices_unchanged'] and saved['all_moving_parts_yaw_parented'] and saved['fixed_shards']==4
 assert saved['blend_sha256']==hashlib.sha256((STUDY/'shard-launcher.blend').read_bytes()).hexdigest()
 assert abs(saved['measured_bore_degrees']-25)<.03
else:assert meta.get('bearing_clearance',meta['rotation_clearance'])>.1
if STAGE>=18:
 audit=json.loads((STUDY/'swept-collision-audit.json').read_text());assert audit['samples']>=64 and not audit['collisions']
assert hashlib.sha256((ART/'shard_launcher-332414650-0.png').read_bytes()).hexdigest()==meta['source_sha256']
assert Image.open(STUDY/'base-raw.png').tobytes()==Image.open(STUDY/'base-control-raw.png').tobytes()
size=meta['frame_size'];base=Image.open(STUDY/'review/base.png').convert('RGBA');atlas=Image.new('RGBA',(size*8,size*4));order=[(4+i)%32 for i in range(32)]
for i,n in enumerate(order):
 frame=Image.open(STUDY/f'review/turret-{n:02d}.png').convert('RGBA');bbox=frame.getbbox();assert bbox and min(bbox[:2])>0 and max(bbox[2:])<size
 atlas.alpha_composite(frame,(i%8*size,i//8*size))
def save(image,name,count):
 info=PngImagePlugin.PngInfo();info.add_text('FrameSize',f'{size},{size}');info.add_text('FrameAmount',str(count));image.save(RUNTIME/name,pnginfo=info)
if STAGE>=18:
 for name in ('shard-launcher-base.png','shard-launcher-turret32.png'):
  backup=OUT/('previous-'+name)
  if not backup.exists():backup.write_bytes((RUNTIME/name).read_bytes())
 save(base,'shard-launcher-base.png',1)
elif STAGE>=15:
 assert base.crop((32,64,352,384)).tobytes()==Image.open(RUNTIME/'shard-launcher-base.png').convert('RGBA').tobytes(), 'Fixed base must remain unchanged'
else:save(base,'shard-launcher-base.png',1)
save(atlas,'shard-launcher-turret32.png',32)
scale=.58;mount=round(28*scale*meta['pivot'][2]*math.sqrt(3)/2*1448/64);tip=meta['muzzle_model'];local=[round(tip[1]*28*scale*1448/128),round(tip[0]*28*scale*1448/128),round((tip[2]-meta['pivot'][2])*28*scale*math.sqrt(3)/2*1448/64)]
report={'actor':'SHAR','image':'shar.native','pass':STAGE,'source_sha256':meta['source_sha256'],'frame_size':size,'scale':scale,'body_offset':[0,-88 if STAGE>=18 else -56],'turret_offset':[0,-36 if STAGE>=15 else -4],'mount':[0,0,mount],'muzzle_local_offset':local,'native_density':28,'native_ground_origin':meta['ground_origin'],'frame_order':order,'bearing_clearance':meta.get('bearing_clearance',meta['rotation_clearance']),'stationary_battery':STAGE>=18,'elevation_degrees':meta.get('elevation_degrees'),'originals_modified':False,'live_review':False,'assets':{name:hashlib.sha256((RUNTIME/name).read_bytes()).hexdigest() for name in ['shard-launcher-base.png','shard-launcher-turret32.png']}}
if STAGE>=30:
 # Cancel the actual integer engine mount exactly; retain the native common ground origin.
 report['turret_offset']=[0,round(report['body_offset'][1]+mount*64/1448/scale,6)]
 report['clearance_method']='saved-scene radial sweep, not whole-part vertical gap'
 import numpy as np
 height=0;half=1920*128/1448/2;bottom=1920*64/1448/2
 for image in [base]+[Image.open(STUDY/f'review/turret-{n:02d}.png').convert('RGBA') for n in order]:
  yy,xx=np.where(np.array(image)[:,:,3]>0);x=(xx+.5-size/2)*scale;y=(yy+.5-meta['ground_origin'][1])*scale
  assert max(abs(x))<=half and max(y+.5*abs(x))<=bottom+.5
  height=max(height,float(max(-y-bottom+.5*abs(x))))
 report['selection_height']=math.ceil(height)+1;report['decoration_bounds']=[1920,1920]
(OUT/'binding.json').write_text(json.dumps(report,indent=2)+'\n')
# Diagnostic native-size assembly on a128x64 diamond, not a live screenshot.
board=Image.new('RGBA',(224*8,224*4),(92,99,81,255));frames=[]
for i in range(32):
 origin=(112,180);frame=Image.new('RGBA',(224,224),(92,99,81,255));draw=ImageDraw.Draw(frame);draw.line([(112,148),(176,180),(112,212),(48,180),(112,148)],fill=(160,170,120,255))
 for image,off,dy in [(base, -88 if STAGE>=15 else -56,0),(atlas.crop((i%8*size,i//8*size,(i%8+1)*size,(i//8+1)*size)),report['turret_offset'][1],-mount*64/1448)]:
  resized=image.resize((round(size*scale),round(size*scale)),Image.Resampling.NEAREST);frame.alpha_composite(resized,(round(origin[0]-size*scale/2),round(origin[1]-size*scale/2+off*scale+dy)))
 draw=ImageDraw.Draw(frame);draw.text((4,4),f'{i*11.25:g} deg',fill='white');board.alpha_composite(frame,(i%8*224,i//8*224));frames.append(frame.convert('RGB'))
board.save(OUT/'all32-native-1x1.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
print(json.dumps(report,indent=2))
