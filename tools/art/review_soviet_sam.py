"""Native source-palette exports; separate fixed base/rotating rack and honest review boards."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,argparse
from contour_geometry import owned_boundaries
p=argparse.ArgumentParser();p.add_argument('--pass',dest='stage',type=int,required=True);a=p.parse_args();ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/soviet sam site';D=F/f'pass-{a.stage:02d}';O=D/'review';O.mkdir(exist_ok=True);g=json.loads((D/'geometry.json').read_text());SIZE=192
source=Image.open(F/'sam_site-887352960-0.png').convert('RGBA');pal=np.array(sorted(set(tuple(c) for c in g['palette'].values())),dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()]
def finish(name):
 raw=np.array(Image.open(D/f'{name}-raw.png').convert('RGBA'));mask=raw[:,:,3]>=128;u,inv=np.unique(raw[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);raw[:,:,:3]=pal[idx][inv].reshape((SIZE,SIZE,3));raw[:,:,3]=mask*255;raw[~mask]=0
 ids=np.array(Image.open(D/f'{name}-ids.png')).astype(np.uint16);labels=ids[:,:,0]+256*ids[:,:,1];labels[~mask]=0;edge=np.zeros_like(mask)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=mask&~np.roll(mask,(dy,dx),(0,1))
 counts=np.bincount(labels.ravel());protected=counts[labels]<6
 # Tiny screws, markings and red diagnostic inserts must retain their actual colors.
 keep=[v for k,v in g['groups'].items() if any(w in k for w in ('Yellow','Red port','status lamp','hazard','caution','bolt','screw','fastener'))];protected|=np.isin(labels,keep)
 if a.stage>=2:edge|=owned_boundaries(labels,protected)&mask
 raw[edge,:3]=ink;im=Image.fromarray(raw);b=im.getbbox();assert b and min(b[:2])>1 and max(b[2:])<SIZE-1,(name,b);return im
base=finish('base');meta=PngInfo();meta.add_text('FrameSize','192,192');meta.add_text('FrameAmount','1');base.save(O/'base.png',pnginfo=meta)
frames={};turrets={}
for i in g['facings']:
 im=finish(f'turret-{i:03d}');im.save(O/f'turret-{i:03d}.png');turrets[i]=im;frames[i]=Image.alpha_composite(base,im);frames[i].save(O/f'assembly-{i:03d}.png')
bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA')
def groundview(frame):
 im=Image.new('RGBA',(SIZE,SIZE),(119,115,110,255));im.alpha_composite(bib,(32,114));im.alpha_composite(frame);return im
board=Image.new('RGB',(768,432),'#77736e');d=ImageDraw.Draw(board)
for k,i in enumerate(range(0,32,4)):
 x=k%4*SIZE;y=k//4*216;im=groundview(frames[i]);board.paste(im,(x,y+24));d.text((x+4,y+5),f'{i*11.25:g} deg / 1x1 bib',fill='white')
board.save(O/'eight-native.png');board.resize((1536,864),Image.Resampling.NEAREST).save(O/'eight-2x.png')
comp=Image.new('RGB',(768,280),'#77736e');d=ImageDraw.Draw(comp)
comp.paste(source,(0,24),source);d.text((4,5),'SUPPLIED 256px SOURCE',fill='white')
# Enlarged native cut at integer scale, not misleading source-scale comparison.
for k,(i,title) in enumerate(((0,'MODEL reference / native2x crop'),(16,'MODEL rear / inferred')),1):
 im=frames[i];comp.paste(im,(k*256+32,56),im);d.text((k*256+4,5),title.replace('native2x crop','native'),fill='white')
comp.resize((1536,560),Image.Resampling.NEAREST).save(O/'comparison-2x.png')
parts=Image.new('RGB',(576,216),'#77736e');d=ImageDraw.Draw(parts)
for k,(im,title) in enumerate(((base,'FIXED modeled base'),(turrets[0],'ROTATING launcher'),(groundview(frames[0]),'ASSEMBLY / 1x1'))):parts.paste(im,(k*192,24),im);d.text((k*192+4,5),title,fill='white')
parts.resize((1152,432),Image.Resampling.NEAREST).save(O/'parts-2x.png')
if len(frames)==32:
 sheet=Image.new('RGBA',(SIZE*8,SIZE*4));contact=Image.new('RGB',(SIZE*8,216*4),'#77736e');d=ImageDraw.Draw(contact)
 for i in range(32):
  # Direction registration is explicitly provisional until runtime integration.
  sheet.paste(turrets[i],(i%8*SIZE,i//8*SIZE));contact.paste(groundview(frames[i]),(i%8*SIZE,i//8*216+24));d.text((i%8*SIZE+4,i//8*216+5),f'{i*11.25:g} deg',fill='white')
 meta=PngInfo();meta.add_text('FrameSize','192,192');meta.add_text('FrameAmount','32');sheet.save(O/'turret32.png',pnginfo=meta);contact.save(O/'all32-native.png')
 views=[groundview(frames[i]).convert('RGB').resize((384,384),Image.Resampling.NEAREST) for i in range(32)];palette=Image.fromarray(np.array([[list(c) for c in sorted({c for im in views for c in im.getdata()})]],dtype=np.uint8)).quantize(colors=256,dither=Image.Dither.NONE);gif=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in views];gif[0].save(O/'rotation.gif',save_all=True,append_images=gif[1:],duration=110,loop=0)
(O/'validation.json').write_text(json.dumps({'pass':a.stage,'facings':len(frames),'base_bounds':base.getbbox(),'assembly_bounds':{i:im.getbbox() for i,im in frames.items()},'native_density':g['density'],'runtime_installed':False,'base_modeled':True,'base_diamond_width':g['base_diamond_width']},indent=2)+'\n');print('SOVIET_SAM_REVIEW',a.stage)
