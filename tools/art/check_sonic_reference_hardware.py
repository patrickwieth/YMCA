"""Verify pass22 only adds requested hardware/replaces the small radar study."""
import bpy,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]/'art-source/gdi sonic turret'
def fingerprint(path):
    bpy.ops.wm.open_mainfile(filepath=str(path));out={}
    for o in bpy.data.objects:
        if o.type!='MESH':continue
        data={'vertices':[list(v.co) for v in o.data.vertices],'faces':[list(p.vertices) for p in o.data.polygons],'matrix':[list(row) for row in o.matrix_world]}
        out[o.name]=hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
    return out
before=fingerprint(ROOT/'pass-20/sonic-source-model.blend');after=fingerprint(ROOT/'pass-22/sonic-source-model.blend')
removed=set(before)-set(after)
assert removed=={'SONIC radar pedestal','SONIC radar ring','SONIC radar inset'},removed
assert all(before[n]==after[n] for n in before.keys()&after.keys())
added=set(after)-set(before)
assert all(n.startswith(('SONIC reference','SONIC fixed','SONIC return pipe coupling','SONIC receiver hanging cable','SONIC twin pipe anchor')) for n in added),added
for n in added:
    o=bpy.data.objects[n]
    assert (o.parent is None)==n.startswith('SONIC fixed'),n
report={'baseline':20,'current':22,'retained_meshes_identical':True,'retained_meshes':len(before)-len(removed),'removed':sorted(removed),'added':sorted(added),'fixed_cables_stationary':True,'large_emitters_unchanged':True}
(ROOT/'pass-22/hardware-geometry-check.json').write_text(json.dumps(report,indent=2)+'\n')
print('SONIC_REFERENCE_HARDWARE_SCOPED',len(added),'new meshes;',report['retained_meshes'],'preserved')
