"""Install Soviet SAM pass12 at native density; reorder directions without resampling."""
from pathlib import Path
import shutil,json
from PIL import Image
from PIL.PngImagePlugin import PngInfo
ROOT=Path(__file__).resolve().parents[2]
S=ROOT/'art-source/soviet sam site/pass-12/review'
D=ROOT/'mods/ca/bits/soviet/buildings'
shutil.copy2(S/'base.png',D/'sam-base.png')
sheet=Image.new('RGBA',(192*8,192*4))
order=[(20+i)%32 for i in range(32)]
for i,j in enumerate(order):sheet.paste(Image.open(S/f'turret-{j:03d}.png'),(i%8*192,i//8*192))
meta=PngInfo();meta.add_text('FrameSize','192,192');meta.add_text('FrameAmount','32')
sheet.save(D/'sam-turret32.png',pnginfo=meta)
meta=PngInfo();meta.add_text('FrameSize','192,192');meta.add_text('FrameAmount','1')
Image.open(S/'assembly-020.png').save(D/'sam-place.png',pnginfo=meta)
lines=['# Soviet SAM pass12; native 192px frames, ground (96,146), pivot height 42.782px.', 'sam.soviet.native:', '\tInherits: sam.soviet']
for name,file,count,offset in [(n,'base',1,'0,-50') for n in ('idle','damaged-idle','base','damaged-base','dead')]+[('place','place',1,'0,-50')]+[(n,'turret32',32,'0,-7') for n in ('turret','damaged-turret')]:
 lines += [f'\t{name}:',f'\t\tFilename: bits/soviet/buildings/sam-{file}.png','\t\tStart: 0','\t\tLength: 1',f'\t\tFacings: {count}','\t\tUseClassicFacings: false','\t\tScale: 0.75','\t\tAnchor: Center',f'\t\tOffset: {offset}']
lines += ['\tmuzzle-native:','\t\tFilename: gunfire2.shp','\t\tLength: 5','\t\tFacings: 1','\t\tScale: 1','\t\tAnchor: Center','\t\tOffset: 0,0']
(ROOT/'mods/ca/sequences/soviet-sam.yaml').write_text('\n'.join(lines)+'\n')
(ROOT/'art-source/soviet sam site/integration-pass12/registration.json').write_text(json.dumps({'source_pass':12,'source_frame_order':order,'ground_pixel':[96,146],'base_offset':[0,-50],'turret_offset':[0,-7],'turret_world_offset':[0,0,726],'muzzle_local_offset':[496,-291,953],'density':26,'runtime_scale':0.75,'resampled':False},indent=2)+'\n')
print('Soviet SAM pass12 native sprites installed.')
