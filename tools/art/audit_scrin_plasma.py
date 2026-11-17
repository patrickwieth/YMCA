"""Read-only actual saved-model parentage, full rotation and separated envelopes."""
from pathlib import Path
import bpy,json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin plasma';path=ART/'pass-05/plasma-cannon.blend';bpy.ops.wm.open_mainfile(filepath=str(path));pivot=bpy.data.objects['turret_pivot'];parts=[o for o in bpy.context.scene.objects if o.type=='MESH'];fixed=[o for o in parts if o['fixed']];moving=[o for o in parts if not o['fixed']]
assert all(o.parent is None for o in fixed);assert all(o.parent==pivot for o in moving)
original={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in fixed};top=max(p[2] for points in original.values() for p in points);bottom=1e9
for i in range(32):
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update()
 for o in fixed:assert [tuple(o.matrix_world@v.co) for v in o.data.vertices]==original[o.name]
 bottom=min(bottom,min((o.matrix_world@v.co).z for o in moving for v in o.data.vertices))
assert bottom-top>.1
report={'blend_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'fixed_meshes':len(fixed),'moving_meshes':len(moving),'angles_checked':32,'fixed_top':top,'moving_bottom':bottom,'vertical_clearance':bottom-top,'fixed_vertices_unchanged':True,'all_upper_parts_parented_to_yaw':True,'original_blend_modified':False}
(ART/'integration-pass05/separation-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(report)
