"""Preview: source-sampled GDI gold plus muzzle-palette roof emission.
No generated RGB texture, blur, resizing or runtime bloom. All original files stay intact.
"""
from ion_art_safety import active_assets
ACTIVE_BEFORE=active_assets()
from pathlib import Path
from collections import Counter
from PIL import Image,ImageDraw
import numpy as np,json,hashlib,colorsys
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-09-preview';OUT=BASE/'attempt-10-preview';OUT.mkdir(exist_ok=True);MASKS=BASE/'attempt-08-preview'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');original_palette={c[:3] for c in source.getdata() if c[3]==255};refs={};gold=set()
def luma(rgb):return sum(a*b for a,b in zip(rgb,(.2126,.7152,.0722)))
for name in ('conyard.png','weapons factory.png','barracks.png'):
 p=ROOT/'mods/ca/bits/gdi/buildings'/name;im=Image.open(p).convert('RGBA');counts=Counter(c[:3] for c in im.getdata() if c[3]==255);picked=[]
 for rgb,n in counts.items():
  h,s,v=colorsys.rgb_to_hsv(*(c/255 for c in rgb))
  if n>=100 and .08<h<.18 and s>.3:gold.add(rgb);picked.append(rgb)
 refs[name]={'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'sampled_colors':picked}
mapping={}
for rgb in sorted(original_palette):
 h,s,v=colorsys.rgb_to_hsv(*(c/255 for c in rgb))
 # Beige/tan armor and its brown shades, not red copper, steel, black or blue.
 if .045<=h<=.16 and s>=.15 and rgb[0]>=90 and rgb[0]-rgb[2]>18:
  mapping[rgb]=min(gold,key=lambda c:(abs(luma(c)-luma(rgb)),c))
labels=json.loads((MASKS/'surface-labels.json').read_text());core_ids=[int(k) for k,v in labels.items() if v['object']=='V7 straight cyan core'];near_ids=[int(k) for k,v in labels.items() if v['object'] in ('V7 straight channel recess','V7 straight left metal edge','V7 straight right metal edge')]
projection=json.loads((OUT/'conduit-projection.json').read_text());white=(173,251,251);cyan=(87,222,247);blue=(68,114,199);lit_metal=(135,229,240)
assert {white,cyan,blue,lit_metal}<=original_palette
allowed=original_palette|gold
stats=[]
def recolor(a):
 b=a.copy()
 for old,new in mapping.items():b[np.all(a[:,:,:3]==old,axis=2)&(a[:,:,3]>0),:3]=new
 return b
for size in (160,192):
 folder=OUT/str(size);folder.mkdir(exist_ok=True);base=Image.fromarray(recolor(np.array(Image.open(OLD/str(size)/'base.png').convert('RGBA'))));base.save(folder/'base.png')
 yy,xx=np.indices((size,size));pixels=np.stack((xx+.5,yy+.5),axis=-1)
 for idx in range(32):
  a=np.array(Image.open(OLD/str(size)/f'turret-{idx:03d}.png').convert('RGBA'));b=recolor(a);ids=np.array(Image.open(MASKS/str(size)/f'surface-id-{idx:03d}.png').convert('RGBA'));v=ids[:,:,:3].astype(int)//16;label=v[:,:,0]+16*v[:,:,1]+256*v[:,:,2];label[ids[:,:,3]<128]=0;core=np.isin(label,core_ids)
  q=np.array(projection[str(idx)])*size;du=q[1]-q[0];dv=q[2]-q[0];matrix=np.stack((du,dv),axis=1);coords=(pixels-q[0])@np.linalg.inv(matrix).T;u=coords[:,:,0];width=abs(np.linalg.det(matrix))/np.linalg.norm(dv)
  # A continuous world-space core, not a screen-horizontal gradient. Keep at
  # least one bright native pixel through narrow projected roof orientations.
  half=min(.46,max(.22,.55/width));bright=core&(abs(u-.5)<=half)
  b[core,:3]=cyan;b[bright,:3]=white
  adjacent=np.zeros_like(core)
  for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):adjacent|=np.roll(core,(dy,dx),(0,1))
  with Image.open(OLD/str(size)/f'contour-mask-{idx:03d}.png') as im:contour=np.array(im)>0
  spill=adjacent&np.isin(label,near_ids)&~contour&~np.all(a[:,:,:3]==(14,13,17),axis=2)
  recess_ids=[int(k) for k,val in labels.items() if val['object']=='V7 straight channel recess'];b[spill,:3]=lit_metal;b[spill&np.isin(label,recess_ids),:3]=blue
  assert np.array_equal(a[:,:,3],b[:,:,3]);assert np.array_equal(a[contour],b[contour]);assert {tuple(c) for c in b[:,:,:3][b[:,:,3]>0]}<=allowed
  Image.fromarray(((core|spill)*255).astype(np.uint8)).save(folder/f'glow-mask-{idx:03d}.png');im=Image.fromarray(b);im.save(folder/f'turret-{idx:03d}.png');Image.alpha_composite(base,im).save(folder/f'assembly-{idx:03d}.png')
  stats.append({'size':size,'frame':idx,'core_pixels':int(core.sum()),'bright_pixels':int(bright.sum()),'local_light_pixels':int(spill.sum()),'projected_width':float(width)})
