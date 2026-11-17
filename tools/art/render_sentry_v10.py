"""Extend only the truncated rear of V9; retain its light, gun/body front and pivot."""
from pathlib import Path
import bpy,json,hashlib,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-10'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-09/sentry-v9-lighting.blend'))
scene=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];EXTENSION=.38

def signature(obj):
 data=[obj.name,[list(row) for row in obj.matrix_world],obj.hide_render]
 if obj.type=='MESH':data.extend(([list(v.co) for v in obj.data.vertices],[list(f.vertices) for f in obj.data.polygons]))
 if obj.type=='CAMERA':data.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
allowed=('sloped_main_housing','rear_roof_ledge','rear_side_equipment','rear_vent_rib','rear_service_','rear_door_source_cut')
protected={o.name:signature(o) for o in scene.objects if not o.name.startswith(allowed)}
changes=[]
# Repack existing original cuts in the new attempt without altering them.
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   path=OUT/'textures'/Path(node.image.filepath).name
   if path.exists():node.image=bpy.data.images.load(str(path),check_existing=True);node.image.pack()

def deform_world(obj,change):
 matrix=obj.matrix_world.copy();inverse=matrix.inverted()
 for vertex in obj.data.vertices:
  p=matrix@vertex.co;change(p);vertex.co=inverse@p
 obj.data.update()
for obj in list(scene.objects):
 name=obj.name
 if name=='sloped_main_housing':
  # Front, weapon receiver and shoulder slopes are untouched; move rear vertices only.
  def extend(p):
   if p.y>.40:p.y+=EXTENSION
  deform_world(obj,extend);changes.append({'object':name,'edit':'rear vertices only','dy':EXTENSION})
 elif name.startswith('rear_roof_ledge'):
  # Reveal a proper rear step instead of burying most of this ledge under the hatch.
  def roof(p):
   p.y+=.12
   if p.y>.64:p.y+=.30
  deform_world(obj,roof);changes.append({'object':name,'edit':'rear step starts behind main hatch; wider overhang'})
 elif name.startswith('rear_side_equipment'):
  def stretch(p):
   if p.y>.74:p.y+=EXTENSION
  deform_world(obj,stretch);changes.append({'object':name,'edit':'extend vent backing; front edge retained'})
 elif name.startswith('rear_vent_rib'):
  obj.hide_render=True;changes.append({'object':name,'edit':'replace uniform five-rib stack with upper grille/lower cover'})
 elif name.startswith(('rear_service_','rear_door_source_cut')):
  obj.location.y+=EXTENSION;changes.append({'object':name,'edit':'move inferred rear service doors with rear wall','dy':EXTENSION})
bpy.context.view_layer.update()

