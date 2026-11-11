"""Blender: sample V13 mesh surfaces into a real voxel lattice, preserving source.
Only geometry/projection data here; color assignment and VXL packing are separate.
"""
from pathlib import Path
import bpy,numpy as np,json,math,hashlib
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet-sentry/voxel-01';OUT.mkdir(parents=True,exist_ok=True)
source=ROOT/'art-source/soviet-sentry/game-v13-32-native/render.blend'
bpy.ops.wm.open_mainfile(filepath=str(source));s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0;bpy.context.view_layer.update()
origin=pivot.matrix_world.translation.copy();vertices=[];faces=[];objects=[]
deps=bpy.context.evaluated_depsgraph_get()
for obj in s.objects:
 if obj.type!='MESH' or obj.hide_render:continue
 evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles();start=len(vertices)
 vertices.extend([evaluated.matrix_world@v.co-origin for v in mesh.vertices]);faces.extend([tuple(start+i for i in f.vertices) for f in mesh.loop_triangles]);objects.append(obj.name)
 evaluated.to_mesh_clear()
bvh=BVHTree.FromPolygons(vertices,faces,all_triangles=True)
a=np.array(vertices);file_coords=np.column_stack((-a[:,1],a[:,0],a[:,2]));density=18
lo=np.floor(file_coords.min(axis=0)*density-1).astype(int);hi=np.ceil(file_coords.max(axis=0)*density+1).astype(int);size=hi-lo
assert np.all((size>0)&(size<255)),size
coords=[];points=[];normals=[]
for iz in range(int(size[2])):
 for iy in range(int(size[1])):
  for ix in range(int(size[0])):
   p=(lo+np.array((ix,iy,iz))+.5)/density
   q=Vector((p[1],-p[0],p[2]))
   hit,normal,face,distance=bvh.find_nearest(q,.72/density)
   if hit is None:continue
   coords.append((ix,iy,iz));points.append(tuple(hit));normals.append(tuple(normal))
 print('VOXEL_Z',iz,'count',len(coords),flush=True)
# Project original geometry points into the 32 matching albedo renders.
projections=[];views=[]
projection=s.camera.calc_matrix_camera(deps,x=120,y=120)@s.camera.matrix_world.inverted()
view=(s.camera.location-origin).normalized()
for index in range(32):
 rot=Matrix.Rotation(math.radians(index*11.25),4,'Z');m=projection@Matrix.Translation(origin)@rot
 projections.append([[60*m[0][j]+(60 if j==3 else 0) for j in range(4)],[-60*m[1][j]+(60 if j==3 else 0) for j in range(4)]])
 views.append(tuple(rot.to_3x3().transposed()@view))
np.savez_compressed(OUT/'surface.npz',coords=np.array(coords,dtype=np.uint8),points=np.array(points,dtype=np.float32),normals=np.array(normals,dtype=np.float32),projections=np.array(projections),views=np.array(views),grid_min=lo,grid_size=size,density=density)
(OUT/'geometry.json').write_text(json.dumps({'source_blend':str(source.relative_to(ROOT)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'voxel_density':density,'grid_size':size.tolist(),'grid_min':lo.tolist(),'surface_voxels':len(coords),'triangles':len(faces),'mesh_objects':objects,'coordinate_mapping':'VXL x=-Blender y, y=Blender x, z=Blender z, all relative to turret pivot','method':'nearest evaluated mesh surface within 0.72 voxel; hollow shell, no procedural texture/noise'},indent=2)+'\n')
print('SENTRY_VOXEL_GEOMETRY_COMPLETE',len(coords))
