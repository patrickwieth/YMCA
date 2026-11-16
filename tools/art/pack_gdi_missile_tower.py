"""Pack true VXL/HVA with exact source-pixel indices; no quantization or runtime install."""
from pathlib import Path
from PIL import Image
import numpy as np,json,struct,re,argparse,hashlib
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);a=p.parse_args()
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/gdi missile tower';OUT=F/f'pass-{a.iteration:02d}';v=np.load(OUT/'surface-uv.npz');meta=json.loads((OUT/'voxel-sampling.json').read_text());coords=v['coords'];normals=v['normals'];size=v['grid_size'];lo=v['grid_min'];density=float(v['density'])
source=Image.open(next(F.glob('*.png'))).convert('RGB');colors=sorted(set(source.getdata()));assert len(colors)<255
palette=[(0,0,0)]+colors;palette += [(0,0,0)]*(256-len(palette));lookup={c:i+1 for i,c in enumerate(colors)};images=[np.array(Image.open(OUT/'source-cuts'/(n+'.png')).convert('RGBA')) for n in meta['textures']]
indices=[]
for uv,tid in zip(v['uv'],v['texture']):
 im=images[int(tid)];h,w=im.shape[:2]
 if meta['textures'][int(tid)].startswith('panel-'):
  x=max(0,min(w-1,int(np.floor(float(uv[0])*w))));y=max(0,min(h-1,h-1-int(np.floor(float(uv[1])*h))))
 else:
  x=int(np.floor(float(uv[0])*w))%w;y=(h-1-int(np.floor(float(uv[1])*h)))%h
 color=im[y,x];assert color[3]==255
 indices.append(lookup[tuple(map(int,color[:3]))])
indices=np.array(indices,dtype=np.uint8)
text=(ROOT/'engine/OpenRA.Mods.Cnc/Traits/World/VoxelNormalsPalette.cs').read_text();body=text.split('static readonly float[] RA2Normals =',1)[1].split('};',1)[0];table=np.array([float(x[:-1]) for x in re.findall(r'-?\d+(?:\.\d+)?f',body)]).reshape(-1,3);file_normals=np.column_stack((-normals[:,1],normals[:,0],normals[:,2]));normal_indices=(file_normals@table.T).argmax(axis=1)
# PNG is authoritative exact8-bit palette. The VXL legacy header stores6-bit RGB by format design.
pal=Image.new('P',(256,1));pal.putdata(list(range(256)));pal.putpalette([c for rgb in palette for c in rgb]);pal.info['transparency']=0;pal.save(OUT/'turret-palette.png')
nx,ny,nz=map(int,size);columns={}
for (x,y,z),c,n in zip(coords,indices,normal_indices):columns.setdefault(int(x)+nx*int(y),{})[int(z)]=(int(c),int(n))
starts=[];ends=[];data=bytearray()
for i in range(nx*ny):
 if i not in columns:starts.append(-1);ends.append(-1);continue
 starts.append(len(data));column=columns[i];z=0
 while z<nz:
  occupied=[k for k in column if k>=z]
  if not occupied:data.extend((nz-z,0,0));break
  first=min(occupied);end=first
  while end in column:end+=1
  count=end-first;data.extend((first-z,count))
  for k in range(first,end):data.extend(column[k])
  data.append(count);z=end
 ends.append(len(data)-1)
body=struct.pack('<'+'i'*len(starts),*starts)+struct.pack('<'+'i'*len(ends),*ends)+data
name=b'turret'.ljust(16,b'\0');identity=(1.,0.,0.,0.,0.,1.,0.,0.,0.,0.,1.,0.);bmin=lo;bmax=lo+size
# OpenRA emits slice vertices in raw lattice units, not bounds-normalized units.
# Scale both vertices AND their origin through the limb Scale, never only its bounds.
limb_scale=1.0/density
header=b'Voxel Animation\0'+struct.pack('<4I',1,1,1,len(body))+bytes((16,31))+bytes(c//4 for rgb in palette for c in rgb);limb=name+struct.pack('<3I',0,0,0);footer=struct.pack('<3If',0,nx*ny*4,nx*ny*8,limb_scale)+struct.pack('<12f',*identity)+struct.pack('<6f',*bmin,*bmax)+bytes((nx,ny,nz,4));assert len(header)==802 and len(footer)==92
(OUT/'missile-turret.vxl').write_bytes(header+limb+body+footer);(OUT/'missile-turret.hva').write_bytes(bytes(16)+struct.pack('<2I',1,1)+name+struct.pack('<12f',*identity))
# Decode every packed column again, including terminal empty spans. Preview consumes this checked lattice.
restored={}
for i,off in enumerate(starts):
 if off<0:continue
 z=0
 while z<nz:
  skip,count=data[off:off+2];off+=2;z+=skip
  for _ in range(count):restored[(i%nx,i//nx,z)]=tuple(data[off:off+2]);off+=2;z+=1
  assert data[off]==count;off+=1
assert len(restored)==len(coords)
for c,col,n in zip(coords,indices,normal_indices):assert restored[tuple(map(int,c))]==(int(col),int(n))
np.savez_compressed(OUT/'colored-voxels.npz',coords=coords,colors=indices,normals=normal_indices.astype(np.uint8),palette=np.array(palette,dtype=np.uint8),grid_min=lo,grid_size=size,density=density)
report={'format':'real single-limb Westwood VXL/HVA','grid_size':size.tolist(),'grid_min':lo.tolist(),'density':density,'limb_scale':limb_scale,'surface_voxels':len(coords),'palette_colors':len(colors),'texture_source':'exact nearest UV texel from documented source cuts; no color fitting/recolor/fallback','palette_requirement':'use turret-palette.png via PaletteFromPng; legacy embedded6-bit palette is inherently lossy','normal_type':'RA2','roundtrip':'every encoded voxel coordinate/color/normal decoded and matched','source_sha256':hashlib.sha256(next(F.glob('*.png')).read_bytes()).hexdigest(),'runtime_installed':False,'hidden_upper_rear':'inferred geometry, native copied surface pixels'}
(OUT/'export.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