review=Image.new('RGB',(1280,704),'#77736e');d=ImageDraw.Draw(review)
for row,study in enumerate((OLD,OUT)):
 for col,idx in enumerate((0,4,8,16)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA').resize((320,320),Image.Resampling.NEAREST);review.paste(im,(col*320,row*352+24),im);d.text((col*320+5,row*352+4),f'{"V9 / beige, flat cyan" if row==0 else "V10 / GDI gold, bright core"} / {idx*11.25:g} deg',fill='white')
review.save(OUT/'gdi-gold-glow-comparison-2x.png')
context=Image.new('RGB',(960,300),'#77736e');cd=ImageDraw.Draw(context)
for i,(path,title) in enumerate(((OUT/'192/assembly-000.png','V10 / native 192'),(ROOT/'mods/ca/bits/gdi/buildings/guard tower.png','Guard Tower / original'),(ROOT/'mods/ca/bits/gdi/buildings/conyard.png','GDI Conyard / source colors'))):
 im=Image.open(path).convert('RGBA');im=im.crop(im.getbbox());context.paste(im,(i*320+(320-im.width)//2,290-im.height),im);cd.text((i*320+5,5),title,fill='white')
context.save(OUT/'gdi-color-context.png')
views=[];sheet=Image.new('RGB',(1280,736),'#77736e')
for frame in range(32):
 idx=(20+frame)%32;v=Image.new('RGB',(320,184),'#77736e');d=ImageDraw.Draw(v)
 for col,study in enumerate((OLD,OUT)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA');v.paste(im,(col*160,20),im);d.text((col*160+4,4),'BEFORE' if col==0 else 'GDI GOLD / GLOW',fill='white')
 views.append(v.resize((640,368),Image.Resampling.NEAREST));sheet.paste(v.crop((160,0,320,184)),((frame%8)*160,(frame//8)*184))
sheet.save(OUT/'all-32-native160.png')
pal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+sorted(allowed);assert len(rgb)<=256;rgb+=[(0,0,0)]*(256-len(rgb));pal.putpalette([c for color in rgb for c in color]);gif=[v.quantize(palette=pal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-gold-glow-2x.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
assert active_assets()==ACTIVE_BEFORE, 'Preview builder changed installed art'
(OUT/'validation.json').write_text(json.dumps({'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'gdi_sources':refs,'color_mapping':[{'before':a,'after':b} for a,b in mapping.items()],'glow_palette':{'core':white,'cyan':cyan,'blue_border':blue,'lit_metal':lit_metal},'glow_method':'projected existing 3D core; bright center and one-pixel local channel/rail light, no blur or alpha expansion','native_sizes':[160,192],'facings':32,'installed':False,'active_game_assets_unchanged':True,'alpha_unchanged':True,'v9_contours_preserved':True,'frames':stats},indent=2)+'\n');print('GDI_GOLD_GLOW_PREVIEW_READY')
