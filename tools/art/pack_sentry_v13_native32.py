"""Pack native 120px Blender renders. No turret downsampling or game scaling.
Frame filenames use render indices (index * 11.25 degrees), not rounded degrees.
"""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import json,math,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[2];STUDY=ROOT/'art-source/soviet-sentry/game-v13-32-native'
OUT=ROOT/'mods/ca/bits/soviet/buildings/experimental';OUT.mkdir(parents=True,exist_ok=True)
base_path=ROOT/'mods/ca/bits/soviet/buildings/sentry base.png'
original=base_path.read_bytes();base=Image.open(base_path).convert('RGBA')
# The base is supplied pixel art, not a modeled mesh. Produce a separate nearest-
# sampled derivative once; never resize it at runtime or overwrite the source.
base=base.resize((74,43),Image.Resampling.NEAREST);base.save(OUT/'sentry-base-native.png')
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors={c for c in source.getdata() if c[3]}
attachment=json.loads((STUDY/'attachment.json').read_text());px,py=attachment['bearing_pivot_screen']
assert attachment['frame_size']==[120,120] and attachment['native_render_scale']==.5
# Outermost ground contacts (1,24) and (72,24), measured at pixel centers,
# are symmetric around (37,24.5). Center that point on the bib origin.
base_offset=(0,-3);socket=(0-74/2+36.5,-3-43/2+8)
world_y=-112*64/1448
offset=(round(socket[0]-(px-60)),round(socket[1]-world_y-(py-60)))
assert offset==(-4,-24),offset
vectors={e['angle']:e for e in json.loads((STUDY/'projected-directions.json').read_text())}
sheet=Image.new('RGBA',(960,480));preview=Image.new('RGB',(8*160,4*180),'#77736e');d=ImageDraw.Draw(preview)
frames=[];required_height=0
for i in range(32):
 index=(20+i)%32 # engine N=0, W=256, S=512, E=768; Blender north=225 degrees
 im=Image.open(STUDY/f'final-turret-{index:03d}.png').convert('RGBA')
 assert im.size==(120,120)
 assert {c for c in im.getdata() if c[3]}<=colors
 assert set(im.getchannel('A').getdata())<={0,255}
 box=im.getbbox();assert min(box[:2])>0 and max(box[2:])<120
 x,y=vectors[index]['gun_screen_vector'];bearing=math.degrees(math.atan2(-x,-2*y))%360
 error=abs((bearing-i*11.25+180)%360-180);assert error<.01
 sheet.alpha_composite(im,((i%8)*120,(i//8)*120))
 ys,xs=np.where(np.array(im)[:,:,3]>0);sx=xs+.5+offset[0]-60;sy=ys+.5+offset[1]-60+world_y
 assert np.all((sx>=-56)&(sx<=56)),i
 assert np.all(sy<=28-.5*np.abs(sx)),i
 required_height=max(required_height,float(np.max(-28+.5*np.abs(sx)-sy)))
 cx,cy=(i%8)*160+80,(i//8)*180+125
 bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA')
 preview.paste(bib,(cx-64,cy-32),bib)
 preview.paste(base,(round(cx-37),round(cy-3-21.5)),base)
 preview.paste(im,(round(cx+offset[0]-60),round(cy+world_y+offset[1]-60)),im)
 d.text(((i%8)*160+5,(i//8)*180+5),f'{i:02d} / facing {i*32}',fill='white')
 frames.append({'frame':i,'render_index':index,'engine_facing':i*32,'blender_degrees':index*11.25,'bounds':box,'bearing_error_degrees':error})
meta=PngInfo();meta.add_text('FrameSize','120,120');meta.add_text('FrameAmount','32')
sheet.save(OUT/'sentry-v13-native32.png',pnginfo=meta)
(OUT/'sentry-v13-native32.yaml').write_text('FrameSize: 120,120\nFrameAmount: 32\n')
preview.save(STUDY/'game-assembly-preview.png')
preview.resize((2560,1440),Image.Resampling.NEAREST).save(STUDY/'game-assembly-preview-2x.png')
# Animated reference: native-size compositing, enlarged by nearest sampling for inspection.
views=[]
for i in range(32):
 view=preview.crop(((i%8)*160,(i//8)*180,(i%8+1)*160,(i//8+1)*180))
 views.append(view.resize((480,540),Image.Resampling.NEAREST))
views[0].save(STUDY/'rotation-preview.gif',save_all=True,append_images=views[1:],duration=80,loop=0)
assert base_path.read_bytes()==original
binding={'source_model':'attempt-13/sentry-v13.blend','sheet':'bits/soviet/buildings/experimental/sentry-v13-native32.png',
 'base_sheet':'bits/soviet/buildings/experimental/sentry-base-native.png','base_size':[74,43],'base_ground_contacts':[[1,24],[72,24]],
 'frame_size':[120,120],'facings':32,'sequence_scale':1,'sequence_offset':offset,'base_sequence_offset':base_offset,
 'source_base_sha256':hashlib.sha256(original).hexdigest(),'bib_image':'sovietbib1x1','bearing_pivot_in_frame':[px,py],
 'unchanged_turret_world_offset':[0,0,112],'pixel_alignment_error':[offset[0]+px-60-socket[0],world_y+offset[1]+py-60-socket[1]],
 'selection_height':max(49,math.ceil(required_height)+1),'native_blender_render':True,'turret_resampled':False,'frames':frames}
(STUDY/'game-binding.json').write_text(json.dumps(binding,indent=2)+'\n')
print('NATIVE32 COMPLETE; offset',offset,'selection height',binding['selection_height'])
