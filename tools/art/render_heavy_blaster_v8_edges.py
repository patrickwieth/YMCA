"""Diagnostic surface-ID renders for native geometry contour pixels.
IDs are masks only, never game textures. V7 meshes/material files remain untouched.
"""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-08-preview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-07-preview/heavy-blaster-v7.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
for o in bpy.data.collections['STATIONARY BASE'].objects:o.hide_render=True
labels={};next_id=1
for obj in bpy.data.collections['ROTATING TURRET'].objects:
 if obj.type!='MESH':continue
 obj.hide_render=False;mesh=obj.data;parent=list(range(len(mesh.polygons)))
 def root(i):
  while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
  return i
 # Suppress polygon facets on round bearing surfaces, retain their cap edges.
 angle=25 if 'bearing' in obj.name or 'trunnion shaft' in obj.name else 10
 edges={}
 for p in mesh.polygons:
  for edge in p.edge_keys:edges.setdefault(tuple(sorted(edge)),[]).append(p.index)
 for adjacent in edges.values():
  if len(adjacent)!=2:continue
  a,b=adjacent
  if mesh.polygons[a].normal.dot(mesh.polygons[b].normal)>math.cos(math.radians(angle)):parent[root(a)]=root(b)
 groups={}
 for p in mesh.polygons:groups.setdefault(root(p.index),[]).append(p.index)
 mesh.materials.clear()
 for faces in groups.values():
  label=next_id;next_id+=1;assert label<4096
  rgb=[((label>>(4*i))&15)*16+8 for i in range(3)]
  mat=bpy.data.materials.new(f'ID MASK {label}');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial')
  def linear(c):
   c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
  em.inputs[0].default_value=(*map(linear,rgb),1);mat.node_tree.links.new(em.outputs[0],out.inputs[0]);mesh.materials.append(mat)
  for face in faces:mesh.polygons[face].material_index=len(mesh.materials)-1
  labels[label]={'object':obj.name,'faces':faces,'rgb':rgb,'crease_degrees':angle,'protect_cyan':obj.name=='V7 straight cyan core'}
for size in (160,192):
 folder=OUT/str(size);folder.mkdir(exist_ok=True);s.render.resolution_x=s.render.resolution_y=size;s.render.resolution_percentage=100
 for i in range(32):
  pivot.rotation_euler.z=math.radians(i*11.25);bpy.context.view_layer.update();s.render.filepath=str(folder/f'surface-id-{i:03d}.png');bpy.ops.render.render(write_still=True)
(OUT/'surface-labels.json').write_text(json.dumps(labels,indent=2)+'\n');print('NATIVE_EDGE_MASKS_COMPLETE',len(labels))
