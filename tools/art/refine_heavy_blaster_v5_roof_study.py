"""V5: broader solid turret, native density appropriate to the existing 2x2 actor.
Preserves V4; no runtime scale or upscaled rendered PNG. Fixed base stays separate.
"""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-05/roof-study-rebuild';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-04/pass-05/heavy-blaster-v4.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
moving=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH'];fixed=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH'];records=[]
# Widen actual cross sections, not the image. Support spacing stays within the
# socket; the gun/receiver/muzzle get the stronger width correction.
for o in moving:
 factor=1.25 if o.name.startswith('V4 ') or o.name.startswith('capacitor ') else 1.65
 before=[min(v.co.x for v in o.data.vertices),max(v.co.x for v in o.data.vertices)]
 for v in o.data.vertices:v.co.x=-.125+(v.co.x+.125)*factor
 o.data.update();after=[min(v.co.x for v in o.data.vertices),max(v.co.x for v in o.data.vertices)]
 records.append({'object':o.name,'cross_section_scale':factor,'before_x':before,'after_x':after})
# The end-on review exposed the old source-projected cyan zigzag on the roof.
# Replace that projection with an actual continuous center conduit and coherent
# armor/channel UV regions on a smoothed physical roof.
collection=bpy.data.collections['ROTATING TURRET'];reference=bpy.data.materials['reference nearest'];housing=bpy.data.objects['closed barrel and receiver housing']
roles=json.loads((BASE/'attempt-01/source-samples.json').read_text())
def flat(name,key):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial')
 def lin(c):
  c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
 e.inputs[0].default_value=(*map(lin,roles[key][:3]),1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);return m
casing=flat('V5 source pale conduit casing','156,88');cyan=flat('V5 source cyan conductor','164,140');dark=flat('V5 source channel recess','112,150');tan=flat('V5 source roof armor','153,91')
def roof(y):return 2.87 if y<=-1.2 else (2.87+(y+1.2)*(.43/.85) if y<-.35 else 3.30)
# Vertex IDs of both extruded profile ends are preserved by the muzzle opening.
for i in list(range(0,8))+list(range(21,29)):
 v=housing.data.vertices[i];v.co.z=roof(v.co.y)
housing.data.materials.append(tan);tan_index=len(housing.data.materials)-1
for i in range(2,9):housing.data.polygons[i].material_index=tan_index
housing.data.update()
def panel(name,x0,x1,y0,y1,mat,pixels=None,lift=.012):
 verts=[(x0,y0,roof(y0)+lift),(x0,y1,roof(y1)+lift),(x1,y1,roof(y1)+lift),(x1,y0,roof(y0)+lift)]
 d=bpy.data.meshes.new(name);d.from_pydata(verts,[],[(0,1,2,3)]);d.update();o=bpy.data.objects.new(name,d);collection.objects.link(o);o.parent=pivot;o.location=-pivot.location;d.materials.append(mat)
 if pixels:
  uv=d.uv_layers.new()
  for loop,(u,v) in zip(d.polygons[0].loop_indices,pixels):uv.data[loop].uv=(u/256,1-v/256)
 moving.append(o)
# Side faces keep source-detail density. Roof rails are separately unwrapped;
# the short source strip is not smeared across the receiver and muzzle.
for x0,x1 in ((-.74375,-.235),(-.015,.49375)):
 panel('V5 source roof rail',x0,x1,-3.397,-1.2,reference,[(38,119),(91,92),(100,97),(44,126)])
panel('V5 recessed top channel',-.235,-.015,-3.397,-1.2,dark)
panel('V5 source channel metal',-.20,-.05,-3.30,-1.75,reference,[(36,111),(81,88),(85,90),(40,114)],.015)
panel('V5 complete source receiver roof',-.74375,.49375,-.35,2.225,reference,[(115,51),(176,23),(198,34),(137,62)],.015)
panel('V5 source sloped armor',-.74375,.49375,-1.2,-.35,reference,[(119,71),(121,59),(129,63),(126,74)],.015)
for y0,y1 in ((-1.75,-1.2),(-1.2,-.35),(-.35,.10)):
 panel('V5 conduit dark channel boundary',-.28,.03,y0,y1,dark,lift=.032)
 panel('V5 actual center conduit casing',-.255,.005,y0,y1,casing,lift=.035)
 panel('V5 actual center cyan conductor',-.20,-.05,y0,y1,reference,[(48,141),(99,115),(101,117),(49,145)],lift=.045)
s.render.resolution_x=s.render.resolution_y=320;s.render.resolution_percentage=100
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v5.blend'))
def render(name,angle=0,part='turret'):
 pivot.rotation_euler.z=math.radians(angle)
 for o in moving:o.hide_render=part=='base'
 for o in fixed:o.hide_render=part=='turret'
 bpy.context.view_layer.update();s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('base-modeled-raw.png',part='base');render('base-control-raw.png',123.75,'base')
for i in range(32):render(f'turret-{i:03d}-raw.png',i*11.25)
render('assembly-modeled-raw.png',part='assembly')
(OUT/'geometry.json').write_text(json.dumps({'source_model':'attempt-04/pass-05/heavy-blaster-v4.blend','frame_size':[320,320],'camera_ortho':8,'native_pixels_per_world_unit':40,'gun_width_factor':1.65,'support_spacing_factor':1.25,'runtime_scale':1,'ground_footprint':'existing 2x2 unchanged','width_changes':records,'base':'stationary rendered underlay; visible original perimeter restored by pack script','muzzle_center':[-.125,-3.405,2.4225],'pivot_world':list(pivot.location),'pivot_in_frame':[162.12132042785334,179.15509498712942]},indent=2)+'\n')
print('HEAVY_BLASTER_V5_RENDER_COMPLETE')
