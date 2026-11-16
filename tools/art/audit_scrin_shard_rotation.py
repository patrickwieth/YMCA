"""Read-only audit of the saved final Blender model's actual parentage and swept envelopes."""
from pathlib import Path
import bpy,json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';path=ART/'pass-22/shard-launcher.blend';bpy.ops.wm.open_mainfile(filepath=str(path));pivot=bpy.data.objects['turret_pivot'];parts=[o for o in bpy.context.scene.objects if o.type=='MESH'];fixed=[o for o in parts if o['fixed']];moving=[o for o in parts if not o['fixed']]
assert all(o.parent is None for o in fixed);assert all(o.parent==pivot for o in moving)
original={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in fixed};fixed_top=max(v[2] for data in original.values() for v in data);low=1e9
for i in range(32):
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update()
 for o in fixed:assert [tuple(o.matrix_world@v.co) for v in o.data.vertices]==original[o.name]
 low=min(low,min((o.matrix_world@v.co).z for o in moving for v in o.data.vertices))
core_top=max(v[2] for o in fixed if not o.get('fixed_battery') for v in original[o.name]);assert low-core_top>.1
collision=json.loads((ART/'pass-22/swept-collision-audit.json').read_text());assert collision['samples']>=64 and not collision['collisions']
assert len([o for o in fixed if o.name.startswith('Magazine shard')])==4
from mathutils import Vector
crystal=bpy.data.objects['Long faceted cyan shard'];axis=crystal.data.vertices[-1].co-sum((v.co for v in crystal.data.vertices[:5]),Vector())/5
pitch=math.degrees(math.atan2(axis.z,math.hypot(axis.x,axis.y)));assert 24.9<pitch<25.1
report={'blend_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'fixed_meshes':len(fixed),'moving_meshes':len(moving),'angles_checked':32,'measured_cannon_elevation_degrees':pitch,'fixed_top':fixed_top,'moving_bottom':low,'bearing_clearance':low-core_top,'swept_collision_samples':collision['samples'],'swept_collisions':0,'stationary_crystals':4,'all_fixed_vertices_unchanged':True,'all_moving_parts_parented_to_yaw':True,'original_blend_modified':False}
(ART/'integration-pass22/separation-audit.json').write_text(json.dumps(report,indent=2)+'\n');print('SCRIN_SHARD_ROTATION_AUDIT',report)
