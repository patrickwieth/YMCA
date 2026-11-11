"""Address all seven reference-review items, retaining the V10 rear and V11 finish.
No changes to game bindings; input blend and source artwork remain untouched.
"""
from pathlib import Path
import bpy,json,math,hashlib
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-12'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-10/sentry-v10-rear.blend'))
scene=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];changes=[]
camera_before=[list(row) for row in scene.camera.matrix_world]
pivot_before=list(pivot.location)
# The thicker muzzle reaches the old frame edge in one direction. Add 4 pixels
# on each side without shrinking/repositioning the artwork in world-pixel scale.
old_density=scene.render.resolution_y/scene.camera.data.ortho_scale
scene.render.resolution_x=scene.render.resolution_y=200
scene.camera.data.ortho_scale=200/old_density
assert abs(scene.render.resolution_y/scene.camera.data.ortho_scale-old_density)<1e-5
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   path=OUT/'textures'/Path(node.image.filepath).name
   if path.exists():node.image=bpy.data.images.load(str(path),check_existing=True);node.image.pack();node.interpolation='Closest'

def world_deform(obj,fn):
 matrix=obj.matrix_world.copy();inverse=matrix.inverted()
 for vertex in obj.data.vertices:
  point=matrix@vertex.co;fn(point);vertex.co=inverse@point
 obj.data.update()

def tune(mat,ambient,diffuse):
 for node in mat.node_tree.nodes:
  if node.type=='EMISSION':node.inputs['Strength'].default_value=ambient
  if node.type=='MIX_RGB' and node.blend_type=='MULTIPLY':node.inputs[2].default_value=(diffuse,diffuse,diffuse,1)

def planar(obj,material,axes,selector):
 mesh=obj.data;uv=mesh.uv_layers.active or mesh.uv_layers.new();idx=len(mesh.materials);mesh.materials.append(material)
 lo=[min(v.co[a] for v in mesh.vertices) for a in axes];hi=[max(v.co[a] for v in mesh.vertices) for a in axes]
 for face in mesh.polygons:
  if not selector(face):continue
  face.material_index=idx
  for li in face.loop_indices:
   p=mesh.vertices[mesh.loops[li].vertex_index].co
   uv.data[li].uv=[(p[a]-lo[i])/max(hi[i]-lo[i],1e-7) for i,a in enumerate(axes)]

# 1. The reference's gray cover is a projecting part, not a glass slot. Move the
# existing hidden cover ABOVE the solid frame; this fixes actual occlusion.
cover=bpy.data.objects['sensor_window_glass']
def raise_cover(p):p.x*=.30/.27;p.y-=.025;p.z+=.035
world_deform(cover,raise_cover)
planar(cover,bpy.data.materials['v6_cut_sensor-metal'],(0,2),lambda f:abs(f.normal.z)>.4)
changes.append('Front: raised/widened existing gray metal cover above the solid frame; source wear retained.')

