"""V13: narrower weapon tone range, unchanged V12 geometry; material-only pass."""
from pathlib import Path
import bpy,json,hashlib,math,shutil
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-13'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-12/sentry-v12.blend'))
scene=bpy.context.scene;pivot=bpy.data.objects['turret_pivot']
shutil.copyfile(BASE/'attempt-12/attachment.json',OUT/'attachment.json')

def geometry_hash():
 data=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  data.append((obj.name,obj.type,[list(r) for r in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':data.extend(([list(v.co) for v in obj.data.vertices],[list(p.vertices) for p in obj.data.polygons]))
  if obj.type=='CAMERA':data.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
before=geometry_hash()
prefixes=('separate_barrel_tube','individual_muzzle_rim','recessed_muzzle_bore',
 'open_barrel_cluster_collar','collar_fastener','collar_dark_edge','octagonal_muzzle_surround',
 'muzzle_outer_lip','central_spindle','gun_socket_rim','gun_socket_steel','receiver_socket_bezel')
def role(obj):
 if obj.name.startswith('recessed_muzzle_bore'):return 2
 if obj.name.startswith('central_spindle'):return 3
 return 1 if obj.name.startswith(prefixes) else 0

def srgb(x):return round(255*(12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055))
def linear(x):
 x/=255
 return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
# All replacement swatches occur verbatim in the source image. Broad metallic
# surfaces use its middle grays, NOT the source's brightest edge/black-hole colors.
swatches={'dark_metal':(42,38,39),'shadow':(42,38,39),'body':(65,63,65),'light_face':(85,81,82)}
clones={};changes=[]
for obj in scene.objects:
 if obj.type!='MESH' or not role(obj):continue
 kind=role(obj)
 for index in sorted({face.material_index for face in obj.data.polygons}):
  original=obj.data.materials[index];key=(kind,original.name)
  if key not in clones:
   mat=original.copy();mat.name=f'v13_weapon_{kind}_{original.name}'
   # True bore cavities stay black; surface lip shadows no longer use broad black bands.
   if kind!=2:
    for node in mat.node_tree.nodes:
     if node.type=='EMISSION':
      node.inputs['Strength'].default_value=.85
      if not node.inputs['Color'].is_linked:
       old=tuple(srgb(x) for x in node.inputs['Color'].default_value[:3])
       peak=max(old)
       new=swatches['dark_metal'] if peak<30 else swatches['shadow'] if peak<=53 else swatches['body'] if peak<=85 else swatches['light_face']
       node.inputs['Color'].default_value=(*[linear(x) for x in new],1)
       # The same color also feeds the diffuse scale node.
       for other in mat.node_tree.nodes:
        if other.type=='MIX_RGB' and other.blend_type=='MULTIPLY' and not other.inputs[1].is_linked:
         other.inputs[1].default_value=(*[linear(x) for x in new],1)
       changes.append({'material':mat.name,'from':old,'to':new})
     elif node.type=='MIX_RGB' and node.blend_type=='MULTIPLY':node.inputs[2].default_value=(.20,.20,.20,1)
     elif node.type=='BSDF_GLOSSY':node.inputs['Color'].default_value=(0,0,0,1)
   clones[key]=mat
  obj.data.materials[index]=clones[key]
assert before==geometry_hash()
scene['study']='V13: reduced weapon-surface contrast; true bores retained; paired 1px plate highlights applied in final sprite step.'
scene.render.filepath=str(OUT/'render-000-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v13.blend'))
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
 scene.render.filepath=str(OUT/f'render-{angle:03d}-raw.png');bpy.ops.render.render(write_still=True)
 restore=albedo_mode();scene.render.filepath=str(OUT/f'albedo-{angle:03d}.png');bpy.ops.render.render(write_still=True)
 for mat,s,value,link in restore:
  s.default_value=value
  if link:mat.node_tree.links.new(link,s)
# Geometry-derived weapon masks permit a V12/V13 area-distribution comparison.
mask_materials={}
for value in (0,1,2,3):
 mat=bpy.data.materials.new(f'v13_diagnostic_weapon_mask_{value}');mat.use_nodes=True
 nodes=mat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
 em.inputs[0].default_value=(linear(value),0,0,1);mat.node_tree.links.new(em.outputs[0],out.inputs[0]);mask_materials[value]=mat
restore_meshes=[]
for obj in scene.objects:
 if obj.type!='MESH':continue
 restore_meshes.append((obj,list(obj.data.materials),[p.material_index for p in obj.data.polygons]))
 obj.data.materials.clear();obj.data.materials.append(mask_materials[role(obj)])
 for face in obj.data.polygons:face.material_index=0
for angle in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update()
 scene.render.filepath=str(OUT/f'weapon-mask-{angle:03d}.png');bpy.ops.render.render(write_still=True)
for obj,slots,indices in restore_meshes:
 obj.data.materials.clear()
 for mat in slots:obj.data.materials.append(mat)
 for p,i in zip(obj.data.polygons,indices):p.material_index=i
pivot.rotation_euler.z=0;bpy.context.view_layer.update();assert before==geometry_hash()
(OUT/'material-change-report.json').write_text(json.dumps({'geometry_before':before,'geometry_after':geometry_hash(),
 'geometry_unchanged':True,'source_textures_unchanged':True,'weapon_swatches':swatches,'changes':changes,
 'weapon_ambient':.85,'weapon_diffuse_weight':.20,'weapon_gloss':0,'true_bores_kept':True,
 'note':'Diagnostic mask colors are not final sprite colors.'},indent=2)+'\n')
print('SENTRY_V13_WEAPON_MATERIALS_COMPLETE')
