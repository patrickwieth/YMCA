"""Independent inspection of the saved background-Blender Nod study."""
from pathlib import Path
import bpy,json,math,hashlib
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];path=ROOT/'art-source/nod laser turret/pass-04';blend=path/'nod-laser-model.blend';bpy.ops.wm.open_mainfile(filepath=str(blend));s=bpy.context.scene;fixed=list(bpy.data.collections['STATIONARY BASE'].objects);moving=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH'];pivot=bpy.data.objects['turret_pivot']
assert len(fixed)==3 and len(moving)>80;assert all(o.parent is None for o in fixed);assert all(o.parent==pivot for o in moving);assert all(o.data.uv_layers.active for o in fixed+moving)
before={o.name:tuple(v for row in o.matrix_world for v in row) for o in fixed};extent=max(max(abs(p.x),abs(p.y)) for o in fixed for v in o.data.vertices for p in [o.matrix_world@v.co]);assert extent<64/(20*math.sqrt(2))
lo=[1e9,1e9];hi=[-1e9,-1e9]
for i in range(32):
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update();assert before=={o.name:tuple(v for row in o.matrix_world for v in row) for o in fixed}
 for o in fixed+moving:
  for v in o.data.vertices:
   p=world_to_camera_view(s,s.camera,o.matrix_world@v.co);xy=(p.x*256,(1-p.y)*256)
   for j in range(2):lo[j]=min(lo[j],xy[j]);hi[j]=max(hi[j],xy[j])
assert min(lo)>1 and max(hi)<255
report={'blend_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'fixed_meshes':len(fixed),'rotating_meshes':len(moving),'vertices':sum(len(o.data.vertices) for o in fixed+moving),'faces':sum(len(o.data.polygons) for o in fixed+moving),'parts_parented_separately':True,'stationary_matrices_unchanged':True,'fixed_xy_extent':extent,'one_cell_xy_limit':64/(20*math.sqrt(2)),'projected_vertex_min':lo,'projected_vertex_max':hi,'all_meshes_uv_mapped':True};(path/'geometry-check.json').write_text(json.dumps(report,indent=2)+'\n');print('NOD_LASER_GEOMETRY_PASSED',json.dumps(report))
