"""Render fixed geometry using exported engine transforms; do not touch editable model."""
from pathlib import Path
import bpy,sys,json,numpy as np,argparse
from mathutils import Matrix
parser=argparse.ArgumentParser();parser.add_argument('--pass',dest='iteration',type=int,required=True);a=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
R=Path(__file__).resolve().parents[2];O=R/'art-source/gdi missile tower'/f'pass-{a.iteration:02d}'
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=224;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1;s.eevee.use_gtao=False;s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
bpy.ops.object.camera_add(location=(0,0,1000));cam=bpy.context.object;cam.rotation_euler=(0,0,0);cam.data.type='ORTHO';cam.data.ortho_scale=224;s.camera=cam
p=json.loads((O/'engine-projection.json').read_text());transform=np.array(p['transform']).reshape(4,4).T
v=np.load(O/'colored-voxels.npz');cells={tuple(map(int,c)):int(col) for c,col in zip(v['coords'],v['colors'])}
def linear(c):
 c=float(c)/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
materials=[]
for i,rgb in enumerate(v['palette']):
 m=bpy.data.materials.new('source RGB %03d'%i);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*[linear(c) for c in rgb],1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);materials.append(m)
sides=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
verts=[];faces=[];colors=[]
for c,color in cells.items():
 for direction,corners in sides:
  if tuple(c[i]+direction[i] for i in range(3)) in cells:continue
  start=len(verts)
  for corner in corners:
   q=transform@np.array([c[0]+corner[0],c[1]+corner[1],c[2]+corner[2],1]);verts.append(tuple(q[:3]))
  faces.append(tuple(range(start,start+4)));colors.append(color)
m=bpy.data.meshes.new('Unchanged exported voxel surfaces');m.from_pydata(verts,[],faces);m.update();ob=bpy.data.objects.new('actual exported cells',m);s.collection.objects.link(ob)
for mat in materials:m.materials.append(mat)
for face,col in zip(m.polygons,colors):face.material_index=col
for i,basis in enumerate(p['matrices']):
 b=np.array(basis).T*p['scale'];b[1]*=-1;mat=np.eye(4);mat[:3,:3]=b;mat[0,3]=p['mount'][0];mat[1,3]=-(80+p['mount'][1]);ob.matrix_world=Matrix(mat.tolist());s.render.filepath=str(O/f'engine-{i:02d}.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'engine-voxel-review.blend'))
