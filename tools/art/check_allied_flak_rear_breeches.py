"""Background Blender: pass15 changes only the requested barrel/rear breech group."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]/'art-source/allies flak'
def snapshot(p):
 bpy.ops.wm.open_mainfile(filepath=str(root/f'pass-{p:02d}/allied-flak-model.blend'))
 return {o.name:([v.co.copy() for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],o.matrix_world.copy()) for o in bpy.data.objects if o.type=='MESH'}
a=snapshot(14);b=snapshot(15);assert a.keys()==b.keys();axis=Vector((0,-math.cos(math.radians(37)),math.sin(math.radians(37))))
group=('Hollow flak barrel','Stepped barrel jacket','Visible muzzle rim','Independent breech sleeve','Individual rear breech cylinder','Breech rear machined cap','Breech locking collar','Breech return sleeve','Breech return bright rod','Breech upper locking stud')
changed=[];error=0
for name,(vertices,faces,matrix) in a.items():
 after=b[name];assert faces==after[1] and matrix==after[2],name
 center=sum(vertices,Vector())/len(vertices)
 if name.startswith(group):changed.append(name)
 for old,new in zip(vertices,after[0]):
  travel=.28 if name.startswith(group) else 0
  if name.startswith('Breech rear machined cap') or name.startswith('Individual rear breech cylinder') and (old-center).dot(axis)<0:travel+=.32
  error=max(error,(new-old+axis*travel).length)
assert error<1e-6,error
assert (root/'pass-14/review/base.png').read_bytes()==(root/'pass-15/review/base.png').read_bytes()
report={'baseline':14,'current':15,'barrel_group_aft':.28,'additional_rear_extension':.32,'changed_meshes':changed,'max_error':error,'other_meshes_and_base_unchanged':True}
(root/'pass-15/rear-breech-check.json').write_text(json.dumps(report,indent=2)+'\n');print('ALLIED_REAR_BREECH_CHECKED',len(changed),error)
