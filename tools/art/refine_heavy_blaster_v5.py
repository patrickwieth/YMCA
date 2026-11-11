"""Selected V5: broader solid turret at native density for the existing 2x2 actor.
Roof/UV rewrites were rejected after review; see preserved roof-study script.
Original V4 and all source PNGs stay untouched. No runtime sprite scaling.
"""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-05';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-04/pass-05/heavy-blaster-v4.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
# V3's supposedly visible left deck patch also sampled a piece of the barrel.
# Remove it from the hidden underlay; the source-cut mask restores real floor.
old_patch=bpy.data.objects.get('base left visible plating')
if old_patch:bpy.data.objects.remove(old_patch,do_unlink=True)
moving=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH'];fixed=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH'];records=[]
for o in moving:
 factor=1.25 if o.name.startswith('V4 ') or o.name.startswith('capacitor ') else 1.65
 before=[min(v.co.x for v in o.data.vertices),max(v.co.x for v in o.data.vertices)]
 for v in o.data.vertices:v.co.x=-.125+(v.co.x+.125)*factor
 o.data.update();after=[min(v.co.x for v in o.data.vertices),max(v.co.x for v in o.data.vertices)]
 records.append({'object':o.name,'cross_section_scale':factor,'before_x':before,'after_x':after})
# Retain observed source detail; stretching the original front/rear cross section
# gives genuinely broader solids. UVs on the Y/Z side armor keep their density.
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
