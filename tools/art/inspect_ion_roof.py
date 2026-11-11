from pathlib import Path
import bpy,json
base=Path(__file__).resolve().parents[2]/'art-source/gdi ion cannon'
bpy.ops.wm.open_mainfile(filepath=str(base/'attempt-05/heavy-blaster-v5.blend'))
o=bpy.data.objects['closed barrel and receiver housing']
print('ROOF_DATA',json.dumps([{'i':p.index,'verts':[list(o.data.vertices[i].co) for i in p.vertices],'mat':p.material_index} for p in o.data.polygons[:10]]))
print('OBJECTS',list(bpy.data.objects.keys()))
