"""Blender 3.6: V5 gun readability, plate rims, bearing UV and rotation spot checks."""
from pathlib import Path
import math
import json
import bpy
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-05'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-04/sentry-v4.blend'))
scene=bpy.context.scene;pivot=bpy.data.objects['turret_pivot']
scene.render.resolution_x=scene.render.resolution_y=192
scene.render.filter_size=.01;scene.render.dither_intensity=0
scene.eevee.use_gtao=False;scene.eevee.taa_render_samples=1
# Repoint source-cut materials to this iteration, preserving packed portability.
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   name=Path(node.image.filepath).name;path=OUT/'textures'/name
   if path.exists():
    node.image=bpy.data.images.load(str(path),check_existing=True);node.image.pack();node.interpolation='Closest'

def linear(x):
 x/=255
 return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4

def flat(name,rgb):
 mat=bpy.data.materials.new(name);mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear()
 emit=nodes.new('ShaderNodeEmission');emit.inputs[0].default_value=(*map(linear,rgb),1)
 out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(emit.outputs[0],out.inputs[0]);return mat
ink=flat('v5_source_outline',(3,2,1))
shadow=flat('v5_source_gun_shadow',(42,38,39))
mid=flat('v5_source_gun_mid',(85,81,82))
shine=flat('v5_source_gun_edge',(121,113,105))
key=Vector((-.6,-.8,1.4)).normalized()

def shade(obj,materials):
 obj.data.materials.clear()
 for mat in materials:obj.data.materials.append(mat)
 bpy.context.view_layer.update()
 for face in obj.data.polygons:
  normal=(obj.matrix_world.to_3x3()@face.normal).normalized();dot=normal.dot(key)
  face.material_index=2 if dot>.45 else 1 if dot>-.1 else 0

def rod(name,a,b,radius=.015,mat=ink):
 a,b=Vector(a),Vector(b)
 bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius,depth=(b-a).length,location=(a+b)/2)
 obj=bpy.context.object;obj.name=name;obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();obj.parent=pivot
 obj.data.materials.append(mat);return obj

def ring(name,x,y,z,outer,inner,depth,materials,n=16):
 verts=[]
 for dy,r in [(-depth/2,outer),(depth/2,outer),(-depth/2,inner),(depth/2,inner)]:
  for i in range(n):
   a=i*math.tau/n;verts.append((x+r*math.cos(a),y+dy,z+r*math.sin(a)))
 faces=[]
 for i in range(n):
  j=(i+1)%n;faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),
                        (n+i,n+j,3*n+j,3*n+i),(2*n+i,3*n+i,3*n+j,2*n+j)])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj);obj.parent=pivot
 shade(obj,materials);return obj

for obj in list(scene.objects):
 if obj.type!='MESH':continue
 n=obj.name
 if n.startswith('separate_barrel_tube'):
  # Dark grooves and continuous bright tube faces, not a uniformly gray bundle.
  shade(obj,[shadow,mid,shine])
 elif n.startswith(('recessed_muzzle_bore','central_spindle','side_plate_outline')):
  obj.data.materials.clear();obj.data.materials.append(ink)
  for face in obj.data.polygons:face.material_index=0
 elif n.startswith('individual_muzzle_rim'):
  shade(obj,[ink,mid,shine])
 elif n.startswith('open_barrel_cluster_collar'):
  shade(obj,[ink,mid,shine])
 elif n.startswith('collar_fastener'):
  # Reduce the noisy ring of tiny bolts; retain only the upper mechanical lug.
  obj.hide_render=obj.location.z<1.30
 elif n.startswith('drum_segment_seam'):
  index=0 if '.' not in n else int(n.rsplit('.',1)[1]);obj.hide_render=index%2==1
 elif n.startswith('shoulder_rail'):
  # Solid edge lines follow the actual rail profile, not a 2D post-process contour.
  points=[obj.matrix_world@v.co for v in obj.data.vertices]
  # Sloping long boundaries, outer and inner; cap lines omitted to avoid doubled seams.
  for start,end in [(0,1),(4,5)]:
   a,b=points[start].copy(),points[end].copy();a.z+=.006;b.z+=.006
   rod('shoulder_separation_line',a,b,.014)
 elif n.startswith('roof_plate_underlay'):
  obj.data.materials.clear();obj.data.materials.append(ink)
  for face in obj.data.polygons:face.material_index=0

# Thin dark front/rear lips on each shared barrel collar give three clear bands.
for sign in (-1,1):
 x=sign*.43
 for y in (-1.38,-1.95,-2.40):
  for dy in (-.055,.055):ring('collar_dark_edge',x,y+dy,1.15,.235,.184,.018,[ink,shadow,mid])
 # A two-tier socket/bezel around each weapon, separate from the red receiver.
 ring('receiver_socket_bezel',x,-1.155,1.15,.272,.237,.03,[ink,shadow,mid])

# The reference has a common muzzle surround, not six exposed flower-like tube ends.
for sign in (-1,1):
 x=sign*.43
 ring('octagonal_muzzle_surround',x,-2.650,1.15,.238,.182,.065,[shadow,mid,shine],8)
 ring('muzzle_outer_lip',x,-2.688,1.15,.242,.218,.014,[ink,mid,shine],8)

