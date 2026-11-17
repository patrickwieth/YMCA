"""Inspect actual saved Blender geometry, not just the render script's claims."""
from pathlib import Path
import bpy,json,math,hashlib
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];reports={}
for kind,folder in (('aa','gdi aa gun'),('sonic','gdi sonic turret')):
 path=ROOT/'art-source'/folder/'pass-05';blend=path/(kind+'-source-model.blend');bpy.ops.wm.open_mainfile(filepath=str(blend));s=bpy.context.scene;fixed=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH'];moving=[o for o in bpy.data.collections['ROTATING TURRET'].objects if o.type=='MESH'];pivot=bpy.data.objects['turret_pivot']
 assert fixed and moving and all(o.parent is None for o in fixed) and all(o.parent==pivot for o in moving)
 before={o.name:tuple(v for row in o.matrix_world for v in row) for o in fixed};pivot.rotation_euler.z=math.radians(123.75);bpy.context.view_layer.update();after={o.name:tuple(v for row in o.matrix_world for v in row) for o in fixed};assert before==after
 extent=max(max(abs(v.x),abs(v.y)) for o in fixed for vert in o.data.vertices for v in [o.matrix_world@vert.co]);limit=64/(20*math.sqrt(2));assert extent<limit,(kind,extent,limit)
 assert all(o.data.uv_layers.active is not None for o in fixed+moving)
 if kind=='aa':
  barrels=[o for o in moving if o.name.startswith('AA hollow barrel')];assert len(barrels)==6;assert all(len(o.data.vertices)==48 for o in barrels) # four 12-vertex rings, real inner walls
 else:
  assert len([o for o in moving if o.name.startswith('SONIC wave emitter collar')])==3
  assert bpy.data.objects['SONIC recessed cyan lens'].parent==pivot
 frames=[];start=4 if kind=='aa' else 12
 for i in range(32):
  theta=(start+i)*math.pi/16
  x,y=(-math.sin(theta),math.cos(theta)) if kind=='aa' else (math.cos(theta),math.sin(theta))
  dx=(x+y)/math.sqrt(2);dy=(x-y)/math.sqrt(8);yaw=math.atan2(-dx,-2*dy)*1024/(2*math.pi)%1024;error=abs((yaw-i*32+512)%1024-512);assert error<1e-6;frames.append({'engine_frame':i,'render_index':(start+i)%32,'yaw_error':error})
 reports[kind]={'blend_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'fixed_meshes':len(fixed),'rotating_meshes':len(moving),'vertices':sum(len(o.data.vertices) for o in fixed+moving),'faces':sum(len(o.data.polygons) for o in fixed+moving),'fixed_xy_extent':extent,'one_cell_xy_limit':limit,'fixed_matrices_unchanged':True,'parts_parented_separately':True,'all_meshes_uv_mapped':True,'facings':frames}
 # Full 3D controls at difficult yaw angles validate the independent layer export.
 for idx in (4,12,20,28):
  pivot.rotation_euler.z=idx*math.pi/16;bpy.context.view_layer.update();s.render.filepath=str(path/f'assembly-control-{idx:03d}-raw.png');bpy.ops.render.render(write_still=True)
 (path/'geometry-check.json').write_text(json.dumps(reports[kind],indent=2)+'\n')
print('GDI_SMALL_TURRET_GEOMETRY_PASSED',json.dumps({k:{a:b for a,b in v.items() if a!='facings'} for k,v in reports.items()}))
