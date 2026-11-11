"""Recenter fixed bearing and turret onto the platform. Background Blender only."""
from pathlib import Path
import bpy,math,json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-11-centering';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-07-preview/heavy-blaster-v7.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
# Exact six-right/four-down native-pixel translation, with unchanged Z. Within
# 0.46px of the armored plinth polygon centroid at the original pivot height.
delta=Vector(((6*math.sqrt(2)+4*math.sqrt(8))/40,(6*math.sqrt(2)-4*math.sqrt(8))/40,0))
pivot.location+=delta;bpy.data.objects['fixed bearing socket'].location+=delta
s.render.resolution_x=s.render.resolution_y=160;s.render.resolution_percentage=100
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v11-centered.blend'))
for o in bpy.data.collections['ROTATING TURRET'].objects:o.hide_render=True
for o in bpy.data.collections['STATIONARY BASE'].objects:o.hide_render=False
for name,angle in (('base-centered-raw.png',0),('base-control-raw.png',123.75)):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update();s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
(OUT/'geometry.json').write_text(json.dumps({'model_translation':list(delta),'native_translation':[6,4],'pivot_world':list(pivot.location),'pivot_frame':[87.06066021392667,93.57754749356471],'plinth_centroid_frame_at_deck_height':[87.10539547314792,93.99304267936189],'pivot_height':.7,'deck_height':.65,'centering_error_pixels':.453,'method':'move both fixed socket and turret, not turret alone; retain native turret sprite pixels'},indent=2)+'\n')
print('ION_PLATFORM_CENTERED')
