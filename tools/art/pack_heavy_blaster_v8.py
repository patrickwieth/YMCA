"""One-native-pixel geometry contours, no texture-contrast tracing or resizing."""
from ion_art_safety import active_assets
ACTIVE_BEFORE=active_assets()
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-07-preview';OUT=BASE/'attempt-08-preview'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');palette={c[:3] for c in source.getdata() if c[3]==255};ink=tuple(json.loads((BASE/'attempt-01/source-samples.json').read_text())['112,150'][:3]);assert ink in palette
labels=json.loads((OUT/'surface-labels.json').read_text());protected={int(k) for k,v in labels.items() if v['protect_cyan'] or 'straight left metal edge' in v['object'] or 'straight right metal edge' in v['object'] or 'bearing rim' in v['object']}
def silhouette(solid):
 edge=np.zeros_like(solid)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 return edge
stats=[]
for size in (160,192):
 folder=OUT/str(size);base0=np.array(Image.open(OLD/str(size)/'base.png').convert('RGBA'));base=base0.copy();base_edge=silhouette(base[:,:,3]>0)&(base[:,:,:3].astype(int).sum(axis=2)>sum(ink));base[base_edge,:3]=ink;baseim=Image.fromarray(base);baseim.save(folder/'base.png')
 for idx in range(32):
  original=np.array(Image.open(OLD/str(size)/f'turret-{idx:03d}.png').convert('RGBA'));a=original.copy();ids=np.array(Image.open(folder/f'surface-id-{idx:03d}.png').convert('RGBA'));solid=a[:,:,3]>0
  code=ids[:,:,:3].astype(np.int32)//16;label=code[:,:,0]+16*code[:,:,1]+256*code[:,:,2];label[ids[:,:,3]<128]=0
  assert np.array_equal(solid,label>0),'Geometry mask and V7 alpha differ'
  assert set(np.unique(label))<={0,*map(int,labels)}
  protect=np.isin(label,list(protected));edge=silhouette(solid)
  # Assign exactly one side of each adjacent surface boundary. Do not thicken,
  # dilate or trace every painted highlight. Put cyan borders on adjacent metal.
  for dy,dx in ((0,1),(1,0)):
   other=np.roll(label,(-dy,-dx),(0,1));other_protect=np.roll(protect,(-dy,-dx),(0,1));boundary=solid&(other>0)&(label!=other)
   here=boundary&~protect;there=boundary&protect&~other_protect
   edge|=here;edge|=np.roll(there,(dy,dx),(0,1))
  edge&=solid&~protect&(a[:,:,:3].astype(int).sum(axis=2)>sum(ink));a[edge,:3]=ink
  assert np.array_equal(a[:,:,3],original[:,:,3]);assert np.array_equal(a[protect],original[protect]);assert {tuple(c) for c in a[:,:,:3][solid]}<=palette
  Image.fromarray((edge*255).astype(np.uint8)).save(folder/f'contour-mask-{idx:03d}.png');im=Image.fromarray(a);im.save(folder/f'turret-{idx:03d}.png');Image.alpha_composite(baseim,im).save(folder/f'assembly-{idx:03d}.png')
  stats.append({'size':size,'frame':idx,'contour_pixels':int(edge.sum()),'changed_pixels':int(np.any(a!=original,axis=2).sum()),'cyan_preserved':True})
review=Image.new('RGB',(1280,704),'#77736e');d=ImageDraw.Draw(review)
for row,study in enumerate((OLD,OUT)):
 for col,idx in enumerate((0,4,8,16)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA').resize((320,320),Image.Resampling.NEAREST);review.paste(im,(col*320,row*352+24),im);d.text((col*320+5,row*352+4),f'{"V7 / before" if row==0 else "V8 / contour pixels"} / {idx*11.25:g} deg',fill='white')
review.save(OUT/'contour-comparison-2x.png')
views=[];sheet=Image.new('RGB',(1280,736),'#77736e')
for frame in range(32):
 idx=(20+frame)%32;v=Image.new('RGB',(320,184),'#77736e');d=ImageDraw.Draw(v)
 for col,study in enumerate((OLD,OUT)):
  im=Image.open(study/'160'/f'assembly-{idx:03d}.png').convert('RGBA');v.paste(im,(col*160,20),im);d.text((col*160+4,4),'BEFORE' if col==0 else 'CONTOURS',fill='white')
 views.append(v.resize((640,368),Image.Resampling.NEAREST));sheet.paste(v.crop((160,0,320,184)),((frame%8)*160,(frame//8)*184))
sheet.save(OUT/'all-32-native160.png')
gifpal=Image.new('P',(1,1));rgb=[(119,115,110),(255,255,255)]+sorted(palette);rgb+=[(0,0,0)]*(256-len(rgb));gifpal.putpalette([c for color in rgb for c in color]);gif=[v.quantize(palette=gifpal,dither=Image.Dither.NONE) for v in views];gif[0].save(OUT/'rotation-contours-before-after-2x.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
assert active_assets()==ACTIVE_BEFORE, 'Preview builder changed installed art'
(OUT/'validation.json').write_text(json.dumps({'method':'visible geometry surface IDs, one-sided native pixel boundaries; source base outer contour only','ink_rgb':ink,'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'sizes':[160,192],'facings':32,'runtime_scale':1,'alpha_unchanged':True,'cyan_core_unchanged':True,'game_assets_unchanged':True,'installed':False,'frames':stats},indent=2)+'\n');print('V8_CONTOUR_PREVIEW_READY')