# 2. More substantial tubes and collars, but no claim about the hidden barrel count.
for obj in list(scene.objects):
 if obj.type!='MESH':continue
 n=obj.name
 if n.startswith('separate_barrel_tube'):
  # Cylinders use local Z as their longitudinal axis.
  for v in obj.data.vertices:v.co.x*=1.20;v.co.y*=1.20
  obj.data.update()
 elif n.startswith('open_barrel_cluster_collar'):
  center=sum((v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
  for v in obj.data.vertices:
   delta=v.co-center;r=math.hypot(delta.x,delta.z)
   if r>.22:
    v.co.x=center.x+delta.x*(.245/.229);v.co.z=center.z+delta.z*(.245/.229)
   v.co.y=center.y+delta.y*(.11/.085)
  obj.data.update()
 elif n.startswith(('collar_dark_edge','octagonal_muzzle_surround','muzzle_outer_lip')):
  center=sum((v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
  for v in obj.data.vertices:
   delta=v.co-center;r=math.hypot(delta.x,delta.z)
   if r>.22:
    factor=(r+.016)/r;v.co.x=center.x+delta.x*factor;v.co.z=center.z+delta.z*factor
  obj.data.update()
 elif n.startswith('individual_muzzle_rim'):
  # Quieter small rims; the common muzzle surround should read first.
  for face in obj.data.polygons:
   mat=obj.data.materials[face.material_index]
   if mat.name=='v5_source_gun_edge':
    idx=len(obj.data.materials);obj.data.materials.append(bpy.data.materials['v5_source_gun_mid']);face.material_index=idx
for name,ambient,diffuse in [('v5_source_gun_edge',.60,.65),('v5_source_gun_mid',.50,.70),
                              ('v5_source_gun_shadow',.40,.75),('v6_cut_steel-tube',.60,.65)]:
 tune(bpy.data.materials[name],ambient,diffuse)
changes.append('Guns: 20% thicker tubes, stronger shared collars/muzzle surround, continuous source-gray metal, quieter tiny muzzle rings.')

# 3. Main roof becomes the dominant plate again. The longer rear body stays.
for name in ('roof_main_plate','roof_plate_underlay'):
 obj=bpy.data.objects[name]
 def hatch(p):
  p.x*=1.1282
  if p.y>.01:p.y+=.22
 world_deform(obj,hatch)
for obj in scene.objects:
 if obj.name.startswith('roof_fastener'):
  def fastener(p):
   p.x*=1.1282
   if p.y>.01:p.y+=.22
  world_deform(obj,fastener)
changes.append('Roof: main hatch widened 12.8% and extended rearwards 0.22 units; rear step remains but is less dominant.')

# 4 + 7. Actual narrow armor bevels provide a light facet next to the black ink.
# Keep broad source-textured faces and copied rivets rather than replacing them.
for obj in list(scene.objects):
 if not obj.name.startswith('side_thick_plate'):continue
 bpy.context.view_layer.objects.active=obj;obj.select_set(True)
 modifier=obj.modifiers.new('v12_armor_edge_facet','BEVEL');modifier.width=.035;modifier.segments=1;modifier.limit_method='ANGLE'
 bpy.ops.object.modifier_apply(modifier=modifier.name);obj.select_set(False)
 light=len(obj.data.materials);obj.data.materials.append(bpy.data.materials['edge'])
 dark=len(obj.data.materials);obj.data.materials.append(bpy.data.materials['ink'])
 for face in obj.data.polygons:
  if .1<abs(face.normal.x)<.99:
   if face.normal.z>.15:face.material_index=light
   elif face.normal.z<-.15:face.material_index=dark
changes.append('Armor: one-segment 0.035-unit bevel, light upper facets/dark lower return; existing worn front edge and rivets retained.')
changes.append('Wear: front orange blob replaced by a 16x3 directed source scratch plus a small corner chip; other V6/V8 wear preserved.')

# 6. Keep the fixed upper-left light, with a slightly stronger front component.
# The bearing gets a clearer bright-front/dark-side transition, not global contrast.
key=bpy.data.objects['reference_key_light'];key.rotation_euler=(-Vector((-2,-5,8))).to_track_quat('-Z','Y').to_euler()
# This source strip already carries the cylinder's dark-side paint. Preserve
# that albedo under the housing instead of darkening the entire red drum twice.
tune(bpy.data.materials['v6_cut_bearing'],.88,.65)
changes.append('Bearing/light: warmer painted front remains readable; stronger directional roundness, separate dark/bright metal rings.')
bpy.context.view_layer.update()
assert [list(row) for row in scene.camera.matrix_world]==camera_before
assert list(pivot.location)==pivot_before
point=world_to_camera_view(scene,scene.camera,pivot.matrix_world.translation)
(OUT/'attachment.json').write_text(json.dumps({'frame_size':[200,200],
 'bearing_pivot_screen':[point.x*200,(1-point.y)*200], 'added_canvas_padding':4,
 'world_pixel_density_unchanged':True,'note':'Preview only; gameplay facing calibration remains pending.'},indent=2)+'\n')
scene['study']='V12: all seven V11/reference review points addressed; final one-pixel ink via edge finish.'
scene.render.filepath=str(OUT/'render-000-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v12.blend'))

# Actual new-view albedo is used for source-palette material-family classification.
materials={s.material for o in scene.objects if o.type=='MESH' for s in o.material_slots if s.material}
def albedo_mode():
 restore=[]
 for mat in materials:
  if not mat.use_nodes:continue
  for node in mat.node_tree.nodes:
   if node.type=='EMISSION':
    s=node.inputs['Strength'];restore.append((mat,s,s.default_value,None));s.default_value=1
   elif node.type in ('BSDF_DIFFUSE','BSDF_GLOSSY'):
    s=node.inputs['Color'];link=s.links[0].from_socket if s.is_linked else None
    restore.append((mat,s,tuple(s.default_value),link))
    for edge in list(s.links):mat.node_tree.links.remove(edge)
    s.default_value=(0,0,0,1)
 return restore
for angle in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update()
 center=bpy.data.objects['bearing_red_drum'].matrix_world.translation
 assert abs(center.x)<1e-5 and abs(center.y-.12)<1e-5
 scene.render.filepath=str(OUT/f'render-{angle:03d}-raw.png');bpy.ops.render.render(write_still=True)
 restore=albedo_mode();scene.render.filepath=str(OUT/f'albedo-{angle:03d}.png');bpy.ops.render.render(write_still=True)
 for mat,s,value,link in restore:
  s.default_value=value
  if link:mat.node_tree.links.new(link,s)
pivot.rotation_euler.z=0;bpy.context.view_layer.update()
(OUT/'review-corrections.json').write_text(json.dumps({'changes':changes,'camera_pose_unchanged':True,'world_pixel_density_unchanged':True,'canvas_size':[200,200],'bearing_pivot_unchanged':True,
 'edge_finish':'V11 one-pixel edge pass follows this render; highlights kept inside the black edge',
 'hidden_barrel_count':'still inferred, not verified by single source view'},indent=2)+'\n')
print('SENTRY_V12_SEVEN_POINT_REVIEW_COMPLETE')
