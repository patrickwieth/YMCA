"""Read-only swept mesh intersections; the tall fixed battery invalidates a Z-gap-only test."""
from pathlib import Path
import bpy,sys,math,json
from mathutils import Matrix
from mathutils.bvhtree import BVHTree
from collections import Counter
stage=int(sys.argv[sys.argv.index('--')+1]);root=Path(__file__).resolve().parents[2];p=root/f'art-source/scrin shard launcher/pass-{stage:02d}'
bpy.ops.wm.open_mainfile(filepath=str(p/'shard-launcher.blend'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];fixed=[o for o in objects if o['fixed']];moving=[o for o in objects if not o['fixed']]
low=min(v.co.z for o in moving for v in o.data.vertices);high=max(v.co.z for o in fixed for v in o.data.vertices)
def data(parts):
 vs=[];fs=[];names=[]
 for o in parts:
  start=len(vs);vs.extend(v.co.copy() for v in o.data.vertices)
  for face in o.data.polygons:
   z=[o.data.vertices[i].co.z for i in face.vertices]
   if max(z)<low-.001 or min(z)>high+.001:continue
   fs.append(tuple(start+i for i in face.vertices));names.append(o.name)
 return vs,fs,names
fv,ff,fn=data(fixed);mv,mf,mn=data(moving);tree=BVHTree.FromPolygons(fv,ff,epsilon=0);hits=[]
for i in range(64):
 rot=Matrix.Rotation(i*math.tau/64,3,'Z');other=BVHTree.FromPolygons([rot@v for v in mv],mf,epsilon=0);overlap=tree.overlap(other);counts=Counter((fn[a],mn[b]) for a,b in overlap)
 if counts:hits.append({'angle':i*360/64,'pairs':[{'fixed':a,'moving':b,'faces':n} for (a,b),n in counts.most_common()]})
report={'pass':stage,'samples':64,'fixed_battery_meshes':sum(bool(o.get('fixed_battery')) for o in fixed),'collisions':hits}
(p/'swept-collision-audit.json').write_text(json.dumps(report,indent=2)+'\n');print('SWEPT_COLLISIONS',len(hits));print(json.dumps(hits[:2],indent=2))
