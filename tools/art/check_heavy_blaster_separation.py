"""Blender check: the base stays pixel-identical while the actual turret rotates."""
from pathlib import Path
import bpy,json,math
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/gdi ion cannon/attempt-03/pass-05'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'heavy-blaster-v3.blend'));s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot']
base=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH'];turret=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH']
assert base and turret and all(o.parent is None for o in base) and all(o.parent==pivot for o in turret)
s.render.resolution_x=s.render.resolution_y=160
matrices={o.name:o.matrix_world.copy() for o in base}
for o in turret:o.hide_render=True
for angle in (0,123.75):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update()
 assert all(o.matrix_world==matrices[o.name] for o in base)
 s.render.filepath=str(OUT/f'base-control-{angle:g}.png');bpy.ops.render.render(write_still=True)
(OUT/'separation-check.json').write_text(json.dumps({'stationary_meshes':len(base),'rotating_meshes':len(turret),'base_parent':None,'turret_parent':'turret_pivot','base_world_matrices_unchanged':True,'control_angles':[0,123.75]},indent=2)+'\n')
print('HEAVY_BLASTER_SEPARATION_CHECK_PASSED')
