"""V5 native 320px export, with original visible base silhouette/detail restored.
Only source cuts are nearest-reduced from supplied enlarged artwork. Turret
frames are actual 320px Blender renders, never enlarged 160px sprites.
"""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-05';OUT=STUDY/'export';OUT.mkdir(exist_ok=True)
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};palette=np.array(sorted(c[:3] for c in colors),dtype=np.int32);ink=palette[palette.sum(axis=1).argmin()]
def finish(path,outline=False):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[idx][inverse].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 if outline:
  edge=np.zeros_like(solid)
  for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
  a[edge,:3]=ink
 im=Image.fromarray(a);box=im.getbbox();assert box and min(box[:2])>0 and max(box[2:])<320
 assert {c for c in im.getdata() if c[3]}<=colors
 return im
modeled=finish(STUDY/'base-modeled-raw.png');control=finish(STUDY/'base-control-raw.png');assert modeled.tobytes()==control.tobytes()
# Upper cut boundary follows the outside of the stationary socket, BELOW the
# rotating arms/capacitors. Preserve all original pixels below it, including
# alpha: this restores the small armor protrusions and genuinely shaped corners
# instead of keeping the chopped, straight edges of the earlier plinth mesh.
cut=[(0,170),(41,170),(56,160),(70,159),(82,164),(98,172),(117,176),(138,176),(158,172),(176,163),(186,151),(188,137),(186,127),(195,126),(256,142),(256,256),(0,256)]
mask=Image.new('L',source.size);ImageDraw.Draw(mask).polygon([(x*4,y*4) for x,y in cut],fill=255)
mask=mask.resize((256,256),Image.Resampling.NEAREST);reference=Image.new('RGBA',(320,320));reference.alpha_composite(source.resize((256,256),Image.Resampling.NEAREST),(32,32));padded=Image.new('L',(320,320));padded.paste(mask,(32,32))
base=Image.composite(reference,modeled,padded)
# Explicitly exclude the original muzzle's tiny overhanging lip, not base armor.
ImageDraw.Draw(base).polygon([(x+32,y+32) for x,y in ((30,159),(43,159),(43,166),(41,169),(30,169))],fill=(0,0,0,0))
assert {c for c in base.getdata() if c[3]}<=colors
meta=PngInfo();meta.add_text('FrameSize','320,320');meta.add_text('FrameAmount','1');base.save(OUT/'heavy-blaster-base.png',pnginfo=meta);padded.save(STUDY/'stationary-source-cut-mask.png')
sheet=Image.new('RGBA',(2560,1280));views=[];frames=[];height=0
for i in range(32):
 index=(20+i)%32;im=finish(STUDY/f'turret-{index:03d}-raw.png',True);assert im.size==(320,320);im.save(OUT/f'turret-{i:02d}.png');sheet.alpha_composite(im,((i%8)*320,(i//8)*320))
 assembled=Image.alpha_composite(base,im);assembled.save(OUT/f'assembly-{i:02d}.png');view=Image.new('RGB',(320,344),'#77736e');view.paste(assembled,(0,20),assembled);ImageDraw.Draw(view).text((5,4),f'{i:02d} / facing {i*32}',fill='white');views.append(view)
 a=np.array(assembled);yy,xx=np.where(a[:,:,3]>0);sx=xx+.5-174;sy=yy+.5-208
 assert np.all(abs(sx)<168) and np.all(sy<=84-.5*abs(sx)),i
 height=max(height,float((-84+.5*abs(sx)-sy).max()))
 frames.append({'frame':i,'render_index':index,'engine_facing':i*32,'bounds':im.getbbox()})
meta=PngInfo();meta.add_text('FrameSize','320,320');meta.add_text('FrameAmount','32');sheet.save(OUT/'heavy-blaster-turret32.png',pnginfo=meta)
contact=Image.new('RGB',(2560,1376),'#77736e')
for i,v in enumerate(views):contact.paste(v,((i%8)*320,(i//8)*344))
contact.save(OUT/'rotation-contact-sheet.png')
gifpal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+[c[:3] for c in sorted(colors)];rgb+=[(0,0,0)]*(256-len(rgb));gifpal.putpalette([v for c in rgb for v in c]);gif=[v.quantize(palette=gifpal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-preview.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
# Actual native-size comparison. Do not enlarge V4 to conceal the size change.
review=Image.new('RGB',(1920,700),'#77736e');d=ImageDraw.Draw(review)
for col,i in enumerate((12,16,20,24,28,0)):
 old=Image.open(BASE/f'attempt-04/export/assembly-{i:02d}.png').convert('RGBA');new=Image.open(OUT/f'assembly-{i:02d}.png').convert('RGBA')
 review.paste(old,(col*320+80,110),old);d.text((col*320+5,5),f'V4 actual native size / {i*32}',fill='white')
 review.paste(new,(col*320,370),new);d.text((col*320+5,350),f'V5 actual native size / {i*32}',fill='white')
review.save(OUT/'native-size-comparison.png')
main=Image.new('RGB',(960,350),'#77736e');d=ImageDraw.Draw(main)
for i,(im,label) in enumerate(((reference,'ORIGINAL / 256px source density'),(Image.open(BASE/'attempt-04/pass-05/assembly-320-raw.png'),'V4 / old narrow model at same density'),(Image.open(OUT/'assembly-12.png'),'V5 / broader model and restored base'))):
 main.paste(im,(i*320,24),im);d.text((i*320+5,5),label,fill='white')
main.resize((1920,700),Image.Resampling.NEAREST).save(OUT/'reference-comparison-2x.png')
parts=Image.new('RGB',(960,344),'#77736e');d=ImageDraw.Draw(parts)
for i,(im,label) in enumerate(((base,'FIXED BASE / original perimeter'),(Image.open(OUT/'turret-12.png'),'SEPARATE BROADER TURRET'),(Image.open(OUT/'assembly-12.png'),'COMPOSITE / native 320px'))):
 parts.paste(im,(i*320,20),im);d.text((i*320+5,4),label,fill='white')
parts.save(OUT/'separate-parts.png')
report={'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'frame_size':[320,320],'facings':32,'scale':1,'turret_resampled':False,'gun_cross_section_factor':1.65,'support_spacing_factor':1.25,'native_density':40,'previous_native_density':20,'base_method':'visible original lower plate/perimeter over stationary modeled hidden deck and socket','stationary_source_cut_256':cut,'source_palette_errors':0,'clipped_frames':0,'base_offset':[-14,-48],'turret_offset':[-2,-19],'turret_world_offset':[0,-272,656],'muzzle_local_offset':[3263,0,1350],'camera_pitch':85,'selection_bounds':[3801,3801,0,0],'selection_height':math.ceil(height)+1,'required_height':height,'frames':frames}
(OUT/'binding.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('frames','stationary_source_cut_256')},indent=2))
