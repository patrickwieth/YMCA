"""Real one-limb VXL/HVA export from sampled V13 geometry and source albedo views.
No procedural texture: colors are sampled from the existing 32 matching renders.
"""
from pathlib import Path
from PIL import Image
import numpy as np,json,struct,re,math,hashlib
ROOT=Path(__file__).resolve().parents[2];STUDY=ROOT/'art-source/soviet-sentry/voxel-01';RENDERS=ROOT/'art-source/soviet-sentry/game-v13-32-native'
OUT=ROOT/'mods/ca/bits/soviet/buildings/experimental';OUT.mkdir(parents=True,exist_ok=True)
v=np.load(STUDY/'surface.npz');coords=v['coords'];points=v['points'];normals=v['normals'];size=v['grid_size'];lo=v['grid_min'];density=int(v['density'])
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=sorted({c[:3] for c in source.getdata() if c[3]==255});palette=np.array(colors,dtype=np.int32)
images=np.stack([np.array(Image.open(RENDERS/f'albedo-{i:03d}.png').convert('RGBA')) for i in range(32)])
projected=np.einsum('vab,nb->nva',v['projections'],np.column_stack((points,np.ones(len(points)))))
scores=normals@v['views'].T;order=np.argsort(-scores,axis=1,kind='stable');sampled=[];fallbacks=0
for n in range(len(points)):
 rgb=None
 for angle in order[n]:
  x,y=np.floor(projected[n,angle]).astype(int)
  if 0<=x<120 and 0<=y<120 and images[angle,y,x,3]>=128:
   rgb=images[angle,y,x,:3];break
 if rgb is None:rgb=(3,2,1);fallbacks+=1
 sampled.append(rgb)
sampled=np.array(sampled,dtype=np.int32);indices=((sampled[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)+1
# RA2 normal table is the one actually used by this mod's normals palette.
text=(ROOT/'engine/OpenRA.Mods.Cnc/Traits/World/VoxelNormalsPalette.cs').read_text()
body=text.split('static readonly float[] RA2Normals =',1)[1].split('};',1)[0]
table=np.array([float(x[:-1]) for x in re.findall(r'-?\d+(?:\.\d+)?f',body)]).reshape(-1,3)
file_normals=np.column_stack((-normals[:,1],normals[:,0],normals[:,2]));normal_indices=(file_normals@table.T).argmax(axis=1)
# Indexed PNG carries exact 8-bit source colors, unlike the lossy legacy 6-bit PAL.
pal=Image.new('P',(256,1));pal.putdata(list(range(256)));rgb=[(0,0,0)]+colors
rgb+= [(0,0,0)]*(256-len(rgb));pal.putpalette([c for p in rgb for c in p]);pal.info['transparency']=0;pal.save(OUT/'sentry-voxel-palette.png')
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
# Native sprite density: 120px / camera ortho 6.75. One voxel is ~1 native pixel.
render_scale=(120/6.75)/density
world_turret_screen_y=-112*64/1448;socket_y=-3-43/2+8
z_shift=(world_turret_screen_y-socket_y)/(math.cos(math.radians(30))*render_scale)
bounds_min=lo.astype(float);bounds_min[2]+=z_shift;bounds_max=bounds_min+size
name=b'turret'.ljust(16,b'\0');identity=(1.,0.,0.,0.,0.,1.,0.,0.,0.,0.,1.,0.)
header=b'Voxel Animation\0'+struct.pack('<4I',1,1,1,len(body))+bytes((16,31))+bytes(c//4 for row in rgb for c in row)
limb=name+struct.pack('<3I',0,0,0)
footer=struct.pack('<3If',0,nx*ny*4,nx*ny*8,1.)+struct.pack('<12f',*identity)+struct.pack('<6f',*bounds_min,*bounds_max)+bytes((nx,ny,nz,4))
assert len(header)==802 and len(limb)==28 and len(footer)==92
(OUT/'sentry-v13-voxel.vxl').write_bytes(header+limb+body+footer)
(OUT/'sentry-v13-voxel.hva').write_bytes(bytes(16)+struct.pack('<2I',1,1)+name+struct.pack('<12f',*identity))
np.savez_compressed(STUDY/'colored-voxels.npz',coords=coords,colors=indices.astype(np.uint8),normals=normal_indices.astype(np.uint8),palette=np.array(rgb,dtype=np.uint8),grid_min=lo,grid_size=size,density=density)
report={'format':'real Westwood VXL/HVA, not sprite frames','limbs':1,'hva_frames':1,'surface_voxels':len(coords),'grid_size':size.tolist(),'grid_min':lo.tolist(),'bounds_min':bounds_min.tolist(),'bounds_max':bounds_max.tolist(),'render_scale':render_scale,'model_z_shift':z_shift,'source_palette_colors':len(colors),'normal_type':'RA2','color_method':'nearest source palette applied to best-normal-facing albedo projection; hidden surfaces approximate','unsampled_dark_fallback_voxels':fallbacks,'source_palette_png':'bits/soviet/buildings/experimental/sentry-voxel-palette.png','source_geometry_sha256':json.loads((STUDY/'geometry.json').read_text())['source_sha256'],'live_engine_appearance':'pending, especially sprite-base/voxel depth layering','known_limitations':['surface-shell voxelization may thicken sub-voxel details','runtime normal lighting changes displayed RGB; not identical to finished sprite palette/ink','old V13 bore geometry retained']}
(STUDY/'export.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
