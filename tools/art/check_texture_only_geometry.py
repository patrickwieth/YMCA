"""Background Blender check: texture updates must preserve accepted mesh geometry."""
import bpy,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
PAIRS=(('nod laser turret',6,8,'nod-laser-model.blend'),('gdi aa gun',10,11,'aa-source-model.blend'),('gdi sonic turret',15,20,'sonic-source-model.blend'),('nod laser turret',9,10,'nod-laser-model.blend'))
def fingerprint(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));result={}
 for o in bpy.data.objects:
  if o.type!='MESH':continue
  data={'vertices':[list(v.co) for v in o.data.vertices],'polygons':[list(p.vertices) for p in o.data.polygons],'matrix':[list(row) for row in o.matrix_world]}
  result[o.name]=hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
 return result
for folder,old,new,name in PAIRS:
 base=ROOT/'art-source'/folder
 before=fingerprint(base/f'pass-{old:02d}'/name);after=fingerprint(base/f'pass-{new:02d}'/name)
 assert before==after,folder+': geometry changed'
 (base/f'pass-{new:02d}'/'texture-only-geometry-check.json').write_text(json.dumps({'baseline_pass':old,'current_pass':new,'identical':True,'mesh_fingerprints':after},indent=2)+'\n')
 print('TEXTURE_ONLY_GEOMETRY_IDENTICAL',folder,len(after))
# Pass09 is an explicitly requested muzzle correction, not a silent redesign.
base=ROOT/'art-source/nod laser turret'
before=fingerprint(base/'pass-08/nod-laser-model.blend');after=fingerprint(base/'pass-09/nod-laser-model.blend')
changed={name for name in before.keys()|after.keys() if before.get(name)!=after.get(name)}
allowed={'Octagonal muzzle armor','Circular muzzle with rectangular aperture','Muzzle black recess','Copper emitter bezel','Emitter dark rim','Recessed red emitter','Emitter bright core'}
assert changed==allowed,changed
(base/'pass-09/muzzle-only-geometry-check.json').write_text(json.dumps({'baseline_pass':8,'current_pass':9,'changed_objects':sorted(changed),'other_geometry_identical':True},indent=2)+'\n')
print('NOD_MUZZLE_ONLY_CHANGE_CONFIRMED')
