"""Render exported voxel cells, not source meshes. This is not a game screenshot."""
from pathlib import Path
import bpy,numpy as np,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet flak/voxel-01';bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art-source/soviet flak/pass-13/soviet-flak-model.blend'));s=bpy.context.scene;s.frame_set(0);pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0;rig=bpy.data.objects['barrel_recoil']
for o in list(s.objects):
 if o.type=='MESH':bpy.data.objects.remove(o,do_unlink=True)
sides=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
materials=[];palette=np.load(OUT/'base.npz')['palette']
for color in palette:
 for shade in range(8):
  m=bpy.data.materials.new('Voxel color');m.use_nodes=True;n=m.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial')
  def lin(x):return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
  em.inputs[0].default_value=(*[lin(float(x)/255)*(.55+.45*shade/7) for x in color],1);m.node_tree.links.new(em.outputs[0],out.inputs[0]);materials.append(m)
for part in ('base','turret','barrel'):
 data=np.load(OUT/f'{part}.npz');cells={tuple(map(int,c)):i for i,c in enumerate(data['coords'])};vs=[];fs=[];colors=[]
 for cell,index in cells.items():
  normal=data['normals'][index];light=np.array((.50,-.25,.85));shade=int(round(max(0,float(normal@light/np.linalg.norm(light)))*7));color=(int(data['colors'][index])-1)*8+shade
  for direction,corners in sides:
   if tuple(cell[i]+direction[i] for i in range(3)) in cells:continue
   start=len(vs)
   for corner in corners:
    p=(data['grid_min']+np.array(cell)+np.array(corner))/float(data['density']);vs.append((p[1],-p[0],p[2]))
   fs.append((start,start+1,start+2,start+3));colors.append(color)
 mesh=bpy.data.meshes.new('Exported voxel '+part);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new('Exported voxel '+part,mesh);s.collection.objects.link(o)
 if part!='base':o.parent=rig if part=='barrel' else pivot;o.location=-pivot.location
 for mat in materials:mesh.materials.append(mat)
 for f,color in zip(mesh.polygons,colors):f.material_index=color
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'voxel-preview.blend'))
for facing in (0,8,16,24):
 pivot.rotation_euler.z=facing*math.pi/16
 for frame in (0,1,4,8,10):
  s.frame_set(frame);s.render.filepath=str(OUT/f'voxel-{facing:02d}-{frame:02d}.png');bpy.ops.render.render(write_still=True)
print('FLAK_VOXEL_PREVIEW_RENDERED')
