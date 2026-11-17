"""V8: V6 wear + source-copied rivets and part-specific source colors. No model edits."""
from pathlib import Path
import bpy,json,hashlib,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-08'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-06/sentry-v6-textures.blend'))
scene=bpy.context.scene
provenance=json.loads((OUT/'texture-provenance.json').read_text());swatches=provenance['swatches']

def geometry_hash():
 records=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  records.append((obj.name,obj.type,[list(row) for row in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':records.append(([list(v.co) for v in obj.data.vertices],[list(p.vertices) for p in obj.data.polygons]))
  if obj.type=='CAMERA':records.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(records,sort_keys=True).encode()).hexdigest()
before=geometry_hash();assignments=[]
# Repack all existing image cuts without recoloring V6's weathering.
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   path=OUT/'textures'/Path(node.image.filepath).name
   if path.exists():
    node.image=bpy.data.images.load(str(path),check_existing=True);node.image.pack();node.interpolation='Closest'

def linear(x):
 x/=255
 return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4

def flat(name):
 mat=bpy.data.materials.new('v8_source_'+name);mat.use_nodes=True
 nodes=mat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
 em.inputs[0].default_value=(*[linear(x) for x in swatches[name]['rgba'][:3]],1)
 mat.node_tree.links.new(em.outputs[0],out.inputs[0]);return mat
flats={name:flat(name) for name in swatches}
textures={}
def texture(name):
 if name in textures:return textures[name]
 mat=bpy.data.materials.new('v8_cut_'+name);mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear()
 tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures'/(name+'.png')),check_existing=True);tex.image.pack();tex.interpolation='Closest';tex.extension='EXTEND'
 em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
 mat.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);mat.node_tree.links.new(em.outputs[0],out.inputs[0]);textures[name]=mat;return mat

def flat_faces(obj,name,selector=lambda f:True):
 idx=len(obj.data.materials);obj.data.materials.append(flats[name])
 for face in obj.data.polygons:
  if selector(face):face.material_index=idx
 assignments.append({'object':obj.name,'source_swatch':name})

def disk_texture(obj,name):
 mesh=obj.data;idx=len(mesh.materials);mesh.materials.append(texture(name))
 uv=mesh.uv_layers.active or mesh.uv_layers.new()
 lo=[min(v.co[a] for v in mesh.vertices) for a in (1,0)]
 hi=[max(v.co[a] for v in mesh.vertices) for a in (1,0)]
 for face in mesh.polygons:
  if abs(face.normal.z)<.9:continue
  face.material_index=idx
  for li in face.loop_indices:
   p=mesh.vertices[mesh.loops[li].vertex_index].co
   uv.data[li].uv=((p.y-lo[0])/(hi[0]-lo[0]),1-(p.x-lo[1])/(hi[1]-lo[1]))
 assignments.append({'object':obj.name,'source_cut':name})

for obj in scene.objects:
 if obj.type!='MESH':continue
 name=obj.name
 if name.startswith('side_recessed_bolt'):
  # Source edge/head structure, not a uniformly gray cylinder with a white dot.
  disk_texture(obj,'side-rivet' if obj.location.y<0 else 'rear-side-rivet')
 elif name.startswith('bolt_center'):
  flat_faces(obj,'rivet_center')
 elif name.startswith('roof_fastener'):
  disk_texture(obj,'recessed-rivet')
 elif name.startswith('side_thick_plate'):
  # Keep V6 wear on the front/sloping faces; only replace the untextured side rim.
  flat_faces(obj,'side_metal',lambda f:abs(f.normal.x)>.9)
 elif name=='bearing_upper_steel_lip':
  flat_faces(obj,'upper_bearing',lambda f:f.normal.z>.5)
  flat_faces(obj,'upper_bearing_shadow',lambda f:abs(f.normal.z)<=.5)
 elif name=='bearing_lower_highlight':
  flat_faces(obj,'lower_bearing_edge',lambda f:abs(f.normal.z)<=.5)

# Audit all flat emission inputs: source colors before rendering, not just output quantization.
source_rgb={tuple(c[:3]) for c in provenance['source_rgba_palette']}
flat_audit=[]
def srgb(v):return round(255*(12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055))
used_materials={slot.material for obj in scene.objects if obj.type=='MESH' for slot in obj.material_slots if slot.material}
for mat in used_materials:
 if not mat.use_nodes:continue
 for node in mat.node_tree.nodes:
  if node.type!='EMISSION' or node.inputs[0].is_linked:continue
  rgb=tuple(srgb(v) for v in node.inputs[0].default_value[:3])
  assert rgb in source_rgb,(mat.name,rgb)
  flat_audit.append({'material':mat.name,'rgb':rgb})
assert geometry_hash()==before
scene['study']='V8: V6 texture basis, source hardware cuts, original sampled colors; model and camera unchanged.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v8-textures.blend'))
pivot=bpy.data.objects['turret_pivot'];original=pivot.rotation_euler.copy()
for degrees in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 center=bpy.data.objects['bearing_red_drum'].matrix_world.translation
 assert abs(center.x)<1e-5 and abs(center.y-.12)<1e-5
 scene.render.filepath=str(OUT/f'render-{degrees:03d}-raw.png');bpy.ops.render.render(write_still=True)
pivot.rotation_euler=original;bpy.context.view_layer.update()
assert before==geometry_hash()
(OUT/'texture-assignment-report.json').write_text(json.dumps({'geometry_before':before,'geometry_after':geometry_hash(),
 'geometry_unchanged':True,'assignments':assignments,'flat_material_source_color_audit':flat_audit},indent=2)+'\n')
print('SENTRY_V8_COMPLETE_SOURCE_COLORS_VERIFIED')
