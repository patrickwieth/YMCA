"""Read-only projection of both barrel-cluster centers from the frozen native Blender model."""
from pathlib import Path
import bpy,json,math
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet-sentry/dual-muzzles';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art-source/soviet-sentry/game-v13-32-native/render.blend'))
s=bpy.context.scene;p=bpy.data.objects['turret_pivot'];inv=p.matrix_world.inverted()
bores=[o for o in s.objects if o.name.startswith('recessed_muzzle_bore')]
groups=[[o for o in bores if (inv@o.matrix_world.translation).x<0],[o for o in bores if (inv@o.matrix_world.translation).x>=0]]
assert len(groups[0])==len(groups[1]) and len(groups[0])>0
points=[]
for i in range(32):
 p.rotation_euler.z=math.radians(((20+i)%32)*11.25);bpy.context.view_layer.update();row=[]
 for group in groups:
  center=sum((o.matrix_world.translation for o in group),Vector())/len(group)
  v=world_to_camera_view(s,s.camera,center)
  row.append([v.x*120-60-4,(1-v.y)*120-60-24-112*64/1448])
 points.append(row)
(OUT/'muzzle-pixels.json').write_text(json.dumps({'bores_per_cluster':list(map(len,groups)),'points':points,'source_blend':'art-source/soviet-sentry/game-v13-32-native/render.blend','source_modified':False},indent=2)+'\n')
print('SENTRY_DUAL_MUZZLES',list(map(len,groups)),points[0])
