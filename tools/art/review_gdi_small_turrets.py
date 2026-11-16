"""Native palette finishing, separate parts and honest multiview study reviews."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib,argparse
from contour_geometry import owned_boundaries
p=argparse.ArgumentParser();p.add_argument('--kind',choices=('aa','sonic','allies','soviet'),required=True);p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--allow-clipping',action='store_true');args=p.parse_args();ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'art-source'/{'aa':'gdi aa gun','sonic':'gdi sonic turret','allies':'allies flak','soviet':'soviet flak'}[args.kind];SOURCE=FOLDER/'flak.png' if args.kind=='soviet' else next(FOLDER.glob('*.png'));STUDY=FOLDER/f'pass-{args.iteration:02d}';OUT=STUDY/'review';OUT.mkdir(exist_ok=True);g=json.loads((STUDY/'geometry.json').read_text());size=g['frame_size'][0]
source=Image.open(SOURCE).convert('RGBA');colors={c[:3] for c in source.getdata() if c[3]==255}
if args.kind=='soviet':
 colors|={c[:3] for c in Image.open(FOLDER/'flak base.png').convert('RGBA').getdata()};colors={c for c in colors if min(c)<243}
pal=np.array(sorted(colors),dtype=np.int32);ink=pal[pal.sum(axis=1).argmin()]
if args.kind=='allies' and args.iteration>=10:
 # Review-only background isolation: the supplied original is never overwritten.
 source=source.copy()
 for point in ((0,0),(source.width-1,0),(0,source.height-1),(source.width-1,source.height-1)):
  ImageDraw.floodfill(source,point,(0,0,0,0),thresh=12)
clipped=[]
def finish(path):
 a=np.array(Image.open(path).convert('RGBA'));solid=a[:,:,3]>=128;u,inv=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);idx=((u.astype(np.int32)[:,None,:]-pal[None,:,:])**2).sum(axis=2).argmin(axis=1);a[:,:,:3]=pal[idx][inv].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 edge=np.zeros_like(solid)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 armor_edge=np.zeros_like(solid);metal_edge=np.zeros_like(solid)
 ids_path=path.with_name(path.name.replace('-raw.png','-ids.png'))
 if (args.iteration>=9 or args.kind=='allies' and args.iteration>=4 or args.kind=='soviet') and ids_path!=path and ids_path.exists():
  ids=np.array(Image.open(ids_path).convert('RGBA'));labels=ids[:,:,0].astype(np.uint16)
  if g.get('surface_id_channels',1)==2:labels+=ids[:,:,1].astype(np.uint16)*256
  labels[~solid]=0;counts=np.bincount(labels.ravel(),minlength=256);protected=(counts[labels]<8)|((a[:,:,1]>150)&(a[:,:,2]>150)&(a[:,:,0]<a[:,:,1]*.8))
  if args.kind=='soviet' and args.iteration>=5:protected|=(a[:,:,0]>a[:,:,1]*1.35)&(a[:,:,0]>a[:,:,2]*1.35)
  interior=owned_boundaries(labels,protected)
  if args.iteration>=10 and args.kind=='aa':armor_edge=interior&~edge&(a[:,:,0].astype(int)>a[:,:,1].astype(int)+20)&(a[:,:,1].astype(int)>a[:,:,2].astype(int)+20)
  if args.kind=='allies' and args.iteration>=9:metal_edge=interior&~edge
  edge|=interior
 a[edge,:3]=ink
 if metal_edge.any():a[metal_edge,:3]=pal[((pal-np.array([27,34,44] if args.iteration>=10 else [43,53,63]))**2).sum(axis=1).argmin()]
 if armor_edge.any():a[armor_edge,:3]=pal[((pal-np.array([103,75,39]))**2).sum(axis=1).argmin()]
 if args.kind=='sonic' and args.iteration>=20 and path.name.startswith('turret-') and ids_path.exists():
  # Keep the dark outer line; put exactly one native highlight pixel inside
  # upper/left geometry edges, like the supplied pedestal's machined trim.
  lit_edge=np.zeros_like(solid)
  for dy,dx in ((1,0),(0,1)):
   lit_edge|=np.roll(edge,(dy,dx),(0,1))&(np.roll(labels,(dy,dx),(0,1))==labels)
  lit_edge&=solid&~edge&~protected&(counts[labels]>=24)
  if args.iteration>=21:lit_edge&=ids[:,:,2]>=128
  gold=(a[:,:,0].astype(int)>a[:,:,1].astype(int)+20)&(a[:,:,1].astype(int)>a[:,:,2].astype(int)+20)
  steel=(a[:,:,0]>40)&(a[:,:,0]<a[:,:,1]*1.15)&(a[:,:,1]<a[:,:,2]*1.25)
  for mask,target in ((lit_edge&gold,[243,208,114]),(lit_edge&steel,[151,174,178])):
   a[mask,:3]=pal[((pal-np.array(target))**2).sum(axis=1).argmin()]
 im=Image.fromarray(a);box=im.getbbox();assert box
 if min(box[:2])<=0 or max(box[2:])>=size:
  clipped.append(str(path.name));assert args.allow_clipping,(path,box)
 return im
base=finish(STUDY/'base-raw.png');assert base.tobytes()==finish(STUDY/'base-control-raw.png').tobytes()
if args.kind=='allies' and args.iteration>=14:base=Image.open(STUDY/'original-base-native.png').convert('RGBA')
base.save(OUT/'base.png');frames={};bib=Image.open(ROOT/'mods/ca/bits/gdi/buildings/bib1x1.png').convert('RGBA');origin=g['ground_origin_frame']
if args.kind in ('allies','soviet'):bib=Image.new('RGBA',(128,64))
for raw in sorted(STUDY.glob('turret-???-raw.png')):
 idx=int(raw.name.split('-')[1]);turret=finish(raw);turret.save(OUT/f'turret-{idx:03d}.png');a=Image.alpha_composite(base,turret);a.save(OUT/f'assembly-{idx:03d}.png');frames[idx]=a
contact=Image.new('RGB',(size*4,(size+24)*2),'#77736e');d=ImageDraw.Draw(contact)
for cell,idx in enumerate(range(0,32,4)):
 a=frames[idx];x=cell%4*size;y=cell//4*(size+24);contact.paste(a,(x,y+24),a);d.text((x+5,y+5),f'{idx*11.25:g} deg / native {size}',fill='white')
contact.resize((size*8,(size+24)*4),Image.Resampling.NEAREST).save(OUT/'eight-views-2x.png')
ref=Image.new('RGBA',(size,size));ref.alpha_composite(source,((size-source.width)//2,(size-source.height)//2));main=Image.new('RGB',(size*3,size+24),'#77736e');d=ImageDraw.Draw(main)
for i,(im,title) in enumerate(((ref,'SOURCE / isolated background' if args.kind=='allies' and args.iteration>=10 else 'SOURCE / padding only'),(frames[0],'MODEL / reference angle'),(frames[16],'MODEL / opposite inferred'))):main.paste(im,(i*size,24),im);d.text((i*size+5,5),title,fill='white')
main.resize((size*6,(size+24)*2),Image.Resampling.NEAREST).save(OUT/'source-comparison-2x.png')
views=[]
for idx in sorted(frames):
 v=Image.new('RGBA',(size,size+24),(119,115,110,255));v.alpha_composite(bib,(round(origin[0]-64),round(origin[1]-32+24)));v.alpha_composite(frames[idx],(0,24));ImageDraw.Draw(v).text((5,5),f'{"native study" if args.kind in ("allies","soviet") else "1x1 bib"} / {idx*11.25:g} deg',fill='white');views.append(v.convert('RGB').resize((size*2,(size+24)*2),Image.Resampling.NEAREST))
palette=Image.new('P',(1,1));allcolors=colors|{c[:3] for c in bib.getdata() if c[3]}|{(119,115,110),(255,255,255)}
# One shared palette avoids preview-only color flicker between directions.
rgb=sorted(allcolors);assert len(rgb)<=256;rgb += [(0,0,0)]*(256-len(rgb));palette.putpalette([c for color in rgb for c in color]);gif=[v.quantize(palette=palette,dither=Image.Dither.NONE) for v in views]
gif[0].save(OUT/('rotation.gif' if args.kind in ('allies','soviet') else 'rotation-with-1x1-bib.gif'),save_all=True,append_images=gif[1:],duration=110 if len(views)==32 else 220,loop=0)
parts=Image.new('RGB',(size*3,size+24),'#77736e');d=ImageDraw.Draw(parts)
for i,(im,title) in enumerate(((base,'FIXED BASE'),(Image.open(OUT/'turret-000.png'),'ROTATING TURRET'),(frames[0],'SEPARATE LAYER COMPOSITE'))):parts.paste(im,(i*size,24),im);d.text((i*size+4,4),title,fill='white')
parts.resize((size*6,(size+24)*2),Image.Resampling.NEAREST).save(OUT/'separate-parts-2x.png')
if len(frames)==32:
 full_contact=Image.new('RGB',(size*8,(size+24)*4),'#77736e');fd=ImageDraw.Draw(full_contact)
 for idx,a in frames.items():
  x=(idx%8)*size;y=(idx//8)*(size+24);full_contact.paste(a,(x,y+24),a);fd.text((x+4,y+4),f'{idx*11.25:g} deg',fill='white')
 full_contact.save(OUT/'all-32-native.png')
 sheet=Image.new('RGBA',(size*8,size*4))
 # AA source gun +Y: source angle0 is engine NE, index order starts at +45deg.
 # Sonic source gun +X: source angle0 is engine SE, ordering starts at +135deg.
 start={'aa':4,'sonic':12,'allies':20,'soviet':20}[args.kind]
 for i in range(32):sheet.paste(Image.open(OUT/f'turret-{(start+i)%32:03d}.png'),((i%8)*size,(i//8)*size))
 meta=PngInfo();meta.add_text('FrameSize',f'{size},{size}');meta.add_text('FrameAmount','32');sheet.save(OUT/'turret32.png',pnginfo=meta);meta=PngInfo();meta.add_text('FrameSize',f'{size},{size}');meta.add_text('FrameAmount','1');base.save(OUT/'base.png',pnginfo=meta)
report={'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'native_frame_size':[size,size],'directions':len(frames),'scale':1,'palette':'source colors only','alpha':'binary','clipped_frames':len(clipped),'clipped_files':clipped,'stationary_control_equal':True,'parts_separate':True,'game_installed':False,'source_comparison':'not an in-game capture','geometry':g};(OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n');print('SMALL_TURRET_REVIEW_READY',args.kind,args.iteration)
