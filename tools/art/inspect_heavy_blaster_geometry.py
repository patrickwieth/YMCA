from pathlib import Path
import bpy,json
ROOT=Path(__file__).resolve().parents[2];P=ROOT/'art-source/gdi ion cannon/attempt-03/pass-05'
bpy.ops.wm.open_mainfile(filepath=str(P/'heavy-blaster-v3.blend'))
r={}
for name in ('near trunnion arm','far trunnion arm','trunnion cap','lower cross brace','closed barrel and receiver housing'):
 o=bpy.data.objects[name];r[name]={'verts':[list(v.co) for v in o.data.vertices],'faces':[list(f.vertices) for f in o.data.polygons]}
(P.parent/'geometry-inspection.json').write_text(json.dumps(r,indent=2))
