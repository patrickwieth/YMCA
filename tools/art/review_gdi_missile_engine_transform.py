"""Render real exported voxel slices using matrices exported by OpenRA's actual Voxel class.
Background Blender only. Not a live OpenRA GPU test. Preserve previous study scenes.
"""
from pathlib import Path
import bpy,sys,json,numpy as np
from mathutils import Matrix,Vector
R=Path(__file__).resolve().parents[2];O=R/'art-source/gdi missile tower/pass-14'
bpy.ops.wm.open_mainfile(filepath=str(O/'turret.blend'));s=bpy.context.scene
# Make the editable model's packed donor pixels and UV density agree with the exported cells.
for image in bpy.data.images:
 p=O/'source-cuts'/Path(image.filepath).name
 if not p.exists():continue
 if image.packed_file:image.unpack(method='REMOVE')
 image.filepath=str(p);image.reload();image.pack()
for ob in s.objects:
 if ob.type!='MESH':continue
 m=ob.data
 for f in m.polygons:
  role=m.materials[f.material_index].name.split('CUT / ',1)[-1]
  factor={'gold':(4/9,10/8),'goldshade':(4/5,6/14)}.get(role)
  if factor:
   for i in f.loop_indices:
    uv=m.uv_layers.active.data[i].uv;uv.x*=factor[0];uv.y*=factor[1]
bpy.ops.wm.save_as_mainfile(filepath=str(O/'turret.blend'))
for ob in list(s.objects):bpy.data.objects.remove(ob,do_unlink=True)
s.render.resolution_x=s.render.resolution_y=224;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(0,0,1000));cam=bpy.context.object;cam.rotation_euler=(0,0,0);cam.data.type='ORTHO';cam.data.ortho_scale=224;s.camera=cam
p=json.loads((O/'engine-projection.json').read_text());transform=np.array(p['transform']).reshape(4,4).T
v=np.load(O/'colored-voxels.npz');cells={tuple(map(int,c)):int(col) for c,col in zip(v['coords'],v['colors'])}
materials=[]
def linear(c):
 c=float(c)/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
for i,rgb in enumerate(v['palette']):
 m=bpy.data.materials.new('exact source RGB %03d'%i);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*[linear(c) for c in rgb],1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);materials.append(m)
sides=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
verts=[];faces=[];colors=[]
for c,color in cells.items():
 for direction,corners in sides:
  if tuple(c[i]+direction[i] for i in range(3)) in cells:continue
  start=len(verts)
  for corner in corners:
   q=transform@np.array([c[0]+corner[0],c[1]+corner[1],c[2]+corner[2],1]);verts.append(tuple(q[:3]))
  faces.append(tuple(range(start,start+4)));colors.append(color)
m=bpy.data.meshes.new('OpenRA raw slice planes transformed by Voxel.TransformationMatrix');m.from_pydata(verts,[],faces);m.update();ob=bpy.data.objects.new('actual exported cells',m);s.collection.objects.link(ob)
for mat in materials:m.materials.append(mat)
for face,col in zip(m.polygons,colors):face.material_index=col
for i,basis in enumerate(p['matrices']):
 a=np.array(basis).T*p['scale'];a[1]*=-1;mat=np.eye(4);mat[:3,:3]=a;mat[0,3]=p['mount'][0];mat[1,3]=-(80+p['mount'][1]);ob.matrix_world=Matrix(mat.tolist());s.render.filepath=str(O/f'engine-{i:02d}.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'engine-voxel-review.blend'))
