"""Background Blender: real three-part voxel sampling, independent of sprite views."""
from pathlib import Path
import bpy,json,math,hashlib,numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet flak/voxel-01';OUT.mkdir(exist_ok=True)
source=ROOT/'art-source/soviet flak/pass-13/soviet-flak-model.blend'
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene;scene.frame_set(0);bpy.data.objects['turret_pivot'].rotation_euler.z=0;bpy.context.view_layer.update()
meshes=[o for o in scene.objects if o.type=='MESH'];density=32
palette=set()
for filename in ('flak.png','flak base.png'):
 image=bpy.data.images.load(str(source.parent.parent/filename));pixels=list(image.pixels[:])
 palette.update(tuple(round(c*255) for c in pixels[i:i+3]) for i in range(0,len(pixels),4) if min(pixels[i:i+3])<.95)
palette=np.array(sorted(palette),dtype=np.int32)
colors={'steel':[(27,30,34),(44,49,54),(75,78,88),(109,125,135),(147,151,156),(175,182,185)],'brass':[(47,37,25),(65,52,33),(94,77,47),(122,103,68),(147,128,91),(171,152,116)],'red':[(42,23,24),(65,28,29),(95,37,35),(124,47,43),(155,67,56),(183,91,73)]}
wear=bpy.data.images.get('Soviet bounded steel wear');wear_pixels=list(wear.pixels[:]) if wear else []
report={'source_blend_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'density':density,'native_pixel_density':20,'parts':{},'method':'nearest real mesh surface; material albedo plus engine normal lighting, not projected sprite pictures'}
for part in ('base','turret','barrel'):
 selected=[o for o in meshes if ('base' if o['fixed'] else 'barrel' if o.parent and o.parent.name=='barrel_recoil' else 'turret')==part]
 vertices=[];faces=[];families=[];uvs=[]
 for o in selected:
  mesh=o.data;mesh.calc_loop_triangles();start=len(vertices);vertices.extend(o.matrix_world@v.co for v in mesh.vertices)
  uv=mesh.uv_layers.get('Native steel wear')
  for tri in mesh.loop_triangles:
   faces.append(tuple(start+i for i in tri.vertices))
   tone=max(0,min(5,4+o['bias']))
   if o.name.startswith(('Hollow main barrel','Upper recoil rod','Axle gear rim','Lower elevation drive')) and tri.polygon_index%4==3:tone=0
   families.append(('steel' if o['family']=='wornsteel' else o['family'],tone))
   uvs.append([Vector((*uv.data[li].uv,0)) for li in tri.loops] if uv else None)
 bvh=BVHTree.FromPolygons(vertices,faces,all_triangles=True);a=np.array(vertices);xyz=np.column_stack((-a[:,1],a[:,0],a[:,2]));lo=np.floor(xyz.min(axis=0)*density-1).astype(int);hi=np.ceil(xyz.max(axis=0)*density+1).astype(int);size=hi-lo
 assert np.all((size>0)&(size<255)),size
 coords=[];indices=[];normals=[]
 for iz in range(int(size[2])):
  for iy in range(int(size[1])):
   for ix in range(int(size[0])):
    p=(lo+np.array((ix,iy,iz))+.5)/density;q=Vector((p[1],-p[0],p[2]));hit,normal,face,distance=bvh.find_nearest(q,.65/density)
    if hit is None:continue
    family,tone=families[face];rgb=np.array(colors[family][tone],dtype=float)
    if uvs[face] and wear_pixels:
     uv=barycentric_transform(hit,*(vertices[j] for j in faces[face]),*uvs[face]);x=max(0,min(63,int(uv.x*64)));y=max(0,min(63,int(uv.y*64)));rgb*=math.sqrt(wear_pixels[(y*64+x)*4])
    color=int(((palette-rgb)**2).sum(axis=1).argmin())+1
    coords.append((ix,iy,iz));indices.append(color);normals.append((-normal.y,normal.x,normal.z))
 np.savez_compressed(OUT/f'{part}.npz',coords=np.array(coords,dtype=np.uint8),colors=np.array(indices,dtype=np.uint8),normals=np.array(normals),grid_min=lo,grid_size=size,density=density,palette=palette)
 report['parts'][part]={'meshes':[o.name for o in selected],'grid_size':size.tolist(),'grid_min':lo.tolist(),'voxels':len(coords)};print('FLAK_VOXEL_PART',part,len(coords),flush=True)
(OUT/'geometry.json').write_text(json.dumps(report,indent=2)+'\n')
