"""Pack three independently rendered Westwood VXL/HVA components, one pose each."""
from pathlib import Path
import numpy as np,struct,re,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];STUDY=ROOT/'art-source/soviet flak/voxel-01';OUT=ROOT/'mods/ca/bits/soviet/buildings/experimental';OUT.mkdir(exist_ok=True)
text=(ROOT/'engine/OpenRA.Mods.Cnc/Traits/World/VoxelNormalsPalette.cs').read_text();body=text.split('static readonly float[] RA2Normals =',1)[1].split('};',1)[0];table=np.array([float(x[:-1]) for x in re.findall(r'-?\d+(?:\.\d+)?f',body)]).reshape(-1,3)
identity=(1.,0.,0.,0.,0.,1.,0.,0.,0.,0.,1.,0.);report={}
for part in ('base','turret','barrel'):
 v=np.load(STUDY/f'{part}.npz');nx,ny,nz=map(int,v['grid_size']);normal_indices=(v['normals']@table.T).argmax(axis=1)
 rgb=[(0,0,0)]+[tuple(map(int,c)) for c in v['palette']];rgb += [(0,0,0)]*(256-len(rgb));columns={}
 for (x,y,z),color,normal in zip(v['coords'],v['colors'],normal_indices):columns.setdefault(int(x)+nx*int(y),{})[int(z)]=(int(color),int(normal))
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
 low=v['grid_min']*20/float(v['density']);high=low+v['grid_size']*20/float(v['density']);name=part.encode().ljust(16,b'\0')
 header=b'Voxel Animation\0'+struct.pack('<4I',1,1,1,len(body))+bytes((16,31))+bytes(c//4 for row in rgb for c in row)
 footer=struct.pack('<3If',0,nx*ny*4,nx*ny*8,1.)+struct.pack('<12f',*identity)+struct.pack('<6f',*low,*high)+bytes((nx,ny,nz,4))
 prefix=OUT/f'flak-{part}';prefix.with_suffix('.vxl').write_bytes(header+name+struct.pack('<3I',0,0,0)+body+footer);prefix.with_suffix('.hva').write_bytes(bytes(16)+struct.pack('<2I',1,1)+name+struct.pack('<12f',*identity))
 report[part]={'voxels':len(v['coords']),'bounds_min':low.tolist(),'bounds_max':high.tolist(),'hva_frames':1,'render_scale':1}
 pal=Image.new('P',(256,1));pal.putdata(list(range(256)));pal.putpalette([c for p in rgb for c in p]);pal.info['transparency']=0;pal.save(OUT/'flak-voxel-palette.png')
(STUDY/'export.json').write_text(json.dumps(report,indent=2)+'\n');print('SOVIET_FLAK_VXL_PACKED',report)
