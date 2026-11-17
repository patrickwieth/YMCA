"""Plan/apply object-local bounded gold atlases; texture-only, no transform edits."""
from pathlib import Path
import bpy, json, sys, argparse, math, hashlib
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--apply',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
R=Path(__file__).resolve().parents[2];F=R/'art-source/gdi missile tower';O=F/f'pass-{a.iteration:02d}';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(F/'pass-14/turret.blend'))
def geometry():
 return {o.name:{'vertices':[list(v.co) for v in o.data.vertices],'faces':[list(f.vertices) for f in o.data.polygons],'matrix':[list(row) for row in o.matrix_world]} for o in bpy.context.scene.objects if o.type=='MESH'}
before=geometry();plan=[]
for ob in bpy.context.scene.objects:
 if ob.type!='MESH':continue
 m=ob.data
 for mi,mat in enumerate(list(m.materials)):
  if not mat or not mat.name.startswith('CUT / '):continue
  role=mat.name.split('CUT / ',1)[1]
  if role not in ('gold','goldlight','goldshade'):continue
  faces=[f for f in m.polygons if f.material_index==mi]
  if not faces:continue
  tex=next(n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE').image
  w,h=tex.size
  loops=[i for f in faces for i in f.loop_indices]
  coords=[(m.uv_layers.active.data[i].uv.x*w,m.uv_layers.active.data[i].uv.y*h) for i in loops]
  lo=[math.floor(min(c[i] for c in coords))-1 for i in (0,1)];hi=[math.ceil(max(c[i] for c in coords))+1 for i in (0,1)]
  size=[max(2,hi[i]-lo[i]) for i in (0,1)];name=f'panel-{len(plan):03d}'
  regions=[[[m.uv_layers.active.data[i].uv.x*w-lo[0],size[1]-1-(m.uv_layers.active.data[i].uv.y*h-lo[1])] for i in f.loop_indices] for f in faces]
  plan.append({'object':ob.name,'role':role,'name':name,'old_size':[w,h],'origin':lo,'size':size,'regions':regions})
  if not a.apply:continue
  image=bpy.data.images.load(str(O/'source-cuts'/f'{name}.png'));image.pack()
  new=mat.copy();new.name='CUT / '+name
  node=next(n for n in new.node_tree.nodes if n.type=='TEX_IMAGE');node.image=image;node.extension='EXTEND';node.interpolation='Closest'
  m.materials.append(new);index=len(m.materials)-1
  for f in faces:
   f.material_index=index
   for i in f.loop_indices:
    uv=m.uv_layers.active.data[i].uv;uv.x=(uv.x*w-lo[0])/size[0];uv.y=(uv.y*h-lo[1])/size[1]
assert before==geometry(),'Texture operation changed geometry or transforms'
(O/'texture-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
if a.apply:
 bpy.ops.wm.save_as_mainfile(filepath=str(O/'turret.blend'))
 (O/'geometry-preservation.json').write_text(json.dumps({'source_pass':14,'identical_vertices_faces_and_world_matrices':True,'mesh_count':len(before),'sha256':hashlib.sha256(json.dumps(before,sort_keys=True).encode()).hexdigest()},indent=2)+'\n')
print('TEXTURE_ONLY',len(plan),'atlases',flush=True)
