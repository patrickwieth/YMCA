"""V4 native exports; Ion Defense installation is a separate preserved copy step."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-04';PASS=STUDY/'pass-05';OUT=STUDY/'export';OUT.mkdir(exist_ok=True)
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};palette=np.array(sorted(c[:3] for c in colors),dtype=np.int32);ink=palette[palette.sum(axis=1).argmin()]
def finish(path,outline=False):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[idx][inverse].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 if outline:
  edge=np.zeros_like(solid)
  for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
  a[edge,:3]=ink
 im=Image.fromarray(a);box=im.getbbox();assert box and min(box[:2])>0 and box[2]<im.width and box[3]<im.height
 assert {c for c in im.getdata() if c[3]}<=colors
 return im
base=finish(PASS/'base-160-raw.png');oldbase=Image.open(BASE/'attempt-03/export/heavy-blaster-base.png').convert('RGBA');assert base.tobytes()==oldbase.tobytes()
with Image.open(PASS/'base-rotation-control.png') as control,Image.open(PASS/'base-160-raw.png') as original:assert control.tobytes()==original.tobytes()
meta=PngInfo();meta.add_text('FrameSize','160,160');meta.add_text('FrameAmount','1');base.save(OUT/'heavy-blaster-base.png',pnginfo=meta)
sheet=Image.new('RGBA',(1280,640));views=[];frames=[]
for frame in range(32):
 index=(20+frame)%32;raw=PASS/f'turret-{index:03d}-raw.png';im=finish(raw,True);assert im.size==(160,160);im.save(OUT/f'turret-{frame:02d}.png');sheet.alpha_composite(im,((frame%8)*160,(frame//8)*160))
 a=Image.alpha_composite(base,im);a.save(OUT/f'assembly-{frame:02d}.png');view=Image.new('RGB',(160,184),'#77736e');view.paste(a,(0,20),a);ImageDraw.Draw(view).text((4,4),f'{frame:02d} / {frame*32}',fill='white');views.append(view)
 frames.append({'frame':frame,'render_index':index,'blender_degrees':index*11.25,'engine_facing':frame*32,'bounds':im.getbbox()})
meta=PngInfo();meta.add_text('FrameSize','160,160');meta.add_text('FrameAmount','32');sheet.save(OUT/'heavy-blaster-turret32.png',pnginfo=meta)
contact=Image.new('RGB',(1280,736),'#77736e')
for i,v in enumerate(views):contact.paste(v,((i%8)*160,(i//8)*184))
contact.save(OUT/'rotation-contact-sheet.png')
gifpal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+[c[:3] for c in sorted(colors)];rgb+=[(0,0,0)]*(256-len(rgb));gifpal.putpalette([v for c in rgb for v in c])
gif=[v.resize((480,552),Image.Resampling.NEAREST).quantize(palette=gifpal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-preview.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
# Enlarged problem views, not just the favorable source-facing pose.
angles=[0,90,135,180,225,270];review=Image.new('RGB',(6*320,3*348),'#77736e');d=ImageDraw.Draw(review)
for col,angle in enumerate(angles):
 idx=int(angle/11.25);engine=(idx-20)%32
 for row in range(3):
  if row==0:im=Image.open(BASE/f'attempt-03/export/assembly-{engine:02d}.png')
  else:im=Image.alpha_composite(base,finish(PASS/f'turret-{idx:03d}-raw.png',row==2))
  im=im.resize((320,320),Image.Resampling.NEAREST);review.paste(im,(col*320,row*348+24),im);d.text((col*320+5,row*348+5),f'{["V3", "V4 geometry/material", "V4 native edge finish"][row]} / {angle}',fill='white')
review.save(OUT/'multiview-comparison-2x.png')
reference=Image.new('RGBA',(320,320));reference.alpha_composite(source.resize((256,256),Image.Resampling.NEAREST),(32,32))
main=Image.new('RGB',(960,352),'#77736e');d=ImageDraw.Draw(main)
for i,(im,label) in enumerate(((reference,'REFERENCE'),(finish(BASE/'attempt-03/pass-05/assembly-320-raw.png'),'V3'),(finish(PASS/'assembly-320-raw.png'),'V4 mechanical correction / study'))):
 main.paste(im,(i*320,24),im);d.text((i*320+4,4),label,fill='white')
main.resize((1920,704),Image.Resampling.NEAREST).save(OUT/'main-comparison-2x.png')
parts=Image.new('RGB',(480,184),'#77736e');d=ImageDraw.Draw(parts)
for i,(im,label) in enumerate(((base,'SAME FIXED BASE'),(Image.open(OUT/'turret-12.png'),'SEPARATE TURRET'),(Image.open(OUT/'assembly-12.png'),'COMPOSITE'))):
 parts.paste(im,(i*160,20),im);d.text((i*160+4,4),label,fill='white')
parts.resize((1440,552),Image.Resampling.NEAREST).save(OUT/'separate-parts-3x.png')
validation={'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'native_frame':[160,160],'facings':32,'sprite_resampled':False,'source_palette_errors':0,'clipped_frames':0,'binary_alpha':True,'base_identical_to_v3':True,'base_rotation_control_identical':True,'texture_lod':'full authoritative source retained; 128px source-LOD experiments rejected','finish':'1 native pixel source-ink exterior contour; no blur or random noise','game_binding':'Ion_Defense / ion_defense.native; see ../integration/binding.json; install with integrate_ion_defense_v4.py','frames':frames}
(OUT/'validation.json').write_text(json.dumps(validation,indent=2)+'\n')
(OUT/'attachment.json').write_bytes((BASE/'attempt-03/export/attachment.json').read_bytes())
print('V4 native palette/alpha/frame/base separation checks passed.')
