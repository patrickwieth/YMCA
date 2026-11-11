"""Preview only: replace the projected roof zigzag with a real straight conduit.
Keep the broad V5 model, smaller native V6 sizes, base, side armor and receiver.
"""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-07-preview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-05/heavy-blaster-v5.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
collection=bpy.data.collections['ROTATING TURRET'];housing=bpy.data.objects['closed barrel and receiver housing']
# Only the short sloped roof, not the long barrel roof or receiver deck.
y0,z0=-1.5935028791427612,2.888803005218506;y1,z1=.0681980550289154,3.365116834640503
roof=lambda y:z0+(y-y0)/(y1-y0)*(z1-z0)
changes=[]
for i in (2,3,4,23,24,25):
 v=housing.data.vertices[i];changes.append({'vertex':i,'before':list(v.co)});v.co.z=roof(v.co.y);changes[-1]['after']=list(v.co)
# Re-unwrap just this roof patch onto clean metal from the source; no blue zigzag
# remains baked beneath the actual conductor. One continuous UV patch, not tiles.
uv=housing.data.uv_layers.active
# Source tan barrel armor, not the pale receiver cap: avoid a new bright slab.
corners=((57,134),(53,130),(82,115),(86,119))
for idx in (3,4,5,6):
 p=housing.data.polygons[idx]
 for loop in p.loop_indices:
  v=housing.data.vertices[housing.data.loops[loop].vertex_index].co;t=(v.y-y0)/(y1-y0);x=(v.x+.74375)/1.2375
  a=[corners[0][k]*(1-x)+corners[1][k]*x for k in (0,1)];b=[corners[3][k]*(1-x)+corners[2][k]*x for k in (0,1)];u,w=[a[k]*(1-t)+b[k]*t for k in (0,1)];uv.data[loop].uv=(u/256,1-w/256)
housing.data.update()
roles=json.loads((BASE/'attempt-01/source-samples.json').read_text())
def material(name,key):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial')
 def lin(c):
  c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
 e.inputs[0].default_value=(*map(lin,roles[key][:3]),1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);return m
dark=material('V7 source channel shadow','112,150');pale=material('V7 source channel metal','156,88');cyan=material('V7 source cyan conductor','164,140')
def strip(name,xa,xb,lift,mat):
 # Closed solid following a single 3D line, centered on the longitudinal axis.
 top=[(xa,y0,roof(y0)+lift),(xb,y0,roof(y0)+lift),(xb,y1,roof(y1)+lift),(xa,y1,roof(y1)+lift)];verts=top+[(x,y,z-.025) for x,y,z in top]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]);mesh.materials.append(mat);mesh.update();o=bpy.data.objects.new(name,mesh);collection.objects.link(o);o.parent=pivot;o.location=-pivot.location
strip('V7 straight channel recess',-.325,.075,.018,dark)
strip('V7 straight left metal edge',-.325,-.265,.045,pale)
strip('V7 straight right metal edge',.015,.075,.045,pale)
strip('V7 straight cyan core',-.225,-.025,.047,cyan)
moving=[o for o in collection.objects if o.type=='MESH'];fixed=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH']
s.render.resolution_percentage=100;s.render.resolution_x=s.render.resolution_y=160
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v7.blend'))
for o in fixed:o.hide_render=True
for size in (160,192):
 folder=OUT/str(size);folder.mkdir(exist_ok=True);s.render.resolution_x=s.render.resolution_y=size
 for i in range(32):
  pivot.rotation_euler.z=math.radians(i*11.25);bpy.context.view_layer.update();s.render.filepath=str(folder/f'turret-{i:03d}-raw.png');bpy.ops.render.render(write_still=True)
(OUT/'geometry.json').write_text(json.dumps({'source_model':'attempt-05/heavy-blaster-v5.blend','sizes':[160,192],'facings':32,'changed_housing_vertices':changes,'changed_uv_faces':[3,4,5,6],'conduit_center_x':-.125,'conduit_endpoints':[[-.125,y0,z0+.047],[-.125,y1,z1+.047]],'base_changed':False,'muzzle_changed':False,'game_changed':False},indent=2)+'\n')
print('V7_STRAIGHT_CONDUIT_RENDER_COMPLETE')
