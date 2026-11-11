"""Smaller previews only. No copy into mods and no rule changes."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-06-preview'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};pal=np.array(sorted(c[:3] for c in colors),dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()]
cut=json.loads((BASE/'attempt-05/export/binding.json').read_text())['stationary_source_cut_256'];mask=Image.new('L',source.size);ImageDraw.Draw(mask).polygon([(x*4,y*4) for x,y in cut],fill=255)
def finish(path):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128;u,inv=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);a[:,:,:3]=pal[idx][inv].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 if path.name.startswith('turret'):
  edge=np.zeros_like(solid)
  for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
  a[edge,:3]=ink
 im=Image.fromarray(a);assert {c for c in im.getdata() if c[3]}<=colors;box=im.getbbox();assert box and min(box[:2])>0 and max(box[2:])<im.width;return im
assemblies={};checks=[]
for size in (160,192):
 f=size/320;folder=OUT/str(size);transform=(4/f,0,-128,0,4/f,-128)
 ref=source.transform((size,size),Image.Transform.AFFINE,transform,Image.Resampling.NEAREST);m=mask.transform((size,size),Image.Transform.AFFINE,transform,Image.Resampling.NEAREST)
 base=Image.composite(ref,finish(folder/'base-modeled-raw.png'),m)
 ImageDraw.Draw(base).polygon([((x+32)*f,(y+32)*f) for x,y in ((30,159),(43,159),(43,166),(41,169),(30,169))],fill=(0,0,0,0));base.save(folder/'base.png')
 for p in sorted(folder.glob('turret-???-raw.png')):
  idx=int(p.name.split('-')[1]);im=finish(p);assert im.size==(size,size);im.save(folder/f'turret-{idx:03d}.png');a=Image.alpha_composite(base,im);a.save(folder/f'assembly-{idx:03d}.png');assemblies[size,idx]=a
 checks.append({'size':size,'frames':sum(1 for k in assemblies if k[0]==size),'native_render':True,'palette_errors':0,'binary_alpha':True})
# One shared ground baseline: no enlargement of the candidates or hidden resizing.
canvas=Image.new('RGB',(1280,380),'#77736e');d=ImageDraw.Draw(canvas);d.line((0,300,1280,300),fill='#99948d')
items=[(Image.open(BASE/'attempt-04/export/assembly-12.png'),'V4: old narrow / 160',160),(Image.open(BASE/'attempt-05/export/assembly-12.png'),'V5: too large / 320',320),(assemblies[160,0],'V6 A: broad / native 160',160),(assemblies[192,0],'V6 B: broad / native 192',192)]
for i,(im,label,size) in enumerate(items):
 x=i*320+160-round(174*size/320);y=300-round(208*size/320);canvas.paste(im,(x,y),im);d.text((i*320+7,7),label,fill='white')
canvas.save(OUT/'start-size-comparison.png')
# Reference and broad model at the same source-pixel density, nearest enlarged
# for inspecting detail. This image is NOT used as a game sprite.
ref=source.transform((160,160),Image.Transform.AFFINE,(8,0,-128,0,8,-128),Image.Resampling.NEAREST)
review=Image.new('RGB',(960,552),'#77736e');d=ImageDraw.Draw(review)
for i,(im,label) in enumerate(((ref,'Reference / matching density'),(assemblies[160,0],'V6 A / broader model'),(assemblies[160,16],'V6 A / inferred reverse'))):
 im=im.resize((320,320),Image.Resampling.NEAREST);review.paste(im,(i*320,24),im);d.text((i*320+5,5),label,fill='white')
for i,idx in enumerate((4,8,12,20,24,28)):
 im=assemblies[160,idx];review.paste(im,(i*160,380),im);d.text((i*160+4,358),f'{idx*11.25:g} deg / native 160',fill='white')
review.save(OUT/'reference-and-directions.png')
views=[];sheet=Image.new('RGB',(1280,736),'#77736e')
for frame in range(32):
 idx=(20+frame)%32;a=assemblies[160,idx];v=Image.new('RGB',(160,184),'#77736e');v.paste(a,(0,20),a);ImageDraw.Draw(v).text((4,4),f'{frame:02d} / {frame*32}',fill='white');views.append(v);sheet.paste(v,((frame%8)*160,(frame//8)*184))
sheet.save(OUT/'all-32-native160.png')
gifpal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+[c[:3] for c in sorted(colors)];rgb+=[(0,0,0)]*(256-len(rgb));gifpal.putpalette([c for color in rgb for c in color]);gif=[v.resize((320,368),Image.Resampling.NEAREST).quantize(palette=gifpal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-native160-2x.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
(OUT/'validation.json').write_text(json.dumps({'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'candidates':checks,'installed':False,'model':'V5 broad solids unchanged; this is a size review, not a new UV reconstruction','source_base':'source cuts resampled directly from original at candidate density; turret renders not resized'},indent=2)+'\n')
print('PREVIEW_READY',OUT/'start-size-comparison.png')
