"""Package straight-roof previews only; no game assets or rules are modified."""
from ion_art_safety import active_assets
ACTIVE_BEFORE=active_assets()
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-07-preview';PREV=BASE/'attempt-06-preview'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};pal=np.array(sorted(c[:3] for c in colors),dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()]
def finish(path):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128;u,inv=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);a[:,:,:3]=pal[idx][inv].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 edge=np.zeros_like(solid)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 a[edge,:3]=ink;im=Image.fromarray(a);assert {c for c in im.getdata() if c[3]}<=colors;box=im.getbbox();assert min(box[:2])>0 and max(box[2:])<im.width;return im
for size in (160,192):
 folder=OUT/str(size);base=Image.open(PREV/str(size)/'base.png').convert('RGBA');(folder/'base.png').write_bytes((PREV/str(size)/'base.png').read_bytes())
 for idx in range(32):
  im=finish(folder/f'turret-{idx:03d}-raw.png');assert im.size==(size,size);im.save(folder/f'turret-{idx:03d}.png');Image.alpha_composite(base,im).save(folder/f'assembly-{idx:03d}.png')
review=Image.new('RGB',(1280,704),'#77736e');d=ImageDraw.Draw(review)
for row,study in enumerate((PREV,OUT)):
 for col,idx in enumerate((0,4,8,16)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA').resize((320,320),Image.Resampling.NEAREST);review.paste(im,(col*320,row*352+24),im);d.text((col*320+5,row*352+4),f'{"BEFORE / projected wave" if row==0 else "AFTER / straight conduit"} / {idx*11.25:g} deg',fill='white')
review.save(OUT/'straight-roof-comparison.png')
views=[];contact=Image.new('RGB',(1280,736),'#77736e')
for frame in range(32):
 idx=(20+frame)%32;v=Image.new('RGB',(320,184),'#77736e');d=ImageDraw.Draw(v)
 for col,study in enumerate((PREV,OUT)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA');v.paste(im,(col*160,20),im);d.text((col*160+4,4),'BEFORE' if col==0 else 'STRAIGHT',fill='white')
 views.append(v.resize((640,368),Image.Resampling.NEAREST));contact.paste(v.crop((160,0,320,184)),((frame%8)*160,(frame//8)*184))
contact.save(OUT/'all-32-native160.png')
gifpal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+[c[:3] for c in sorted(colors)];rgb+=[(0,0,0)]*(256-len(rgb));gifpal.putpalette([c for color in rgb for c in color]);gif=[v.quantize(palette=gifpal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-before-after-2x.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
assert active_assets()==ACTIVE_BEFORE, 'Preview builder changed installed art'
(OUT/'validation.json').write_text(json.dumps({'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'native_sizes':[160,192],'facings_per_size':32,'source_palette_errors':0,'clipping_errors':0,'base_pixel_identical_to_v6':True,'active_game_assets_unchanged':True,'installed':False},indent=2)+'\n')
print('V7_ROOF_PREVIEW_READY')
