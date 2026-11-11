"""Project the existing straight core for source-palette glow finishing; no renders."""
from pathlib import Path
import bpy,json,math
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-10-preview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-07-preview/heavy-blaster-v7.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];obj=bpy.data.objects['V7 straight cyan core'];result={}
for i in range(32):
 pivot.rotation_euler.z=math.radians(i*11.25);bpy.context.view_layer.update();points=[]
 for index in (0,1,3):
  p=world_to_camera_view(s,s.camera,obj.matrix_world@obj.data.vertices[index].co);points.append([p.x,1-p.y])
 result[i]=points
(OUT/'conduit-projection.json').write_text(json.dumps(result,indent=2)+'\n');print('CONDUIT_PROJECTION_READY')
