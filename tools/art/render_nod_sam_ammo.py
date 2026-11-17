"""Render ammo states from the APPROVED pass12 blend, without remodeling it.
Run in background Blender. Only complete missile-object visibility changes.
"""
from pathlib import Path
import bpy,json,hashlib,math,re,shutil
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/nod sam site';SOURCE=F/'pass-12';OUT=F/'ammo-variants';OUT.mkdir(exist_ok=True)
scene_hash=hashlib.sha256((SOURCE/'nod-sam.blend').read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'nod-sam.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];objects=[o for o in bpy.data.objects if o.type=='MESH' and 'family' in o]
original_mats={o.name:list(o.data.materials) for o in objects}
slots={}
for o in objects:
 match=re.match(r'^Missile (\d+) ',o.name) or re.match(r'^Missile jacket red witness (\d+)$',o.name)
 if match:slots[o.name]=int(match[1])
assert len(slots)==60 and set(slots.values())==set(range(4)),(len(slots),slots)
def fingerprint(o):
 data={'vertices':[list(v.co) for v in o.data.vertices],'faces':[list(p.vertices) for p in o.data.polygons],'uv':[[list(v.uv) for v in layer.data] for layer in o.data.uv_layers],'basis':[list(row) for row in o.matrix_basis],'parent_inverse':[list(row) for row in o.matrix_parent_inverse],'parent':o.parent.name if o.parent else None,'family':o['family'],'bias':o['bias']}
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
before={o.name:fingerprint(o) for o in objects}
# Unused ID materials are not retained by Blender's save/load cycle. Recreate
# only these export diagnostics, using the exact approved renderer's encoding.
def linear(x):
 x=x/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
for gid in {o['surface_id'] for o in objects}:
 name='Surface '+str(gid)
 if name not in bpy.data.materials:
  m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(linear(gid%256),linear(gid//256),0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0])
def light(mask=False):
 direction=Vector((-.25,-.5,.85)).normalized()
 for o in objects:
  mats=original_mats[o.name];names=[m.name for m in mats]
  for j,m in enumerate(mats):o.data.materials[j]=bpy.data.materials['Surface '+str(o['surface_id'])] if mask else m
  for poly in o.data.polygons:
   shade=max(0,min(5,int(((o.matrix_world.to_3x3()@poly.normal).normalized().dot(direction)+1)*2.7)+o['bias']));poly.material_index=names.index(o['family']+str(shade))
# Front pair first, then rear pair. Surviving rockets never move to another slot.
fire_order=[2,3,0,1];manifest={'approved_blend_sha256':scene_hash,'source_pass':12,'fire_order':fire_order,'slot_objects':slots,'object_geometry_sha256':before,'states':{},'runtime_installed':False}
source_geometry=json.loads((SOURCE/'geometry.json').read_text())
for count in range(5):
 folder=OUT/str(count);folder.mkdir(exist_ok=True);loaded=sorted(fire_order[4-count:]);hidden=sorted(name for name,slot in slots.items() if slot not in loaded)
 for o in objects:o.hide_render=o.name in hidden;o.hide_set(o.hide_render)
 pivot.rotation_euler.z=0;bpy.context.view_layer.update();light()
 if count==4:
  # Freeze the artist-approved output byte-for-byte, not merely approximately.
  shutil.copy2(SOURCE/'nod-sam.blend',folder/'nod-sam.blend');shutil.copytree(SOURCE/'review',folder/'review',dirs_exist_ok=True)
  # One independent render checks that the loaded-scene lighting path reproduces
  # the frozen raw image; this is not used to replace the approved output.
  s.render.filepath=str(folder/'full-control-raw.png');bpy.ops.render.render(write_still=True)
 else:
  bpy.ops.wm.save_as_mainfile(filepath=str(folder/'nod-sam.blend'))
  for i in range(32):
   pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update()
   for mask in (False,True):
    light(mask);s.render.filepath=str(folder/(f'turret-{i:03d}-'+('ids.png' if mask else 'raw.png')));bpy.ops.render.render(write_still=True)
 assert {o.name:fingerprint(o) for o in objects}==before,'Geometry changed during visibility-only export'
 g=dict(source_geometry,missile_count=count,loaded_slots=loaded,hidden_objects=hidden,source_pass=12,source_blend_sha256=scene_hash,visibility_only=True,object_geometry_sha256=before)
 (folder/'geometry.json').write_text(json.dumps(g,indent=2)+'\n');manifest['states'][str(count)]={'loaded_slots':loaded,'hidden_objects':hidden,'missile_count':count,'geometry_unchanged':True}
 print('NOD_SAM_AMMO_RENDERED',count,flush=True)
assert hashlib.sha256((SOURCE/'nod-sam.blend').read_bytes()).hexdigest()==scene_hash
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print('NOD_SAM_AMMO_COMPLETE',flush=True)
