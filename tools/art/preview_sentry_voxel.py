"""Blender inspection of actual exported voxel cells (not an engine screenshot)."""
from pathlib import Path
import bpy,numpy as np,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet-sentry/voxel-01'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art-source/soviet-sentry/game-v13-32-native/render.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
for o in list(s.objects):
 if o.type=='MESH':o.hide_render=True;o.hide_set(True)
v=np.load(OUT/'colored-voxels.npz');cells={tuple(map(int,c)):int(color) for c,color in zip(v['coords'],v['colors'])};lo=v['grid_min'];density=int(v['density'])
materials=[]
for i,rgb in enumerate(v['palette']):
 m=bpy.data.materials.new(f'voxel source palette {i}');m.use_nodes=True;nodes=m.node_tree.nodes;nodes.clear();e=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
 def linear(x):
  x=float(x)/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
 e.inputs[0].default_value=(*[linear(x) for x in rgb],1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);materials.append(m)
verts=[];faces=[];colors=[]
sides=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
for c,color in cells.items():
 for direction,corners in sides:
  if tuple(c[i]+direction[i] for i in range(3)) in cells:continue
  start=len(verts)
  for corner in corners:
   p=(lo+np.array(c)+np.array(corner))/density;verts.append((float(p[1]),float(-p[0]),float(p[2])))
  faces.append((start,start+1,start+2,start+3));colors.append(color)
mesh=bpy.data.meshes.new('actual voxel cells');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('voxel surface',mesh);s.collection.objects.link(o);o.parent=pivot
for m in materials:mesh.materials.append(m)
for f,color in zip(mesh.polygons,colors):f.material_index=color
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'voxel-study.blend'))
for angle in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update();s.render.filepath=str(OUT/f'voxel-preview-{angle:03d}.png');bpy.ops.render.render(write_still=True)
print('VOXEL_PREVIEW_COMPLETE',len(faces))
