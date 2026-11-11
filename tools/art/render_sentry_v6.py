"""Texture/UV-only revision of V5. Meshes, transforms and camera are immutable."""
from pathlib import Path
import bpy,hashlib,json,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-06'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-05/sentry-v5.blend'))
scene=bpy.context.scene

def geometry_hash():
 data=[]
 for obj in sorted(scene.objects,key=lambda o:o.name):
  data.append((obj.name,obj.type,[list(row) for row in obj.matrix_world],obj.hide_render))
  if obj.type=='MESH':
   data.append(([list(v.co) for v in obj.data.vertices],[list(p.vertices) for p in obj.data.polygons]))
  if obj.type=='CAMERA':data.append((obj.data.type,obj.data.ortho_scale))
 return hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
before=geometry_hash();materials={};assignments=[]
def material(name):
 if name in materials:return materials[name]
 mat=bpy.data.materials.new('v6_cut_'+name);mat.use_nodes=True
 nodes=mat.node_tree.nodes;nodes.clear()
 tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures'/(name+'.png')));tex.image.pack()
 tex.interpolation='Closest';tex.extension='EXTEND'
 emit=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
 mat.node_tree.links.new(tex.outputs['Color'],emit.inputs[0]);mat.node_tree.links.new(emit.outputs[0],out.inputs[0])
 materials[name]=mat;return mat

def planar(obj,name,axes,selector,flip=(False,False)):
 mesh=obj.data;uv=mesh.uv_layers.active or mesh.uv_layers.new()
 idx=len(mesh.materials);mesh.materials.append(material(name))
 lo=[min(v.co[a] for v in mesh.vertices) for a in axes]
 hi=[max(v.co[a] for v in mesh.vertices) for a in axes]
 count=0
 for face in mesh.polygons:
  if not selector(face):continue
  face.material_index=idx;count+=1
  for li in face.loop_indices:
   point=mesh.vertices[mesh.loops[li].vertex_index].co
   values=[(point[a]-lo[i])/max(hi[i]-lo[i],1e-7) for i,a in enumerate(axes)]
   uv.data[li].uv=[1-v if flip[i] else v for i,v in enumerate(values)]
 assignments.append({'object':obj.name,'cut':name,'faces':count,'axes':axes,'flip':flip})

for obj in scene.objects:
 if obj.type!='MESH':continue
 name=obj.name
 if name.startswith('roof_main_plate'):
  # Source rectification U runs rearwards; V runs towards the left side.
  # V4/V5 mapped X/Y directly, effectively turning the source roof detail sideways.
  planar(obj,'roof',(1,0),lambda p:p.normal.z>.8,(False,True))
 elif name.startswith('rear_roof_ledge'):
  planar(obj,'rear-roof',(1,0),lambda p:p.normal.z>.8,(False,True))
 elif name.startswith('front_sloping_red_armor'):
  planar(obj,'red-slope',(0,2),lambda p:abs(p.normal.z)>.4)
 elif name.startswith('sensor_window_glass'):
  # This reference detail is a worn metal cover, not a black glass screen.
  planar(obj,'sensor-metal',(0,2),lambda p:abs(p.normal.z)>.4)
 elif name.startswith('front_receiver_red_plate'):
  planar(obj,'receiver',(0,2),lambda p:p.normal.y<-.5)
 elif name.startswith('shoulder_rail'):
  planar(obj,'shoulder',(0,2),lambda p:abs(p.normal.z)>.4)
 elif name.startswith('side_thick_plate'):
  planar(obj,'front-edge',(0,2),lambda p:p.normal.y<-.4)
 elif name.startswith('separate_barrel_tube'):
  # Only the bright face gets a copied longitudinal wear strip; preserve dark grooves.
  normals=[(obj.matrix_world.to_3x3()@p.normal).normalized() for p in obj.data.polygons]
  key=Vector((-.6,-.8,1.4)).normalized()
  candidates=[p for p,n in zip(obj.data.polygons,normals) if abs(p.normal.z)<.1]
  face=max(candidates,key=lambda p:normals[p.index].dot(key))
  # Axis with maximum transverse variation on this face, rather than stretching across tube diameter.
  transverse=max((0,1),key=lambda a:max(obj.data.vertices[i].co[a] for i in face.vertices)-min(obj.data.vertices[i].co[a] for i in face.vertices))
  planar(obj,'steel-tube',(2,transverse),lambda p:p.index==face.index,(True,False))
 elif name=='bearing_red_drum':
  idx=len(obj.data.materials);obj.data.materials.append(material('bearing'))
  uv=obj.data.uv_layers.active
  for face in obj.data.polygons:
   if abs(face.normal.z)>.5:continue
   face.material_index=idx
   points=[obj.data.vertices[obj.data.loops[li].vertex_index].co for li in face.loop_indices]
   us=[(math.atan2(p.y,p.x)/math.tau+.25)%1 for p in points]
   if max(us)-min(us)>.5:us=[u+1 if u<.5 else u for u in us]
   for li,p,u in zip(face.loop_indices,points,us):uv.data[li].uv=(u,(p.z+.185)/.37)
  for node in material('bearing').node_tree.nodes:
   if node.type=='TEX_IMAGE':node.extension='REPEAT'
  assignments.append({'object':name,'cut':'bearing','mapping':'continuous cylindrical'})
after=geometry_hash();assert before==after,'Texture pass altered model geometry or camera'
(OUT/'texture-assignment-report.json').write_text(json.dumps({'geometry_before':before,
 'geometry_after':after,'geometry_unchanged':True,'assignments':assignments},indent=2)+'\n')
scene['study']='V6: texture/UV pass only; exact V5 geometry and camera, asserted by hash.'
scene.render.filepath=str(OUT/'textured-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v6-textures.blend'))
bpy.ops.render.render(write_still=True)
print('SENTRY_V6_COMPLETE_GEOMETRY_UNCHANGED')
