"""Preview only: retain the broad V5 solids, directly render smaller native sizes.
Does not install files, modify rules or touch the user's open Blender session.
"""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-06-preview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-05/heavy-blaster-v5.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
moving=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH'];fixed=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH']
for size in (160,192):
 folder=OUT/str(size);folder.mkdir(exist_ok=True);s.render.resolution_x=s.render.resolution_y=size;s.render.resolution_percentage=100
 for o in fixed:o.hide_render=False
 for o in moving:o.hide_render=True
 s.render.filepath=str(folder/'base-modeled-raw.png');bpy.ops.render.render(write_still=True)
 for o in fixed:o.hide_render=True
 for o in moving:o.hide_render=False
 for i in (range(32) if size==160 else (0,4,8,12,16,20,24,28)):
  pivot.rotation_euler.z=math.radians(i*11.25);bpy.context.view_layer.update();s.render.filepath=str(folder/f'turret-{i:03d}-raw.png');bpy.ops.render.render(write_still=True)
(OUT/'render.json').write_text(json.dumps({'source_model':'attempt-05/heavy-blaster-v5.blend','physical_gun_width_factor_vs_v4':1.65,'sizes':[160,192],'camera_ortho':8,'native_render':True,'sprite_resampling':False,'game_changed':False,'purpose':'size preview, not a claim of finished model/UV quality'},indent=2)+'\n')
print('SMALLER_ION_PREVIEW_RENDERED')
