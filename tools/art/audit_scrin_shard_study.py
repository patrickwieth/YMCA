"""Read-only saved scene/rotation audit for the new, deeply recessed collar study."""
from pathlib import Path
import bpy,json,math,hashlib,sys
from mathutils import Vector
stage=int(sys.argv[sys.argv.index('--')+1]);root=Path(__file__).resolve().parents[2];art=root/'art-source/scrin shard launcher';p=art/f'pass-{stage:02d}'
def geometry(path):
 bpy.ops.wm.open_mainfile(filepath=str(path))
 return {o.name:([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons]) for o in bpy.context.scene.objects if o.type=='MESH'}
reference_stage=30 if stage>=31 else 29 if stage==30 else None
previous=geometry(art/f'pass-{reference_stage:02d}/shard-launcher.blend') if reference_stage is not None else None
current=geometry(p/'shard-launcher.blend');same=previous==current if previous is not None else None
if previous is not None:assert same,'Material-only revision changed geometry'
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];fixed=[o for o in objects if o['fixed']];moving=[o for o in objects if not o['fixed']];pivot=bpy.data.objects['turret_pivot'];assert all(o.parent==pivot for o in moving);assert all(o.parent is None for o in fixed)
original={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in fixed}
shards=[o for o in fixed if o.name.startswith('Magazine shard ')];assert len(shards)==4
core=bpy.data.objects['Long faceted cyan shard'];start=sum((core.data.vertices[i].co for i in range(5)),Vector())/5;end=core.data.vertices[-1].co;axis=end-start;angle=math.degrees(math.atan2(axis.z,math.hypot(axis.x,axis.y)));assert abs(angle-25)<.03,angle
for i in range(64):
 pivot.rotation_euler.z=i*math.tau/64;bpy.context.view_layer.update()
 for o in fixed:assert [tuple(o.matrix_world@v.co) for v in o.data.vertices]==original[o.name]
report={'pass':stage,'fixed_vertices_unchanged':True,'fixed_shards':4,'all_moving_parts_yaw_parented':True,'measured_bore_degrees':angle,'material_only_geometry_unchanged':same,'geometry_reference_pass':reference_stage,'blend_sha256':hashlib.sha256((p/'shard-launcher.blend').read_bytes()).hexdigest(),'source_sha256':hashlib.sha256((art/'shard_launcher-332414650-0.png').read_bytes()).hexdigest(),'runtime_installed':False,'source_cut_face_counts':{o.name:len(o.get('texture_faces',[])) for o in objects if o.get('texture_faces',[])}}
(p/'saved-scene-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='source_cut_face_counts'},indent=2))
