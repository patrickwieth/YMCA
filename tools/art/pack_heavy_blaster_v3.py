"""Finish and review V3, preserving separate stationary base and 32 turret frames.
All native frames come from 160px renders. Only review images are enlarged.
"""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-03';PASS=STUDY/'pass-05';OUT=STUDY/'export';OUT.mkdir(exist_ok=True)
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};palette=np.array(sorted(c[:3] for c in colors),dtype=np.int32)
def finish(path):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
 index=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[index][inverse].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 im=Image.fromarray(a);assert {c for c in im.getdata() if c[3]}<=colors
 box=im.getbbox();assert box and min(box[:2])>0 and box[2]<im.width and box[3]<im.height,(path,box)
 return im
base=finish(PASS/'base-160-raw.png');meta=PngInfo();meta.add_text('FrameSize','160,160');meta.add_text('FrameAmount','1');base.save(OUT/'heavy-blaster-base.png',pnginfo=meta)
sheet=Image.new('RGBA',(1280,640));views=[];report=[]
for frame in range(32):
 index=(20+frame)%32 # Engine N=0, W=256, S=512, E=768; Blender north=225deg.
 turret=finish(PASS/f'rotation-{index:03d}-raw.png');assert turret.size==(160,160)
 turret.save(OUT/f'turret-{frame:02d}.png');sheet.alpha_composite(turret,((frame%8)*160,(frame//8)*160))
 assembly=Image.alpha_composite(base,turret);assembly.save(OUT/f'assembly-{frame:02d}.png')
 view=Image.new('RGB',(160,184),'#77736e');view.paste(assembly,(0,20),assembly);ImageDraw.Draw(view).text((4,4),f'{frame:02d} / facing {frame*32}',fill='white');views.append(view)
 # Verify physical direction, not image labels. Forward=-Y before root yaw.
 a=math.radians(index*11.25);x,y=math.sin(a),-math.cos(a);sx=(x+y)/math.sqrt(2);sy=(x-y)/math.sqrt(8)
 bearing=math.degrees(math.atan2(-sx,-2*sy))%360;error=abs((bearing-frame*11.25+180)%360-180);assert error<1e-6
 report.append({'frame':frame,'engine_facing':frame*32,'blender_render_index':index,'blender_degrees':index*11.25,'bounds':turret.getbbox(),'direction_error_degrees':error})
meta=PngInfo();meta.add_text('FrameSize','160,160');meta.add_text('FrameAmount','32');sheet.save(OUT/'heavy-blaster-turret32.png',pnginfo=meta)
# Keep GIF palette fixed between frames. Display enlargement, not sprite scaling.
gif_palette=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+[c[:3] for c in sorted(colors)];rgb+=( [(0,0,0)]*(256-len(rgb)) );gif_palette.putpalette([v for c in rgb for v in c])
gif=[v.resize((480,552),Image.Resampling.NEAREST).quantize(palette=gif_palette,dither=Image.Dither.NONE) for v in views]
gif[0].save(OUT/'rotation-preview.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
contact=Image.new('RGB',(1280,736),'#77736e')
for i,view in enumerate(views):contact.paste(view,((i%8)*160,(i//8)*184))
contact.save(OUT/'rotation-contact-sheet.png')
# Match reference and model at the same calibrated density. No registration warp.
reference=Image.new('RGBA',(320,320));reference.alpha_composite(source.resize((256,256),Image.Resampling.NEAREST),(32,32))
comparison=Image.new('RGB',(960,352),'#77736e');d=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate(((reference,'REFERENCE'),(Image.open(BASE/'attempt-02/front-320.png'),'REJECTED V2'),(finish(PASS/'assembly-320-raw.png'),'REBUILT V3 / pass 5'))):
 comparison.paste(im,(i*320,24),im);d.text((i*320+6,6),label,fill='white')
comparison.resize((1920,704),Image.Resampling.NEAREST).save(OUT/'comparison-2x.png')
parts=Image.new('RGB',(480,184),'#77736e');d=ImageDraw.Draw(parts)
for i,(im,label) in enumerate(((base,'STATIONARY BASE'),(Image.open(OUT/'turret-12.png'),'TURRET / SW'),(Image.open(OUT/'assembly-12.png'),'COMPOSITE / SW'))):
 parts.paste(im,(i*160,20),im);d.text((i*160+4,4),label,fill='white')
parts.resize((1440,552),Image.Resampling.NEAREST).save(OUT/'separate-parts-3x.png')
# Document every reviewed pass, not just the final render.
iterations=Image.new('RGB',(1600,344),'#77736e');d=ImageDraw.Draw(iterations)
for i in range(1,6):
 im=finish(STUDY/f'pass-{i:02d}/assembly-320-raw.png');iterations.paste(im,((i-1)*320,20),im);d.text(((i-1)*320+5,3),f'V3 pass {i}',fill='white')
iterations.save(STUDY/'iteration-contact-sheet.png')
geometry=json.loads((PASS/'geometry-provenance.json').read_text());separate=np.array(Image.open(OUT/'assembly-12.png'));combined=np.array(finish(PASS/'assembly-160-raw.png'))
validation={'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'source_unchanged':True,'native_resolution':[160,160],'runtime_png_scale':1,'sprite_resampling':False,'source_palette_errors':0,'binary_alpha':True,'clipped_frames':0,'separate_assets':{'base':'heavy-blaster-base.png','turret':'heavy-blaster-turret32.png'},'pivot_in_shared_native_canvas':geometry['pivot_native'],'base_frames':1,'turret_frames':32,'frame_order':'counterclockwise engine N/W/S/E','two_layer_vs_3d_assembly_different_pixels':int(np.any(separate!=combined,axis=2).sum()),'game_binding':'NOT installed; standalone art export for review','known_limitations':['camera-registered source UVs make front comparison close but do not prove unseen geometry','rear/hidden details are inferred or reuse source panels','two-layer PNG composition intentionally draws turret above fixed base'],'frames':report}
assert validation['source_sha256']==geometry['source_sha256']
(OUT/'validation.json').write_text(json.dumps(validation,indent=2)+'\n')
(OUT/'attachment.json').write_text(json.dumps({'canvas':[160,160],'base_and_turret_canvas_origin_identical':True,'pivot_pixel':geometry['pivot_native'],'pivot_world':geometry['pivot_world'],'sprite_scale':1,'base_rotation':'none','turret_frames':32,'reference_SW_frame':12,'game_offsets':'not yet calibrated to actor/bib; do not infer from canvas center'},indent=2)+'\n')
print('HEAVY_BLASTER_V3_EXPORT',json.dumps({k:v for k,v in validation.items() if k!='frames'},indent=2))
