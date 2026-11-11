"""Blender: export existing V12 control/V13 model at unchanged native density.
240px canvas adds 20px padding per side; original blend files are never saved over.
"""
from pathlib import Path
import bpy,json,math,argparse,sys
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
p=argparse.ArgumentParser();p.add_argument('--version',type=int,choices=(12,13),required=True)
p.add_argument('--native-32',action='store_true',help='Render directly at 120px, 32 directions; never downsample the turret.')
a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/(f'game-v{a.version}-32-native' if a.native_32 else f'game-v{a.version}-8');OUT.mkdir(parents=True,exist_ok=True)
size=120 if a.native_32 else 240
bpy.ops.wm.open_mainfile(filepath=str(BASE/f'attempt-{a.version}/sentry-v{a.version}.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
assert s.render.resolution_x==s.render.resolution_y==200
s.camera.data.ortho_scale*=240/200;s.render.resolution_x=s.render.resolution_y=size
s.render.resolution_percentage=100
bpy.context.view_layer.update()
def project(point):
 v=world_to_camera_view(s,s.camera,point);return [v.x*size,(1-v.y)*size]
attachment={'frame_size':[size,size],'bearing_pivot_screen':project(pivot.matrix_world.translation),
 'world_pixel_density_unchanged':not a.native_32,'native_render_scale':size/240,'added_padding_per_side':size/12,'source_version':a.version}
(OUT/'attachment.json').write_text(json.dumps(attachment,indent=2)+'\n')
s.render.filepath=str(OUT/'render-000-raw.png');bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'render.blend'))
materials={slot.material for o in s.objects if o.type=='MESH' for slot in o.material_slots if slot.material}
def albedo():
 restore=[]
 for m in materials:
  if not m.use_nodes:continue
  for n in m.node_tree.nodes:
   if n.type=='EMISSION':
    v=n.inputs['Strength'];restore.append((m,v,v.default_value,None));v.default_value=1
   elif n.type in ('BSDF_DIFFUSE','BSDF_GLOSSY'):
    v=n.inputs['Color'];link=v.links[0].from_socket if v.is_linked else None
    restore.append((m,v,tuple(v.default_value),link))
    for edge in list(v.links):m.node_tree.links.remove(edge)
    v.default_value=(0,0,0,1)
 return restore
angles=range(32) if a.native_32 else range(0,360,45);directions=[]
for angle in angles:
 degrees=angle*11.25 if a.native_32 else angle
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 start=project(pivot.matrix_world@Vector((0,0,1.15)));end=project(pivot.matrix_world@Vector((0,-2,1.15)))
 directions.append({'angle':angle,'blender_degrees':degrees,'gun_screen_vector':[end[i]-start[i] for i in (0,1)]})
 s.render.filepath=str(OUT/f'render-{angle:03d}-raw.png');bpy.ops.render.render(write_still=True)
 restore=albedo();s.render.filepath=str(OUT/f'albedo-{angle:03d}.png');bpy.ops.render.render(write_still=True)
 for m,v,value,link in restore:
  v.default_value=value
  if link:m.node_tree.links.new(link,v)
(OUT/'projected-directions.json').write_text(json.dumps(directions,indent=2)+'\n')
if a.version==13:
 prefixes=('separate_barrel_tube','individual_muzzle_rim','recessed_muzzle_bore','open_barrel_cluster_collar',
  'collar_fastener','collar_dark_edge','octagonal_muzzle_surround','muzzle_outer_lip','central_spindle',
  'gun_socket_rim','gun_socket_steel','receiver_socket_bezel')
 def role(o):
  if o.name.startswith('recessed_muzzle_bore'):return 2
  if o.name.startswith('central_spindle'):return 3
  return 1 if o.name.startswith(prefixes) else 0
 mats={}
 for value in range(4):
  m=bpy.data.materials.new(f'weapon-mask-{value}');m.use_nodes=True;n=m.node_tree.nodes;n.clear()
  em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');em.inputs[0].default_value=(value/255/12.92,0,0,1)
  m.node_tree.links.new(em.outputs[0],out.inputs[0]);mats[value]=m
 for o in s.objects:
  if o.type!='MESH':continue
  o.data.materials.clear();o.data.materials.append(mats[role(o)])
  for face in o.data.polygons:face.material_index=0
 for angle in angles:
  pivot.rotation_euler.z=math.radians(angle*11.25 if a.native_32 else angle);bpy.context.view_layer.update()
  s.render.filepath=str(OUT/f'weapon-mask-{angle:03d}.png');bpy.ops.render.render(write_still=True)
print('FULL_ROTATION_RENDER_COMPLETE',a.version)