# Reconstructed rear service doors: functional geometric detail, source-cut surfaces.
# Hidden topology is inferred; do not claim these doors are visible in the reference.
def box(name,pos,size,mat):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
 obj=bpy.context.object;obj.name=name;obj.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 obj.parent=pivot;obj.data.materials.append(mat);return obj
box('rear_service_shadow',(0,.694,1.31),(1.34,.04,.94),ink)
for sign in (-1,1):
 x=sign*.335
 box('rear_service_frame',(x,.726,1.31),(.63,.035,.89),mid)
 box('rear_service_inset',(x,.751,1.31),(.53,.022,.77),shadow)
 # Upper grille slats, deliberately sparse enough to read at sprite resolution.
 for z in (1.42,1.52,1.62):
  box('rear_service_grille',(x,.771,z),(.45,.025,.034),mid)
 box('rear_service_handle',(x+sign*.19,.787,1.15),(.035,.035,.12),shine)
 for z in (.94,1.68):box('rear_service_fastener',(x+sign*.23,.779,z),(.032,.018,.032),shine)
# A narrow copied plate section in the lower door, without tiling procedural scratches.
rear_mat=bpy.data.materials.new('rear_source_cut');rear_mat.use_nodes=True
nodes=rear_mat.node_tree.nodes;nodes.clear()
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures/rear-vent-face.png'));tex.image.pack();tex.interpolation='Closest'
emit=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
rear_mat.node_tree.links.new(tex.outputs['Color'],emit.inputs[0]);rear_mat.node_tree.links.new(emit.outputs[0],out.inputs[0])
for sign in (-1,1):
 x=sign*.335
 verts=[(x-.20,.767,.99),(x+.20,.767,.99),(x+.20,.767,1.32),(x-.20,.767,1.32)]
 mesh=bpy.data.meshes.new('rear_door_source_cut');mesh.from_pydata(verts,[],[(0,1,2,3)]);mesh.uv_layers.new()
 for loop,uv in zip(mesh.uv_layers.active.data,[(0,0),(1,0),(1,1),(0,1)]):loop.uv=uv
 obj=bpy.data.objects.new('rear_door_source_cut',mesh);scene.collection.objects.link(obj);obj.parent=pivot;mesh.materials.append(rear_mat)

# Material for an entire continuous drum circumference, without repeating one scratch per face.
mat=bpy.data.materials.new('v5_drum_circumference');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear()
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures/drum-circumference.png'));tex.image.pack();tex.interpolation='Closest';tex.extension='REPEAT'
emit=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
mat.node_tree.links.new(tex.outputs['Color'],emit.inputs[0]);mat.node_tree.links.new(emit.outputs[0],out.inputs[0])
obj=bpy.data.objects['bearing_red_drum'];idx=len(obj.data.materials);obj.data.materials.append(mat)
for face in obj.data.polygons:
 if abs(face.normal.z)>.5:continue
 face.material_index=idx
 points=[obj.data.vertices[obj.data.loops[li].vertex_index].co for li in face.loop_indices]
 us=[math.atan2(p.y,p.x)/math.tau%1 for p in points]
 if max(us)-min(us)>.5:us=[u+1 if u<.5 else u for u in us]
 for li,p,u in zip(face.loop_indices,points,us):obj.data.uv_layers.active.data[li].uv=(u,(p.z+.185)/.37)
scene['study']='V5: outlined open collars, readable barrel tubes, separate sockets, plate edge geometry and continuous source-cut drum UV.'
pivot.rotation_euler.z=0
# Put rotation exactly at the bearing center; preserve all objects in world space.
# Earlier prototypes had a 0.12-unit offset between bearing and pivot.
for sign,label in [(-1,'left'),(1,'right')]:
 marker=bpy.data.objects.new('muzzle_'+label,None);scene.collection.objects.link(marker)
 marker.location=(sign*.43,-2.688,1.15);marker.parent=pivot
bpy.context.view_layer.update()
world_matrices={obj:obj.matrix_world.copy() for obj in pivot.children}
pivot.location=(0,.12,0);bpy.context.view_layer.update()
for obj,matrix in world_matrices.items():obj.matrix_world=matrix
bpy.context.view_layer.update()
point=world_to_camera_view(scene,scene.camera,pivot.matrix_world.translation)
(OUT/'attachment.json').write_text(json.dumps({'frame_size':[192,192],
 'bearing_pivot_screen':[point.x*192,(1-point.y)*192],
 'model_rotation_axis':'world Z through bearing center',
 'muzzles':['muzzle_left','muzzle_right'],
 'note':'Preview attachment only; OpenRA facing order still needs calibration.'},indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v5.blend'))
# Still only a few diagnostic views, not a final animation/export.
for degrees in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 bearing=bpy.data.objects['bearing_red_drum'].matrix_world.translation
 assert abs(bearing.x)<1e-5 and abs(bearing.y-.12)<1e-5, 'Bearing wobbles around pivot'
 scene.render.filepath=str(OUT/f'render-{degrees:03d}-raw.png');bpy.ops.render.render(write_still=True)
pivot.rotation_euler.z=0
print('SENTRY_V5_COMPLETE')
