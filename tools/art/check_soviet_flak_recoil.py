"""Background Blender: prove rest geometry preservation and exclusively axial recoil."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]/'art-source/soviet flak'
def snapshot():
 return {o.name:([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],o.matrix_world.copy()) for o in bpy.data.objects if o.type=='MESH'}
bpy.ops.wm.open_mainfile(filepath=str(root/'pass-10/soviet-flak-model.blend'));old=snapshot()
p=root/'pass-11';bpy.ops.wm.open_mainfile(filepath=str(p/'soviet-flak-model.blend'));s=bpy.context.scene;s.frame_set(0);bpy.context.view_layer.update();new=snapshot();assert set(old)==set(new)
for name in old:
 assert old[name][:2]==new[name][:2],name
 assert max(abs(old[name][2][i][j]-new[name][2][i][j]) for i in range(4) for j in range(4))<1e-6,name
info=json.loads((p/'recoil.json').read_text());members=set(info['barrel_members']);axis=Vector(info['axis_local']);pivot=bpy.data.objects['turret_pivot'];error=0
for facing in range(32):
 pivot.rotation_euler.z=facing*math.pi/16;s.frame_set(0);bpy.context.view_layer.update();rest=snapshot();s.frame_set(1);bpy.context.view_layer.update()
 delta=Matrix.Rotation(facing*math.pi/16,3,'Z')@(-axis*.24)
 for o in bpy.data.objects:
  if o.type!='MESH':continue
  expected=delta if o.name in members else Vector()
  for v in o.data.vertices:error=max(error,((o.matrix_world@v.co)-(rest[o.name][2]@v.co)-expected).length)
assert error<1e-5,error
s.frame_set(14);bpy.context.view_layer.update();assert bpy.data.objects['barrel_recoil'].location.length<1e-8
report={'rest_geometry_exact':True,'meshes_preserved':len(old),'facings_checked':32,'max_axial_error':error,'only_barrel_group_moves':True,'returns_to_rest':True,'runtime_integrated':False}
(p/'recoil-check.json').write_text(json.dumps(report,indent=2)+'\n');print('SOVIET_RECOIL_CHECKED',report)
