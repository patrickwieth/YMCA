"""Geometry-derived part masks for one-native-pixel ink edges. V10 stays untouched.
Diagnostic ID colors are NOT texture artwork and never appear in final sprites.
"""
from pathlib import Path
import bpy,json,hashlib,math,argparse,sys
parser=argparse.ArgumentParser()
parser.add_argument('--input-attempt',default='attempt-10')
parser.add_argument('--output-attempt',default='attempt-11')
parser.add_argument('--blend-file',default='sentry-v10-rear.blend')
parser.add_argument('--output-blend',default='sentry-v11-edge-study.blend')
parser.add_argument('--angles',default='0,45,180,315')
parser.add_argument('--angle-step',type=float,default=1,help='Degrees per integer filename index.')
options=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
angles=tuple(map(int,options.angles.split(',')))
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/options.output_attempt
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/options.input_attempt/options.blend_file))
scene=bpy.context.scene;pivot=bpy.data.objects['turret_pivot']

def geometry_hash():
 data=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  if obj.type not in ('MESH','CAMERA','EMPTY'):continue
  data.append((obj.name,[list(row) for row in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':data.append(([list(v.co) for v in obj.data.vertices],[list(f.vertices) for f in obj.data.polygons]))
  if obj.type=='CAMERA':data.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
before=geometry_hash()
# Higher group owns a shared boundary, so it gets ONE line, not one line per side.
# Tubes/collars use group 1 and are excluded: a 1px stroke would swallow thin tubes.
def group(name):
 if name.startswith(('roof_main_plate','roof_fastener')):return 90
 if name.startswith('rear_roof_ledge'):return 80
 if name.startswith(('sensor_window','front_receiver_red_plate')):return 75
 if name.startswith(('shoulder_rail','shoulder_separation')):return 70
 if name.startswith(('source_emblem','side_inset_armor','side_plate_outline','side_recessed_bolt','bolt_center')):return 65
 if name.startswith('side_thick_plate'):return 60
 if name.startswith(('v10_rear_lower_cover','rear_service_frame','rear_service_inset','rear_door_source_cut')):return 55
 if name.startswith(('v10_rear_grille','rear_service_grille','rear_service_handle','rear_service_fastener')):return 50
 if name.startswith(('front_sloping_red','sloped_main_housing','front_receiver_rim','rear_side_equipment','v10_rear_red_return','rear_service_shadow')):return 40
 if name.startswith(('bearing_','drum_segment_seam')):return 30
 return 1
materials={};original={};groups={}
for obj in scene.objects:
 if obj.type!='MESH':continue
 index=group(obj.name);groups[obj.name]=index
 if index not in materials:
  mat=bpy.data.materials.new('diagnostic_part_id_'+str(index));mat.use_nodes=True
  nodes=mat.node_tree.nodes;nodes.clear();em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
  v=index/255;linear=v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
  em.inputs[0].default_value=(linear,0,0,1);mat.node_tree.links.new(em.outputs[0],out.inputs[0]);materials[index]=mat
 original[obj.name]=(list(obj.data.materials),[f.material_index for f in obj.data.polygons])
 obj.data.materials.clear();obj.data.materials.append(materials[index])
 for face in obj.data.polygons:face.material_index=0
scene.render.dither_intensity=0;scene.render.filter_size=.01;scene.eevee.taa_render_samples=1
for angle in angles:
 pivot.rotation_euler.z=math.radians(angle*options.angle_step);bpy.context.view_layer.update()
 scene.render.filepath=str(OUT/f'part-ids-{angle:03d}.png');bpy.ops.render.render(write_still=True)
pivot.rotation_euler.z=0;bpy.context.view_layer.update()
# Restore V10 surfaces. The saved model is usable normally, NOT a colored ID model.
for name,(slots,indices) in original.items():
 mesh=bpy.data.objects[name].data;mesh.materials.clear()
 for mat in slots:mesh.materials.append(mat)
 for face,index in zip(mesh.polygons,indices):face.material_index=index
assert geometry_hash()==before
scene['edge_finish']='Run finish_sentry_v11_edges.py after ID passes: one native-pixel original-black edge, unchanged geometry/alpha.'
scene.render.filepath=str(OUT/'render-000-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/options.output_blend))
(OUT/'edge-mask-report.json').write_text(json.dumps({'geometry_before':before,'geometry_after':geometry_hash(),
 'geometry_unchanged':True,'part_groups':groups,'diagnostic_mask_only':True,
 'rule':'Higher group owns shared boundary; group 1 excludes tiny weapon parts.'},indent=2)+'\n')
print('SENTRY_V11_EDGE_MASKS_COMPLETE')
