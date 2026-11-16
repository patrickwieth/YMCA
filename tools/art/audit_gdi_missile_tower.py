"""Audit exact copied source texels and VXL indices. This does not grade visual quality."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,struct,argparse
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,default=12);a=p.parse_args()
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/gdi missile tower';OUT=F/f'pass-{a.iteration:02d}';CUT=OUT/'source-cuts';manifest=json.loads((CUT/'manifest.json').read_text());src=ROOT/manifest['source'];sha=hashlib.sha256(src.read_bytes()).hexdigest();assert sha==manifest['source_sha256']=='d2ee8229382a6db3f21c1cd5663f91b168408de08f44986b8f3155745d1356dc'
source=np.array(Image.open(src).convert('RGB'));cuts={c['name']:c for c in manifest['cuts'] if 'source_rect' in c}
for name,c in cuts.items():
 x0,y0,x1,y1=c['source_rect'];image=np.array(Image.open(CUT/(name+'.png')).convert('RGBA'));assert (image[:,:,3]==255).all();assert np.array_equal(image[:,:,:3],source[y0:y1,x0:x1]),name
base=np.array(Image.open(CUT/'base.png').convert('RGBA'));original_colors=set(map(tuple,source.reshape(-1,3)));assert set(map(tuple,base[base[:,:,3]>0,:3]))<=original_colors
# Remaining visible source architecture retains native RGB; requested cuts may remove pixels.
from PIL import ImageDraw
repaired=np.zeros((128,128),dtype=bool)
for record in manifest['cuts']:
 if record['name']=='removed-right-turbines':
  mask=Image.new('1',(128,128));ImageDraw.Draw(mask).polygon(record['source_polygon'],fill=1);repaired=np.array(mask,dtype=bool)
  if 'ground_rect' in record:
   assert (base[:,:,3][repaired]==255).all(),'Repair must not leave a transparent hole.'
   original=np.array(Image.open(F/'pass-10/base.png').convert('RGBA'))
   assert np.array_equal(base[~repaired],original[~repaired]),'Unrelated base pixels changed.'
  else:assert (base[:,:,3][repaired]==0).all()
alpha=(base[54:,:,3]>0)&~repaired[54:];assert np.array_equal(base[54:,:,:3][alpha],source[54:][alpha])
raw=np.load(OUT/'surface-uv.npz');colored=np.load(OUT/'colored-voxels.npz');names=json.loads((OUT/'voxel-sampling.json').read_text())['textures'];xy=[]
for uv,tid,col in zip(raw['uv'],raw['texture'],colored['colors']):
 c=cuts[names[int(tid)]];x0,y0,x1,y1=c['source_rect'];w=x1-x0;h=y1-y0;x=int(np.floor(float(uv[0])*w))%w;y=(h-1-int(np.floor(float(uv[1])*h)))%h
 assert np.array_equal(source[y0+y,x0+x],colored['palette'][col]);xy.append((x0+x,y0+y))
np.savez_compressed(OUT/'source-pixel-lookup.npz',voxel=raw['coords'],source_xy=np.array(xy,dtype=np.uint8))
# Actual exported binary, not merely the intermediate arrays.
vxl=(OUT/'missile-turret.vxl').read_bytes();assert vxl[:16]==b'Voxel Animation\0';limbs,repeated,body_size=struct.unpack_from('<III',vxl,20);assert limbs==repeated==1;body=vxl[830:830+body_size];footer=vxl[830+body_size:];assert len(footer)==92;nx,ny,nz,normal_type=footer[-4:];assert normal_type==4
starts=struct.unpack_from('<'+'i'*(nx*ny),body);data_start=nx*ny*8;decoded={}
for i,offset in enumerate(starts):
 if offset<0:continue
 at=data_start+offset;z=0
 while z<nz:
  skip,count=body[at:at+2];at+=2;z+=skip
  for _ in range(count):decoded[(i%nx,i//nx,z)]=tuple(body[at:at+2]);at+=2;z+=1
  assert body[at]==count;at+=1
assert len(decoded)==len(colored['coords'])
for c,color,normal in zip(colored['coords'],colored['colors'],colored['normals']):assert decoded[tuple(map(int,c))]==(int(color),int(normal))
hva=(OUT/'missile-turret.hva').read_bytes();assert len(hva)==88 and struct.unpack_from('<II',hva,16)==(1,1)
for prefix in ('turret','voxel'):
 frames=[p for p in OUT.glob(prefix+'-*.png') if p.stem.removeprefix(prefix+'-').isdigit()];assert len(frames)==32
 for frame in frames:
  im=Image.open(frame).convert('RGBA');b=im.getbbox();assert b and b[0]>0 and b[1]>0 and b[2]<im.width and b[3]<im.height,frame.name
report={'source_unchanged':True,'textures_exact_rectangular_copies':True,'voxel_colors_traced_to_original_pixels':True,'remaining_visible_base_pixels_unchanged':True,'vxl_binary_roundtrip':True,'hva_identity_frame_valid':True,'full_rotation_unclipped':True,'runtime_installed':all((ROOT/'mods/ca/bits/gdi/buildings'/b).exists() and (OUT/a).read_bytes()==(ROOT/'mods/ca/bits/gdi/buildings'/b).read_bytes() for a,b in [('base.png','missile-tower-base.png'),('missile-turret.vxl','missile-tower-turret.vxl'),('missile-turret.hva','missile-tower-turret.hva'),('turret-palette.png','missile-tower-palette.png')]),'visual_review':'Separate native mesh/voxel turntables; not inferred from these checks.'}
(OUT/'audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
