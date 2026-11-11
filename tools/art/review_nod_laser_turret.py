"""Palette-exact native reviews; geometry-derived interior contours, no alpha growth."""
from pathlib import Path
import argparse,json,hashlib
import numpy as np
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
from contour_geometry import owned_boundaries
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);args=p.parse_args();ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'art-source/nod laser turret';study=folder/f'pass-{args.iteration:02d}';out=study/'review';out.mkdir(exist_ok=True);g=json.loads((study/'geometry.json').read_text());source=Image.open(folder/'laser3 turret.png').convert('RGBA');colors=sorted({c[:3] for c in source.getdata() if c[3]==255});pal=np.array(colors,dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()];size=256

def finish(path,ids=None):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128;u,inv=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);a[:,:,:3]=pal[idx][inv].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 edge=np.zeros_like(solid)
 for dy,dx in ((0,1),(0,-1),(1,0),(-1,0)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 if ids:
  labels=np.array(Image.open(ids).convert('RGBA'))[:,:,0].copy();labels[~solid]=0;counts=np.bincount(labels.flatten(),minlength=256);protected=((a[:,:,0]>150)&(a[:,:,0]>a[:,:,1]*1.5))|(counts[labels]<8)
  if args.iteration>=2:
   teeth=[v for k,v in g['groups'].items() if k.endswith(' teeth') or (args.iteration>=3 and k=='Copper emitter bezel')];protected|=np.isin(labels,teeth)
  edge|=owned_boundaries(labels,protected)
 a[edge,:3]=ink;im=Image.fromarray(a);bounds=im.getbbox();assert bounds and min(bounds[:2])>0 and max(bounds[2:])<size,(path,bounds);return im
base=finish(study/'base-raw.png');assert base.tobytes()==finish(study/'base-control-raw.png').tobytes();meta=PngInfo();meta.add_text('FrameSize','256,256');meta.add_text('FrameAmount','1');base.save(out/'base.png',pnginfo=meta);frames={}
for raw in sorted(study.glob('turret-???-raw.png')):
 i=int(raw.name.split('-')[1]);turret=finish(raw,study/f'turret-{i:03d}-ids.png');turret.save(out/f'turret-{i:03d}.png');assembly=Image.alpha_composite(base,turret);assembly.save(out/f'assembly-{i:03d}.png');frames[i]=assembly
board=Image.new('RGB',(size*4,(size+22)*2),'#77736e');d=ImageDraw.Draw(board)
for cell,i in enumerate(range(0,32,4)):
 x=cell%4*size;y=cell//4*(size+22);board.paste(frames[i],(x,y+22),frames[i]);d.text((x+5,y+4),f'{i*11.25:g} deg',fill='white')
board.save(out/'eight-views-native.png');board.resize((size*8,(size+22)*4),Image.Resampling.NEAREST).save(out/'eight-views-2x.png')
ref=Image.new('RGBA',(size,size));ref.alpha_composite(source,(48,48));compare=Image.new('RGB',(size*3,size+22),'#77736e');d=ImageDraw.Draw(compare)
for col,(im,label) in enumerate(((ref,'SOURCE / unchanged'),(frames[0],'RECONSTRUCTION / native'),(frames[16],'OPPOSITE / inferred'))):compare.paste(im,(col*size,22),im);d.text((col*size+4,4),label,fill='white')
compare.resize((size*6,(size+22)*2),Image.Resampling.NEAREST).save(out/'source-comparison-2x.png')
views=[];gifpal=Image.new('P',(1,1));rgb=colors+[(119,115,110),(255,255,255)];assert len(rgb)<=256;rgb += [(0,0,0)]*(256-len(rgb));gifpal.putpalette([c for color in rgb for c in color])
for i,im in frames.items():
 v=Image.new('RGB',(size,size+22),'#77736e');v.paste(im,(0,22),im);ImageDraw.Draw(v).text((5,4),f'{i*11.25:g} deg / native / no game binding',fill='white');views.append(v.resize((size*2,(size+22)*2),Image.Resampling.NEAREST).quantize(palette=gifpal,dither=Image.Dither.NONE))
views[0].save(out/'rotation.gif',save_all=True,append_images=views[1:],duration=110 if len(views)==32 else 220,loop=0)
if len(frames)==32:
 sheet=Image.new('RGBA',(2048,1024));contact=Image.new('RGB',(2048,1112),'#77736e')
 # Model forward -Y: render index20 points engine north. Counterclockwise engine order.
 for i in range(32):
  sheet.paste(Image.open(out/f'turret-{(20+i)%32:03d}.png'),((i%8)*256,(i//8)*256));contact.paste(frames[i],((i%8)*256,(i//8)*278+22),frames[i])
 meta=PngInfo();meta.add_text('FrameSize','256,256');meta.add_text('FrameAmount','32');sheet.save(out/'turret32.png',pnginfo=meta);contact.save(out/'all-32-native.png')
controls={};rawbase=Image.open(study/'base-raw.png').convert('RGBA')
for i in (0,4,12,20,28):
 raw=Image.open(study/f'turret-{i:03d}-raw.png').convert('RGBA');full=Image.open(study/('assembly-reference-raw.png' if i==0 else f'assembly-control-{i:03d}-raw.png')).convert('RGBA');diff=int(np.any(np.array(full)!=np.array(Image.alpha_composite(rawbase,raw)),axis=2).sum());assert diff==0,(i,diff);controls[i]=diff
report={'source_sha256':hashlib.sha256((folder/'laser3 turret.png').read_bytes()).hexdigest(),'blend_sha256':hashlib.sha256((study/'nod-laser-model.blend').read_bytes()).hexdigest(),'directions':len(frames),'frame_size':[256,256],'scale':1,'clipped_frames':0,'source_palette_only':True,'binary_alpha':True,'stationary_control_equal':True,'full_3d_vs_layers_pixel_errors':controls,'geometry_contours':True,'game_installed':False};(out/'validation.json').write_text(json.dumps(report,indent=2)+'\n');print('NOD_LASER_REVIEW_READY',args.iteration)
