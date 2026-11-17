"""Texture-only V7: same model, UVs and camera as V6; diagnostic angles only."""
from pathlib import Path
import bpy,hashlib,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-07'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-06/sentry-v6-textures.blend'))
scene=bpy.context.scene

def geometry_hash():
 records=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  records.append((obj.name,obj.type,[list(row) for row in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':records.append(([list(v.co) for v in obj.data.vertices],[list(p.vertices) for p in obj.data.polygons]))
  if obj.type=='CAMERA':records.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(records,sort_keys=True).encode()).hexdigest()
before=geometry_hash();replaced=[]
for mat in bpy.data.materials:
 if not mat.name.startswith('v6_cut_'):continue
 for node in mat.node_tree.nodes:
  if node.type!='TEX_IMAGE' or not node.image:continue
  filename=Path(node.image.filepath).name;path=OUT/'textures'/filename
  assert path.exists(),path
  node.image=bpy.data.images.load(str(path),check_existing=True);node.image.pack();node.interpolation='Closest'
  replaced.append({'material':mat.name,'texture':filename})
# V5's solid sensor-frame mesh occludes the recessed cover. Resolve it using
# a material on the visible frame face, without changing/removing any geometry.
mat=bpy.data.materials.new('v7_visible_source_cover');mat.use_nodes=True
nodes=mat.node_tree.nodes;nodes.clear()
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures/sensor-frame.png'));tex.image.pack();tex.interpolation='Closest';tex.extension='EXTEND'
emit=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
mat.node_tree.links.new(tex.outputs['Color'],emit.inputs[0]);mat.node_tree.links.new(emit.outputs[0],output.inputs[0])
obj=bpy.data.objects['sensor_window_frame'];mesh=obj.data
idx=len(mesh.materials);mesh.materials.append(mat)
uv=mesh.uv_layers.active or mesh.uv_layers.new()
lo=[min(v.co[a] for v in mesh.vertices) for a in (0,2)]
hi=[max(v.co[a] for v in mesh.vertices) for a in (0,2)]
for face in mesh.polygons:
 if abs(face.normal.z)<.4:continue
 face.material_index=idx
 for li in face.loop_indices:
  p=mesh.vertices[mesh.loops[li].vertex_index].co
  uv.data[li].uv=((p.x-lo[0])/(hi[0]-lo[0]),(p.z-lo[1])/(hi[1]-lo[1]))
replaced.append({'material':mat.name,'texture':'sensor-frame.png','object':obj.name})
assert before==geometry_hash()
scene['study']='V7: source-cut texture composition and visible cover UV only; exact V6 geometry and camera retained.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v7-textures.blend'))
pivot=bpy.data.objects['turret_pivot'];original=pivot.rotation_euler.copy()
for degrees in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(degrees);bpy.context.view_layer.update()
 center=bpy.data.objects['bearing_red_drum'].matrix_world.translation
 assert abs(center.x)<1e-5 and abs(center.y-.12)<1e-5
 scene.render.filepath=str(OUT/f'render-{degrees:03d}-raw.png');bpy.ops.render.render(write_still=True)
pivot.rotation_euler=original;bpy.context.view_layer.update()
assert before==geometry_hash()
(OUT/'texture-assignment-report.json').write_text(json.dumps({'geometry_before':before,
 'geometry_after':geometry_hash(),'geometry_unchanged':True,'replaced_images':replaced},indent=2)+'\n')
print('SENTRY_V7_TEXTURES_COMPLETE')
