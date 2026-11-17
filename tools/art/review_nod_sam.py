"""Native Nod SAM export and source/rotation review; original base pixels untouched."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,argparse
from contour_geometry import owned_boundaries
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--study-dir',type=Path);a=p.parse_args();ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/nod sam site';D=a.study_dir if a.study_dir else F/f'pass-{a.iteration:02d}';O=D/'review';O.mkdir(exist_ok=True)
source=Image.open(F/'sam_site-1179307279-0.png').convert('RGBA');base=Image.open(F/'source-study/base.png').convert('RGBA');pal=np.array(sorted({c[:3] for c in source.getdata()}),dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()];g=json.loads((D/'geometry.json').read_text());frames={};turrets={};bounds={}
for i in g['facings']:
 raw=np.array(Image.open(D/f'turret-{i:03d}-raw.png').convert('RGBA'));mask=raw[:,:,3]>=128
 u,inv=np.unique(raw[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);ids=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);raw[:,:,:3]=pal[ids][inv].reshape((256,256,3));raw[:,:,3]=mask*255;raw[~mask]=0
 labels=np.array(Image.open(D/f'turret-{i:03d}-ids.png')).astype(np.uint16);labels=labels[:,:,0]+256*labels[:,:,1];labels[~mask]=0
 edge=np.zeros_like(mask)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=mask&~np.roll(mask,(dy,dx),(0,1))
 if a.iteration>=3:
  counts=np.bincount(labels.ravel());protected=counts[labels]<10
  if a.iteration>=9:
   protected_ids=[v for k,v in g['groups'].items() if 'Cream identification' in k or 'Recessed red yoke' in k or 'Race red seal' in k]
   protected|=np.isin(labels,protected_ids)
  edge|=owned_boundaries(labels,protected)&mask
 raw[edge,:3]=ink
 if a.iteration>=10:
  lit=np.zeros_like(mask)
  for dy,dx in ((1,0),(0,1)):
   lit|=np.roll(edge,(dy,dx),(0,1))&(np.roll(labels,(dy,dx),(0,1))==labels)
  lit&=mask&~edge
  for key,color,threshold in (('cone',(190,64,66),110),('body',(95,97,89),55)):
   group_ids=[v for k,v in g['groups'].items() if k.startswith('Missile') and k.endswith(key)]
   selected=lit&np.isin(labels,group_ids)&(raw[:,:,0]>threshold)
   raw[selected,:3]=pal[((pal-np.array(color))**2).sum(axis=1).argmin()]
 im=Image.fromarray(raw);box=im.getbbox();assert box and min(box[:2])>0 and max(box[2:])<256,(i,box);bounds[i]=box;im.save(O/f'turret-{i:03d}.png');turrets[i]=im;frames[i]=Image.alpha_composite(base,im);frames[i].save(O/f'assembly-{i:03d}.png')
meta=PngInfo();meta.add_text('FrameSize','256,256');meta.add_text('FrameAmount','1');base.save(O/'base.png',pnginfo=meta)
board=Image.new('RGB',(1024,560),'#77736e');d=ImageDraw.Draw(board)
for k,i in enumerate(range(0,32,4)):
 x=k%4*256;y=k//4*280;board.paste(frames[i],(x,y+24),frames[i]);d.text((x+4,y+5),f'{i*11.25:g} degrees / native',fill='white')
board.save(O/'eight-native.png');board.resize((2048,1120),Image.Resampling.NEAREST).save(O/'eight-2x.png')
comp=Image.new('RGB',(768,280),'#77736e');d=ImageDraw.Draw(comp)
for k,(im,title) in enumerate(((source,'SUPPLIED SOURCE'),(frames[0],'MODEL / reference view'),(frames[16],'MODEL / inferred opposite'))):comp.paste(im,(k*256,24),im);d.text((k*256+4,5),title,fill='white')
comp.resize((1536,560),Image.Resampling.NEAREST).save(O/'comparison-2x.png')
parts=Image.new('RGB',(768,280),'#77736e');d=ImageDraw.Draw(parts)
for k,(im,title) in enumerate(((base,'FIXED / extracted base'),(turrets[0],'ROTATING / modeled turret'),(frames[0],'COMPOSITE'))):parts.paste(im,(k*256,24),im);d.text((k*256+4,5),title,fill='white')
parts.resize((1536,560),Image.Resampling.NEAREST).save(O/'parts-2x.png')
if len(frames)==32:
 sheet=Image.new('RGBA',(2048,1024));contact=Image.new('RGB',(2048,1120),'#77736e');d=ImageDraw.Draw(contact)
 for i in range(32):
  sheet.paste(turrets[(4+i)%32],(i%8*256,i//8*256));x=i%8*256;y=i//8*280;contact.paste(frames[i],(x,y+24),frames[i]);d.text((x+4,y+5),f'{i*11.25:g} degrees',fill='white')
 meta=PngInfo();meta.add_text('FrameSize','256,256');meta.add_text('FrameAmount','32');sheet.save(O/'turret32.png',pnginfo=meta);contact.save(O/'all32-native.png')
 views=[]
 for i in range(32):
  im=Image.new('RGBA',(256,280),(119,115,110,255));im.alpha_composite(frames[i],(0,24));ImageDraw.Draw(im).text((5,5),f'{i*11.25:g} degrees',fill='white');views.append(im.convert('RGB').resize((512,560),Image.Resampling.NEAREST))
 palette=Image.fromarray(np.array([[list(c) for c in sorted({c for v in views for c in v.getdata()})]],dtype=np.uint8)).quantize(colors=256,dither=Image.Dither.NONE)
 gif=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in views];gif[0].save(O/'rotation.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
(O/'validation.json').write_text(json.dumps({'pass':a.iteration,'facings':len(frames),'bounds':bounds,'clipped':False,'base_fixed':True,'base_source':'source-study/base.png','native_scale':1,'pivot_pixel':g['pivot_pixel'],'engine_facing_start':4,'runtime_installed':False},indent=2)+'\n');print('NOD_SAM_REVIEW',a.iteration)
