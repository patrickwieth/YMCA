"""Sample actual triangulated surfaces and native cut UVs, never camera-projected colors.
Run in background Blender. Packing/source-pixel lookup is done outside Blender.
"""
from pathlib import Path
import bpy,numpy as np,json,math,sys,argparse
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/gdi missile tower'/f'pass-{a.iteration:02d}';bpy.ops.wm.open_mainfile(filepath=str(OUT/'turret.blend'));bpy.data.objects['turret_pivot'].rotation_euler.z=0;bpy.context.view_layer.update()
verts=[];faces=[];uvs=[];roles=[];textures=[];deps=bpy.context.evaluated_depsgraph_get()
for o in bpy.context.scene.objects:
 if o.type!='MESH' or o.hide_render:continue
 obj=o.evaluated_get(deps);m=obj.to_mesh();m.calc_loop_triangles();start=len(verts);verts.extend([obj.matrix_world@v.co for v in m.vertices])
 for t in m.loop_triangles:
  faces.append(tuple(start+i for i in t.vertices));uvs.append([Vector((*m.uv_layers.active.data[i].uv,0)) for i in t.loops]);name=m.materials[t.material_index].name.split('CUT / ',1)[1]
  if name not in textures:textures.append(name)
  roles.append(textures.index(name))
 obj.to_mesh_clear()
bvh=BVHTree.FromPolygons(verts,faces,all_triangles=True);xyz=np.array(verts);density=1.25;file_xyz=np.column_stack((-xyz[:,1],xyz[:,0],xyz[:,2]));lo=np.floor(file_xyz.min(axis=0)*density-1).astype(int);hi=np.ceil(file_xyz.max(axis=0)*density+1).astype(int);size=hi-lo;assert np.all(size<255),size
coords=[];normals=[];samples=[];ids=[]
for z in range(int(size[2])):
 for y in range(int(size[1])):
  for x in range(int(size[0])):
   fx=(lo[0]+x+.5)/density;fy=(lo[1]+y+.5)/density;fz=(lo[2]+z+.5)/density;q=Vector((fy,-fx,fz));hit,normal,face,d=bvh.find_nearest(q,.69/density)
   if hit is None:continue
   triangle=faces[face];uv=barycentric_transform(hit,*[verts[i] for i in triangle],*uvs[face]);coords.append((x,y,z));normals.append(tuple(normal));samples.append((uv.x,uv.y));ids.append(roles[face])
 if z%10==0:print('VOXEL_LAYER',z,'of',size[2],len(coords),flush=True)
np.savez_compressed(OUT/'surface-uv.npz',coords=np.array(coords,dtype=np.uint8),normals=np.array(normals,dtype=np.float32),uv=np.array(samples,dtype=np.float32),texture=np.array(ids,dtype=np.uint8),grid_min=lo,grid_size=size,density=density)
(OUT/'voxel-sampling.json').write_text(json.dumps({'textures':textures,'triangles':len(faces),'voxels':len(coords),'density':density,'grid_size':size.tolist(),'method':'nearest true mesh surface and barycentric face UV; native source cuts only; no projection/fallback/invented colors'},indent=2)+'\n')
print('VOXEL_SAMPLING_COMPLETE',len(coords),flush=True)
