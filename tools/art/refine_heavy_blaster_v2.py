"""Blender: source-detail/UV correction study, isolated from preserved V1.
Explicit reference UV landmarks, not procedural surface decoration.
"""
from pathlib import Path
import bpy,json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-02';OUT.mkdir(parents=True,exist_ok=True)
SOURCE=BASE/'heavy blaster 3.png';before=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(BASE/'attempt-01/heavy-blaster-v1.blend'))
s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0
# Preserve the raw pixel detail rather than crushing it with the V1 contrast look.
s.view_settings.look='None';s.eevee.use_gtao=False
image=bpy.data.images.load(str(SOURCE));image.pack()
m=bpy.data.materials.new('V2 authoritative reference / nearest UV landmarks');m.use_nodes=True
nodes=m.node_tree.nodes;nodes.clear();tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='CLIP'
em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial');m.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);m.node_tree.links.new(em.outputs[0],out.inputs[0])
records=[]
def uvface(obj,face_index,pixels):
 face=obj.data.polygons[face_index];assert len(face.loop_indices)==len(pixels)
 if m.name not in obj.data.materials:obj.data.materials.append(m)
 face.material_index=list(obj.data.materials).index(m)
 uv=obj.data.uv_layers.active or obj.data.uv_layers.new()
 for loop,(x,y) in zip(face.loop_indices,pixels):uv.data[loop].uv=(x/256,1-y/256)
 records.append({'object':obj.name,'face':face_index,'reference_pixels_256':pixels})
def quad(name,corners,pixels,parent=pivot):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(corners,[],[(0,1,2,3)]);mesh.update()
 obj=bpy.data.objects.new(name,mesh);s.collection.objects.link(obj);obj.parent=parent;uvface(obj,0,pixels);return obj
# Delete nothing: old decal planes remain archived in the blend but hidden.
for obj in s.objects:
 if obj.type!='MESH':continue
 if obj.name.startswith(('source barrel armor','source upper rail','source receiver armor','source-cut base','source side window')):
  obj.hide_render=True;obj.hide_set(True)
# Broad armored rail: upper armor, cyan conduit and lower copper are separate
# source regions, instead of stretching one mixed crop over the entire side.
for sign in (-1,1):
 x=sign*.650
 quad('V2 rail armor',[(x,-3.00,2.12),(x,-.96,2.255),(x,-.96,1.96),(x,-3.00,1.82)],[(42,124),(101,94),(101,111),(42,143)])
 quad('V2 lower copper rail',[(x,-3.02,1.54),(x,-.80,1.79),(x,-.80,1.48),(x,-3.02,1.32)],[(43,150),(105,119),(104,137),(43,164)])
 quad('V2 cyan side conduit',[(x*1.003,-3.00,1.79),(x*1.003,-.82,1.99),(x*1.003,-.82,1.89),(x*1.003,-3.00,1.68)],[(47,140),(106,111),(106,117),(47,146)])
 x0,x1=sorted((sign*.23,sign*.64))
 quad('V2 rail upper bevel',[(x0,-3.00,2.122),(x0,-1.02,2.262),(x1,-1.02,2.262),(x1,-3.00,2.122)],[(34,118),(91,88),(102,94),(43,124)])
 # Cyan receiver windows and sloped vent armor are distinct continuous panels.
 quad('V2 receiver windows',[(sign*.690,-.20,2.47),(sign*.690,1.20,2.47),(sign*.690,1.20,2.13),(sign*.690,-.20,2.13)],[(176,55),(211,38),(214,57),(178,75)])
 quad('V2 receiver vent armor',[(sign*.692,-.72,2.22),(sign*.692,-.20,2.47),(sign*.692,-.20,2.13),(sign*.692,-.72,1.91)],[(144,70),(175,55),(178,76),(148,91)])
 quad('V2 side badge',[(sign*.706,-.79,1.99),(sign*.706,-.30,1.99),(sign*.706,-.30,1.67),(sign*.706,-.79,1.67)],[(117,106),(137,96),(137,112),(117,122)])
# Map the actual support polygon, not a tiny generic strip. Rivets, metal edges
# and the blue inset now follow the arm instead of being replaced by flat brown.
for obj in list(s.objects):
 if obj.name.startswith('support inset'):obj.hide_render=True;obj.hide_set(True)
 if obj.name.startswith('support cyan conductor'):obj.hide_render=True;obj.hide_set(True)
 if obj.name.startswith('angled trunnion support'):
  # Profile has six vertices per end. UVs stay in source order on each side.
  pixels=[(126,163),(122,144),(153,95),(169,105),(166,128),(144,158)]
  uvface(obj,1,pixels);uvface(obj,0,list(reversed(pixels)))
# Source top plating: seams, chamfer highlights, fasteners. Bearing covers center.
quad('V2 stationary top plating',[(-1.48,-1.625,.560),(1.48,-1.625,.560),(1.48,1.625,.560),(-1.48,1.625,.560)],[(48,163),(129,195),(235,145),(154,113)],None)
# Separate front gear, side pipes and corner rivet strips: no tiled duplicates.
quad('V2 base front armor',[(-1.45,-1.661,.47),(1.45,-1.661,.47),(1.45,-1.661,.02),(-1.45,-1.661,.02)],[(46,174),(126,205),(126,224),(46,192)],None)
quad('V2 base side armor',[(1.761,-1.40,.47),(1.761,1.40,.47),(1.761,1.40,.02),(1.761,-1.40,.02)],[(145,202),(235,157),(235,175),(145,224)],None)
# The broad brown V1 bearing top was not present in the reference.
bpy.data.objects['bearing upper'].data.materials[0]=bpy.data.materials['source steel']
receiver=bpy.data.objects['rear receiver']
uvface(receiver,3,[(108,72),(115,51),(137,62),(128,82)])
uvface(receiver,4,[(115,51),(166,25),(197,38),(144,64)])
# Raised roof channel replaces the conspicuously blank sloping front surface.
# Bends follow physical geometry; cyan/metal pixels come from the actual conduit.
quad('V2 sloping roof conduit',[(-.24,-.70,2.225),(-.24,-.32,2.655),(.24,-.32,2.655),(.24,-.70,2.225)],[(99,80),(116,57),(126,62),(110,85)])
quad('V2 conduit nose',[(-.24,-1.35,2.285),(-.24,-.70,2.225),(.24,-.70,2.225),(.24,-1.35,2.285)],[(85,88),(99,80),(110,85),(92,95)])
# Main view first. Also render native 128px directly; never shrink a large render.
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v2.blend'))
for size in (320,128):
 s.render.resolution_x=s.render.resolution_y=size;s.render.filepath=str(OUT/f'front-{size}-raw.png');bpy.ops.render.render(write_still=True)
(OUT/'uv-provenance.json').write_text(json.dumps({'source_sha256':before,'source':'heavy blaster 3.png','sampling':'nearest from untouched full-resolution source','faces':records,'status':'detail/UV study; geometry and hidden sides still unfinished','native_preview':'128px direct render, not a game binding'},indent=2)+'\n')
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==before
print('HEAVY_BLASTER_V2_DETAIL_STUDY_COMPLETE')
