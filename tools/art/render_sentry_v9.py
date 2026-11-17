"""V9 lighting study: real directional/contact shadows on unchanged V8 geometry.
Source texture images are unchanged. Final sprite palette is restored separately.
"""
from pathlib import Path
import bpy,json,hashlib,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-09'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-08/sentry-v8-textures.blend'))
scene=bpy.context.scene

def geometry_hash():
 records=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  if obj.type not in ('MESH','CAMERA','EMPTY'):continue
  records.append((obj.name,obj.type,[list(row) for row in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':records.append(([list(v.co) for v in obj.data.vertices],[list(p.vertices) for p in obj.data.polygons]))
  if obj.type=='CAMERA':records.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(records,sort_keys=True).encode()).hexdigest()
before=geometry_hash();changed=[]
# Diffuse light supplies actual face orientation, cast shadows, and AO. A low
# unlit contribution retains source detail in shaded faces without flattening it.
used={slot.material for obj in scene.objects if obj.type=='MESH' for slot in obj.material_slots if slot.material}
for mat in used:
 if not mat.use_nodes:continue
 nodes=mat.node_tree.nodes;links=mat.node_tree.links
 for em in list(nodes):
  if em.type!='EMISSION':continue
  destinations=[link.to_socket for link in em.outputs[0].links]
  if not destinations:continue
  source=em.inputs[0].links[0].from_socket if em.inputs[0].is_linked else None
  color=tuple(em.inputs[0].default_value)
  diffuse=nodes.new('ShaderNodeBsdfDiffuse');diffuse.name='V9 source diffuse';diffuse.inputs['Roughness'].default_value=0
  # The source side plate/emblem already includes strong baked shading. Do not
  # double-darken these; lighting mainly adds geometric relief to edges/seams.
  baked_side=any(tag in mat.name for tag in ('side-metal','emblem-cutout','v8_source_side_metal'))
  ambient=.90 if baked_side else .35
  diffuse_weight=.25 if baked_side else .75
  if mat.name=='v6_cut_bearing':ambient=.55;diffuse_weight=.65
  scale=nodes.new('ShaderNodeMixRGB');scale.blend_type='MULTIPLY';scale.inputs[0].default_value=1
  scale.inputs[2].default_value=(diffuse_weight,diffuse_weight,diffuse_weight,1)
  if source:links.new(source,scale.inputs[1])
  else:scale.inputs[1].default_value=color
  links.new(scale.outputs[0],diffuse.inputs['Color'])
  # Keep transparent source-emblem masking: replace only the emission surface
  # connection, not its parent transparency/mix graph.
  add=nodes.new('ShaderNodeAddShader');add.name='V9 ambient plus directional'
  em.inputs['Strength'].default_value=ambient
  for destination in destinations:links.new(add.outputs[0],destination)
  links.new(em.outputs[0],add.inputs[0]);links.new(diffuse.outputs[0],add.inputs[1])
  metal=(mat.name in ('armor','shade','light','edge') or any(tag in mat.name for tag in
   ('roof','shoulder','front-edge','steel','side-metal','gun_mid','gun_edge','side_metal','bearing_edge')))
  if metal and not any(tag in mat.name for tag in ('red','emblem','outline')):
   # A restrained metallic glint gives curved tubes and chamfers a narrow lit
   # edge. It is quantized to existing source colors, not a smooth white shine.
   gloss=nodes.new('ShaderNodeBsdfGlossy');gloss.inputs['Color'].default_value=(.08,.07,.06,1)
   gloss.inputs['Roughness'].default_value=.35
   metal_add=nodes.new('ShaderNodeAddShader');metal_add.name='V9 narrow metal glint'
   for destination in destinations:links.new(metal_add.outputs[0],destination)
   links.new(add.outputs[0],metal_add.inputs[0]);links.new(gloss.outputs[0],metal_add.inputs[1])
  changed.append(mat.name)
# Fixed world-space light: does NOT rotate with turret.
data=bpy.data.lights.new('reference_key_light','SUN');data.energy=4.0;data.angle=0
key=bpy.data.objects.new('reference_key_light',data);scene.collection.objects.link(key)
# Direction towards the model from upper-left/front.
key.rotation_euler=(-Vector((-3,-4,8))).to_track_quat('-Z','Y').to_euler()
data.use_shadow=True;data.use_contact_shadow=True;data.contact_shadow_distance=.22;data.contact_shadow_bias=.002
scene.eevee.use_gtao=True;scene.eevee.gtao_distance=.30;scene.eevee.gtao_factor=1.5
scene.eevee.use_soft_shadows=False;scene.eevee.shadow_cube_size='2048';scene.eevee.shadow_cascade_size='4096'
scene.eevee.taa_render_samples=1
scene.render.dither_intensity=0;scene.render.filter_size=.01
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(0,0,0,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=0
assert before==geometry_hash()
scene['study']='V9: unchanged source textures/geometry, physical key light, hard cast/contact shadows and AO. Final source palette via compare script.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v9-lighting.blend'))
pivot=bpy.data.objects['turret_pivot'];original=pivot.rotation_euler.copy()
for degrees in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 scene.render.filepath=str(OUT/f'render-{degrees:03d}-raw.png');bpy.ops.render.render(write_still=True)
pivot.rotation_euler=original;bpy.context.view_layer.update()
assert before==geometry_hash()
(OUT/'lighting-report.json').write_text(json.dumps({'geometry_before':before,'geometry_after':geometry_hash(),
 'geometry_unchanged':True,'source_textures_unchanged':True,'converted_materials':sorted(set(changed)),
 'lighting':{'sun_energy':data.energy,'direction_to_light':[-3,-4,8],'ambient_emission':.35,'baked_side_ambient':.90,'diffuse_weight':.75,'baked_side_diffuse_weight':.25,'bearing_ambient':.55,'bearing_diffuse_weight':.65,
 'ao_distance':.30,'ao_factor':1.5,'contact_shadow_distance':.22,'metal_gloss_roughness':.35,'metal_gloss_linear_rgb':[.08,.07,.06]},
 'note':'Raw diffuse renders contain new lighting colors. Final sprites must pass exact source-palette mapping.'},indent=2)+'\n')
print('SENTRY_V9_LIGHTING_COMPLETE')
