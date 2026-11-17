"""Recenter the base bearing, retain approved V10 turret bytes, preview 2x2 bib."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib,shutil,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-10-preview';OUT=BASE/'attempt-11-centering';EXPORT=OUT/'export';EXPORT.mkdir(exist_ok=True);DEST=ROOT/'mods/ca/bits/gdi/buildings/experimental'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');palette=np.array(sorted({c[:3] for c in source.getdata() if c[3]==255}),dtype=np.int32)
def finish(path):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128;u,inv=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1);a[:,:,:3]=palette[idx][inv].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0;return Image.fromarray(a)
modeled=finish(OUT/'base-centered-raw.png');assert modeled.tobytes()==finish(OUT/'base-control-raw.png').tobytes()
cut=json.loads((BASE/'attempt-05/export/binding.json').read_text())['stationary_source_cut_256'];mask=Image.new('L',source.size);ImageDraw.Draw(mask).polygon([(x*4,y*4) for x,y in cut],fill=255);transform=(8,0,-128,0,8,-128);reference=source.transform((160,160),Image.Transform.AFFINE,transform,Image.Resampling.NEAREST);mask=mask.transform((160,160),Image.Transform.AFFINE,transform,Image.Resampling.NEAREST);base=Image.composite(reference,modeled,mask)
ImageDraw.Draw(base).polygon([((x+32)*.5,(y+32)*.5) for x,y in ((30,159),(43,159),(43,166),(41,169),(30,169))],fill=(0,0,0,0))
a=np.array(base);solid=a[:,:,3]>0;edge=np.zeros_like(solid)
for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
a[edge&(a[:,:,:3].astype(int).sum(axis=2)>44),:3]=(14,13,17);original=a.copy()
for change in json.loads((OLD/'validation.json').read_text())['color_mapping']:a[np.all(original[:,:,:3]==change['before'],axis=2)&solid,:3]=change['after']
base=Image.fromarray(a);base.save(OUT/'base-centered.png');meta=PngInfo();meta.add_text('FrameSize','160,160');meta.add_text('FrameAmount','1');base.save(EXPORT/'heavy-blaster-base.png',pnginfo=meta)
shutil.copyfile(OLD/'export/heavy-blaster-turret32.png',EXPORT/'heavy-blaster-turret32.png')
bib=Image.open(ROOT/'mods/ca/bits/gdi/buildings/bib2x2.png').convert('RGBA');oldbase=Image.open(OLD/'160/base.png').convert('RGBA')
def view(idx,new=True,mark=False):
 canvas=Image.new('RGBA',(320,260),(119,115,110,255));origin=(160,160)
 if new:canvas.alpha_composite(bib,(32,96))
 canvas.alpha_composite(base if new else oldbase,(73,56));turret=Image.open(OLD/'160'/f'turret-{idx:03d}.png').convert('RGBA')
 if not mark:canvas.alpha_composite(turret,(79,60) if new else (73,56))
 else:
  d=ImageDraw.Draw(canvas);x,y=(160.061,149.578) if new else (154.061,145.578);d.line((x-5,y,x+5,y),fill='lime' if new else 'red');d.line((x,y-5,x,y+5),fill='lime' if new else 'red')
 ImageDraw.Draw(canvas).text((5,5),'CENTERED + 2x2 BIB' if new else 'BEFORE / OFF-CENTER',fill='white');return canvas.convert('RGB')
comparison=Image.new('RGB',(640,520))
for row,mark in enumerate((True,False)):
 for col,new in enumerate((False,True)):comparison.paste(view(0,new,mark),(col*320,row*260))
comparison.save(OUT/'centering-and-bib.png');views=[];contact=Image.new('RGB',(1280,1040),'#77736e')
for frame in range(32):
 idx=(20+frame)%32;v=view(idx);views.append(v)
 if frame%2==0:contact.paste(v,((frame//2%4)*320,(frame//8)*260))
contact.save(OUT/'rotation-contact-sheet.png');views[0].save(OUT/'rotation-centered.gif',save_all=True,append_images=views[1:],duration=110,loop=0)
# Validate the shifted assembly within the unchanged visual selection envelope.
height=0
for i in range(32):
 t=np.array(Image.open(OLD/'160'/f'turret-{i:03d}.png').convert('RGBA'))
 for data,dx,dy in ((a,0,0),(t,6,4)):
  y,x=np.where(data[:,:,3]>0);sx=x+.5-87+dx;sy=y+.5-104+dy;assert np.all(abs(sx)<84) and np.all(sy<=42-.5*abs(sx));height=max(height,float((-42+.5*abs(sx)-sy).max()))
assets={}
for name in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):
 shutil.copyfile(EXPORT/name,DEST/name);assets[name]=hashlib.sha256((EXPORT/name).read_bytes()).hexdigest()
r={'actor':'ion_defense','native_size':160,'facings':32,'assets':assets,'turret_pixels':'byte-identical V10 export','base_offset':[-7,-24],'turret_sprite_offset':[-1,-10],'turret_world_offset':[0,0,226],'muzzle_local_offset':[1631,0,675],'native_turret_translation':[6,4],'bib':'gdibib2x2 / original 256x128 Scale 1','required_selection_height':height,'selection_height':66,'selection_bounds':[1901,1901,0,0],'base_stationary_control':True,'installed':True,'live_review':False};(OUT/'binding.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r,indent=2))
