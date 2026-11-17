"""Background-Blender regression for flak geometry, not an artistic approval."""
import bpy,bmesh,json,math
from collections import Counter
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
STUDIES=(('allies flak',13,'allied-flak-model.blend','Hollow flak barrel ',4),('soviet flak',11,'soviet-flak-model.blend','Hollow main barrel',1))
for folder,iteration,file,prefix,count in STUDIES:
 path=ROOT/'art-source'/folder/f'pass-{iteration:02d}';bpy.ops.wm.open_mainfile(filepath=str(path/file));meshes=[o for o in bpy.data.objects if o.type=='MESH'];closed=[];fixed=[]
 for o in meshes:
  assert (o.parent is None)==bool(o['fixed']),o.name
  if o['fixed']:fixed += [o.matrix_world@v.co for v in o.data.vertices]
  uses=Counter(tuple(sorted(edge)) for polygon in o.data.polygons for edge in polygon.edge_keys)
  if uses and set(uses.values())=={2}:
   bm=bmesh.new();bm.from_mesh(o.data);volume=bm.calc_volume(signed=True);bm.free();assert volume>1e-9,(o.name,volume);closed.append(o.name)
 extent=max(max(abs(v.x),abs(v.y)) for v in fixed)*20*math.sqrt(2)
 assert extent<64,(folder,extent)
 barrels=[o for o in meshes if o.name.startswith(prefix)];assert len(barrels)==count
 bore=[]
 for o in barrels:
  vs=[v.co.copy() for v in o.data.vertices];assert len(vs)%4==0;n=len(vs)//4
  a=sum(vs[:n],Vector())/n;b=sum(vs[n:2*n],Vector())/n;axis=(b-a).normalized()
  outer=sum((v-a).length for v in vs[:n])/n;inner=sum((v-a).length for v in vs[2*n:3*n])/n
  assert .025<inner<outer,(o.name,inner,outer)
  bore.append({'mesh':o.name,'inner_radius':inner,'outer_radius':outer,'length':(b-a).length})
 report={'pass':iteration,'closed_meshes_outward':len(closed),'fixed_and_moving_parentage':True,'ground_diamond_limit':64,'ground_diamond_max':extent,'hollow_barrels':bore,'artistic_approval':False,'game_installed':False}
 (path/'geometry-check.json').write_text(json.dumps(report,indent=2)+'\n');print('FLAK_GEOMETRY_CHECKED',folder,iteration,extent)