# Materials inherit V9's real-lighting graph. Copied gray side cuts already have
# source shading, so use its compensated side material rather than double-darkening.
new_materials={}
def cut_material(name,template='cutpaste_side-metal'):
 if name in new_materials:return new_materials[name]
 mat=bpy.data.materials[template].copy();mat.name='v10_source_'+name
 images=[n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE']
 assert images,(name,template)
 for tex in images:
  tex.image=bpy.data.images.load(str(OUT/'textures'/(name+'.png')),check_existing=True);tex.image.pack();tex.interpolation='Closest';tex.extension='EXTEND'
 new_materials[name]=mat;return mat

def planar(obj,mat,axes,selector=lambda f:True,flip=(False,False)):
 mesh=obj.data;idx=len(mesh.materials);mesh.materials.append(mat);uv=mesh.uv_layers.active or mesh.uv_layers.new()
 lo=[min(v.co[a] for v in mesh.vertices) for a in axes];hi=[max(v.co[a] for v in mesh.vertices) for a in axes]
 for face in mesh.polygons:
  if not selector(face):continue
  face.material_index=idx
  for li in face.loop_indices:
   p=mesh.vertices[mesh.loops[li].vertex_index].co
   values=[(p[a]-lo[i])/max(hi[i]-lo[i],1e-7) for i,a in enumerate(axes)]
   uv.data[li].uv=[1-v if flip[i] else v for i,v in enumerate(values)]
planar(bpy.data.objects['rear_roof_ledge'],cut_material('rear-roof-extension','v6_cut_rear-roof'),(1,0),lambda f:f.normal.z>.8,(False,True))

# World-space helper keeps the existing offset bearing pivot intact.
def box(name,pos,size,mat,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);obj=bpy.context.object;obj.name=name;obj.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  modifier=obj.modifiers.new('rear_plate_chamfer','BEVEL');modifier.width=bevel;modifier.segments=1
  bpy.ops.object.modifier_apply(modifier=modifier.name)
 matrix=obj.matrix_world.copy();obj.parent=pivot;obj.matrix_world=matrix
 obj.data.materials.append(bpy.data.materials[mat]);return obj
for sign in (-1,1):
 # Upper two-slit grille and separate lower service cover, as visible in the source.
 x=sign*1.005
 box('v10_rear_grille_recess',(x,.94,1.69),(.035,.55,.25),'ink',.012)
 grille=box('v10_rear_grille_face',(x+sign*.022,.94,1.69),(.012,.49,.19),'dark')
 planar(grille,cut_material('rear-upper-grille'),(1,2),lambda f:abs(f.normal.x)>.9)
 for z in (1.64,1.74):
  box('v10_rear_grille_metal_lip',(x+sign*.032,.94,z),(.018,.49,.018),'shade')
 box('v10_rear_cover_recess',(x,.94,1.30),(.03,.55,.42),'ink',.012)
 cover=box('v10_rear_lower_cover',(x+sign*.024,.94,1.30),(.025,.49,.35),'armor',.012)
 planar(cover,cut_material('rear-lower-cover'),(1,2),lambda f:abs(f.normal.x)>.9)
 # Narrow red rear return and a dark upper gasket, not the background fence pole.
 box('v10_rear_red_return',(x,1.21,1.46),(.045,.045,.88),'red',.008)
 box('v10_rear_top_gasket',(sign*.87,.97,1.89),(.28,.56,.035),'ink')

# Contact shading stays local; do not globally darken V9's already shaded art.
scene.eevee.gtao_distance=.24;scene.eevee.gtao_factor=1.5
bpy.data.lights['reference_key_light'].contact_shadow_distance=.30
bearing=bpy.data.materials['v6_cut_bearing']
for node in bearing.node_tree.nodes:
 if node.type=='EMISSION':node.inputs['Strength'].default_value=.45
 if node.type=='MIX_RGB' and node.blend_type=='MULTIPLY':node.inputs[2].default_value=(.80,.80,.80,1)
bpy.context.view_layer.update()
for name,old in protected.items():assert signature(bpy.data.objects[name])==old,name
scene['study']='V10: extended stepped rear, upper grille and lower cover from reference; V9 lighting refined. Front/guns/pivot/camera protected.'
scene.render.filepath=str(OUT/'render-000-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v10-rear.blend'))

# Render both lit and exact unlit albedo of the NEW geometry. Palette-family masks
# must no longer use V8 screen pixels now that the rear silhouette has changed.
materials={slot.material for obj in scene.objects if obj.type=='MESH' for slot in obj.material_slots if slot.material}
def albedo_mode():
 restore=[]
 for mat in materials:
  if not mat.use_nodes:continue
  for node in mat.node_tree.nodes:
   if node.type=='EMISSION':
    socket=node.inputs['Strength'];restore.append((mat,socket,socket.default_value,None));socket.default_value=1
   elif node.type in ('BSDF_DIFFUSE','BSDF_GLOSSY'):
    socket=node.inputs['Color'];link=socket.links[0].from_socket if socket.is_linked else None
    restore.append((mat,socket,tuple(socket.default_value),link))
    for edge in list(socket.links):mat.node_tree.links.remove(edge)
    socket.default_value=(0,0,0,1)
 return restore

def restore_lighting(state):
 for mat,socket,value,link in state:
  socket.default_value=value
  if link:mat.node_tree.links.new(link,socket)
for degrees in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 center=bpy.data.objects['bearing_red_drum'].matrix_world.translation
 assert abs(center.x)<1e-5 and abs(center.y-.12)<1e-5
 scene.render.filepath=str(OUT/f'render-{degrees:03d}-raw.png');bpy.ops.render.render(write_still=True)
 state=albedo_mode();scene.render.filepath=str(OUT/f'albedo-{degrees:03d}.png');bpy.ops.render.render(write_still=True);restore_lighting(state)
pivot.rotation_euler.z=0;bpy.context.view_layer.update()
for name,old in protected.items():assert signature(bpy.data.objects[name])==old,name
(OUT/'model-change-report.json').write_text(json.dumps({'rear_extension_world_units':EXTENSION,
 'protected_existing_objects':sorted(protected),'protected_objects_unchanged':True,'changes':changes,
 'new_parts':[o.name for o in scene.objects if o.name.startswith('v10_')],
 'camera_and_pivot_unchanged':True,'rear_hidden_details':'inferred, not visible in single source view',
 'lighting_changes':{'ao_distance':.24,'contact_shadow_distance':.30,'bearing_ambient':.45,'bearing_diffuse_weight':.80}},indent=2)+'\n')
print('SENTRY_V10_REAR_COMPLETE')
