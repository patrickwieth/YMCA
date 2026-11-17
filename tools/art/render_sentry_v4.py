"""Refine V3 geometry and use separate source-cut UV materials for each armor part."""
import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-04'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-03/sentry-geometry-v3.blend'))
scene=bpy.context.scene
pivot=bpy.data.objects['turret_pivot']
# Shorter rear housing/side plate; keep gun receiver and pivot fixed.
housing_prefixes=('sloped_main','front_sloping','sensor_window','side_','bolt_center',
                  'rear_side','rear_vent','rear_roof','roof_')
for obj in scene.objects:
 if obj.type=='MESH' and obj.name.startswith(housing_prefixes):
  matrix=obj.matrix_world.copy(); inverse=matrix.inverted()
  for vertex in obj.data.vertices:
   point=matrix@vertex.co
   point.y=-.95+(point.y+.95)*.84
   vertex.co=inverse@point
  obj.data.update()

# Distinct gray shoulder rails frame the narrower red center strip.
def rail(sign):
 yz=[(-.97,1.39),(-.36,1.98),(-.28,2.01),(-.86,1.39)]
 xs=sorted((sign*.49,sign*.77));n=len(yz)
 vertices=[(x,-.95+(y+.95)*.84,z+.035) for x in xs for y,z in yz]
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 mesh=bpy.data.meshes.new('shoulder_rail');mesh.from_pydata(vertices,[],faces);mesh.update()
 obj=bpy.data.objects.new('shoulder_rail',mesh);scene.collection.objects.link(obj);obj.parent=pivot
 for name in ('armor','light','dark'):obj.data.materials.append(bpy.data.materials[name])
 for face in mesh.polygons:face.material_index=1 if face.normal.z>.4 else 0
 return obj
rails=[rail(-1),rail(1)]

textures={}
def material(name,alpha=False):
 if name in textures:return textures[name]
 m=bpy.data.materials.new('cutpaste_'+name);m.use_nodes=True
 nodes=m.node_tree.nodes;nodes.clear()
 tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures'/ (name+'.png')));tex.image.pack()
 tex.interpolation='Closest';tex.extension='EXTEND'
 emission=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
 m.node_tree.links.new(tex.outputs['Color'],emission.inputs['Color'])
 if alpha:
  transparent=nodes.new('ShaderNodeBsdfTransparent');mix=nodes.new('ShaderNodeMixShader')
  m.node_tree.links.new(tex.outputs['Alpha'],mix.inputs[0]);m.node_tree.links.new(transparent.outputs[0],mix.inputs[1]);m.node_tree.links.new(emission.outputs[0],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],output.inputs[0])
  m.blend_method='CLIP';m.alpha_threshold=.5
 else:m.node_tree.links.new(emission.outputs[0],output.inputs[0])
 textures[name]=m;return m

def planar(obj,name,axes,selector=lambda p:True,alpha=False,bounds=None):
 data=obj.data
 if not data.uv_layers:data.uv_layers.new()
 uv=data.uv_layers.active.data
 index=len(data.materials);data.materials.append(material(name,alpha))
 coords=[v.co for v in data.vertices]
 if bounds:low,high=bounds
 else:
  low=[min(v[a] for v in coords) for a in axes];high=[max(v[a] for v in coords) for a in axes]
 for poly in data.polygons:
  if not selector(poly):continue
  poly.material_index=index
  for li in poly.loop_indices:
   co=data.vertices[data.loops[li].vertex_index].co
   uv[li].uv=tuple((co[a]-low[i])/max(high[i]-low[i],1e-6) for i,a in enumerate(axes))

for obj in list(scene.objects):
 if obj.type!='MESH':continue
 name=obj.name
 if name.startswith('side_inset_armor'):
  planar(obj,'side-metal',(1,2),lambda p:abs(p.normal.x)>.9)
  # Separate emblem layer shares the plate geometry/UV footprint; no background projection.
  mesh=obj.data.copy();decal=bpy.data.objects.new('source_emblem_decal',mesh);scene.collection.objects.link(decal)
  decal.parent=pivot;decal.matrix_world=obj.matrix_world.copy()
  sign=1 if sum(v.co.x for v in mesh.vertices)>0 else -1
  decal.location.x+=sign*.012
  # Only the outward face remains, eliminating rear/edge decal artifacts.
  vertices=[tuple(v.co) for v in mesh.vertices]
  faces=[tuple(p.vertices) for p in mesh.polygons if p.normal.x*sign>.9]
  clean=bpy.data.meshes.new('emblem_face');clean.from_pydata(vertices,[],faces);clean.update();decal.data=clean
  planar(decal,'emblem-cutout',(1,2),alpha=True)
 elif name.startswith('roof_main_plate'):
  planar(obj,'roof_plate',(0,1),lambda p:p.normal.z>.9)
 elif name.startswith('rear_roof_ledge'):
  planar(obj,'roof_rear',(0,1),lambda p:p.normal.z>.9)
 elif name.startswith('front_sloping_red_armor'):
  planar(obj,'front_red',(0,1),lambda p:p.normal.z>.4)
 elif name.startswith('rear_side_equipment'):
  planar(obj,'rear_equipment',(1,2),lambda p:abs(p.normal.x)>.9)
 elif name=='bearing_red_drum':
  # Each cylindrical segment receives a narrow source strip, not the entire curved photograph.
  data=obj.data;data.uv_layers.new() if not data.uv_layers else None
  idx=len(data.materials);data.materials.append(material('drum_red'))
  for poly in data.polygons:
   if abs(poly.normal.z)>.5:continue
   poly.material_index=idx
   for li,uv in zip(poly.loop_indices,[(0,0),(.25,0),(.25,1),(0,1)]):data.uv_layers.active.data[li].uv=uv
# Shoulder pieces get source wear only on the sloping face, with nearest sampling.
for obj in rails:planar(obj,'shoulder_metal',(0,2),lambda p:p.normal.z>.4)
scene['study']='V4: shortened housing, separate gray shoulders, per-part source-cut UVs and separate source emblem.'
scene.render.filepath=str(OUT/'textured-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-v4.blend'))
bpy.ops.render.render(write_still=True)
# Geometry-only companion, while saved .blend retains textures.
for obj in scene.objects:
 if obj.type!='MESH':continue
 if obj.name.startswith('source_emblem_decal'):obj.hide_render=True;continue
 offset=len(obj.data.materials)
 for name in ('dark','armor','light'):obj.data.materials.append(bpy.data.materials[name])
 for poly in obj.data.polygons:
  normal=(obj.matrix_world.to_3x3()@poly.normal).normalized()
  poly.material_index=offset+(2 if normal.z>.6 else 1 if normal.dot(Vector((-.6,-.8,1.4)))>0 else 0)
scene.render.filepath=str(OUT/'neutral-raw.png');bpy.ops.render.render(write_still=True)
print('SENTRY_V4_COMPLETE')
