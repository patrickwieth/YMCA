"""Render checked, exported voxel cells, not the smooth modeling mesh. No game changes."""
from pathlib import Path
import bpy,numpy as np,math,sys,argparse
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--all',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/gdi missile tower'/f'pass-{a.iteration:02d}';bpy.ops.wm.open_mainfile(filepath=str(OUT/'turret.blend'));s=bpy.context.scene;root=bpy.data.objects['turret_pivot'];root.rotation_euler.z=0
for o in s.objects:
 if o.type=='MESH':o.hide_render=True;o.hide_set(True)
v=np.load(OUT/'colored-voxels.npz');cells={tuple(map(int,c)):int(col) for c,col in zip(v['coords'],v['colors'])};lo=v['grid_min'];density=float(v['density']);materials=[]
def linear(c):
 c=float(c)/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
for i,rgb in enumerate(v['palette']):
 m=bpy.data.materials.new('source pixel index%03d'%i);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*[linear(x) for x in rgb],1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);materials.append(m)
verts=[];faces=[];colors=[]
sides=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
for c,color in cells.items():
 for direction,corners in sides:
  if tuple(c[i]+direction[i] for i in range(3)) in cells:continue
  start=len(verts)
  for corner in corners:
   p=(lo+np.array(c)+np.array(corner))/density;verts.append((float(p[1]),float(-p[0]),float(p[2])))
  faces.append((start,start+1,start+2,start+3));colors.append(color)
m=bpy.data.meshes.new('exported voxel surface');m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new('actual VXL cells / byte-roundtrip verified',m);s.collection.objects.link(o);o.parent=root
for mat in materials:m.materials.append(mat)
for face,color in zip(m.polygons,colors):face.material_index=color
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'voxel-preview.blend'))
for i in range(32 if a.all else 8):
 angle=i*(11.25 if a.all else 45);root.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update();s.render.filepath=str(OUT/f'voxel-{round(angle*100):05d}.png');bpy.ops.render.render(write_still=True)
print('REAL_VOXEL_REVIEW_COMPLETE',len(faces),flush=True)
