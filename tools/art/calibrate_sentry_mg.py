"""Read-only background Blender muzzle projection for the installed native sprite."""
from pathlib import Path
import bpy,json,math
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet-sentry/mg-integration';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art-source/soviet-sentry/game-v13-32-native/render.blend'))
s=bpy.context.scene;p=bpy.data.objects['turret_pivot']
bores=[o for o in s.objects if o.name.startswith('recessed_muzzle_bore')]
assert bores
points=[]
for i in range(32):
 p.rotation_euler.z=math.radians(((20+i)%32)*11.25);bpy.context.view_layer.update()
 center=sum((o.matrix_world.translation for o in bores),Vector())/len(bores)
 v=world_to_camera_view(s,s.camera,center)
 points.append([v.x*120-60-4,(1-v.y)*120-60-24-112*64/1448])
(OUT/'muzzle-pixels.json').write_text(json.dumps(points,indent=2)+'\n')
print('SENTRY_MG_MUZZLE_PROJECTED',len(bores))
