"""Reference-led twin rotary cannon; fixed armored plinth and independent native32 yaw."""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys,ast
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--pass',dest='stage',type=int,required=True);p.add_argument('--all',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);stage=a.stage
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';OUT=ART/f'pass-{stage:02d}';OUT.mkdir(exist_ok=True)
S=20;D=28.;SIZE=320;GROUND=224;PIVOT=1.24
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=SIZE;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.filter_size=.01;scene.render.dither_intensity=0;scene.eevee.taa_render_samples=1;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
fixed=bpy.data.collections.new('FIXED / armored1x1 plinth');moving=bpy.data.collections.new('YAW / twin gun carriage, feeds and rotary bundles');scene.collection.children.link(fixed);scene.collection.children.link(moving);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,PIVOT)
colors=[tuple(v) for v in json.loads((ART/('reference-review/palette-expanded.json' if stage>=3 else 'reference-review/palette.json')).read_text())];objects=[];mats={};palette={}
source=ast.parse((Path(__file__).parent/'model_scrin_shard_launcher.py').read_text());functions=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('nearest','linear','mesh','smooth','organ','ring')];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native mesh primitives>','exec'),globals())
families={'shell':[(10,12,9),(18,21,15),(29,33,23),(43,47,34),(59,64,47),(78,82,64),(103,108,87),(136,141,120),(174,178,157)],'steel':[(29,32,30),(45,49,47),(68,73,70),(95,101,97),(124,132,128),(157,165,160),(190,198,192),(221,227,220),(245,247,237)],'white':[(54,60,53),(76,84,75),(104,114,103),(137,147,135),(168,178,165),(195,205,192),(216,224,212),(235,242,230),(251,253,241)],'tan':[(52,46,26),(75,66,36),(100,88,49),(127,112,65),(157,140,84),(185,169,107),(210,195,135),(233,220,162),(250,240,187)],'red':[(47,12,8),(72,20,13),(98,28,18),(126,38,24),(155,51,32),(183,67,43),(208,86,57),(229,111,79),(245,143,108)]}
for family,tones in families.items():
 for i,rgb in enumerate(tones):
  key=family+str(i);rgb=nearest(rgb);palette[key]=rgb;m=bpy.data.materials.new(key);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(linear,rgb),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);mats[key]=m
U=Vector((1,1,0)).normalized();V=Vector((1,-1,0)).normalized()
def plinth(name,extent,z0,z1,family,bevel=.55,bias=0):
 r=extent;b=r-bevel;outline=[(-b,r),(b,r),(r,b),(r,-b),(b,-r),(-b,-r),(-r,-b),(-r,b)];vs=[tuple(U*x+V*y+Vector((0,0,z))) for z in (z0,z1) for x,y in outline];n=len(outline);return mesh(name,vs,[tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)],family,True,bias)
def box(name,center,size,family='shell',fixed_part=False,bias=0):
 x,y,z=center;dx,dy,dz=[v/2 for v in size];vs=[(x+sx*dx,y+sy*dy,z+sz*dz) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]];return mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],family,fixed_part,bias)
def panel(name,x0,x1,y,z0,z1,family,bias=0):
 return mesh(name,[tuple(U*x+V*y+Vector((0,0,z))) for x,z in [(x0,z0),(x1,z0),(x1,z1),(x0,z1)]],[(0,1,2,3)],family,True,bias)
def bore(name,x,z,y0,y1,outer,inner,family='steel'):
 n=16;vs=[]
 for y,r in [(y0,outer),(y1,outer),(y1,inner),(y1-.19,inner)]:
  for k in range(n):
   t=k*math.tau/n;vs.append((x+r*math.cos(t),y,z+.06*(y+.1)+r*math.sin(t)))
 fs=[tuple(reversed((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))) for j in range(3) for k in range(n)];o=mesh(name,vs,fs,family,bias=0)
 # Recess is physical geometry with a dark cap behind the bore lip.
 mesh(name+' dark recessed bore',vs[3*n:],[tuple(reversed(range(n)))],'shell',bias=-3)
 return o
plinth('Fixed lower armored foot',2.45,.04,.20,'shell',bias=-1)
plinth('Fixed beveled sandstone housing',2.38,.20,.82,'tan',bias=0)
plinth('Fixed red upper rim',2.43,.75 if stage>=2 else .82,1.01,'red',bias=1 if stage>=2 else 0)
plinth('Fixed pale inset deck',1.92 if stage>=2 else 2.15,1.012,1.08,'tan',bias=1)
ring('Fixed dark azimuth socket',1.083,1.48,.02,1.20,True,bias=-3)
panel('Fixed front recessed field',-1.61,1.61,2.389,.29,.68,'tan',bias=1)
for i in range(6):
 x=-1.25+i*.49;panel(f'Fixed front lower vent {i}',x,x+.19,2.397,.32,.40,'shell',bias=-1)
 if stage>=2:panel(f'Fixed front upper vent {i}',x+.045,x+.145,2.397,.49,.57,'tan',bias=-2)
for side in (-1,1):
 for z in (.28,.70):
  c=U*(side*1.64)+V*2.40+Vector((0,0,z));organ(f'Fixed face bolt {side}/{z}',[c,c+V*.018],[.042,.035],'steel',True,bias=2,n=6,steps=1)
# The complete rotating support starts above the fixed socket.
ring('Rotating azimuth bearing',1.24,1.32,.15,1.04,bias=0)
box('Rotating central crosshead',(0,-.12,1.63),(2.85,1.20,.47),bias=-1)
if stage>=2:
 organ('Rotating closed azimuth cover',[(0,0,1.25),(0,0,1.385)],[1.15,1.15],bias=-2,n=32,steps=1)
 organ('Central transverse steel linkage',[(-1.30,-.65,1.90),(1.30,-.65,1.90)],[.18,.18],'steel',bias=0,n=12,steps=1)
 organ('Central linkage gearbox',[(0,-.90,2.02),(0,-.50,2.02)],[(.35,.28),(.30,.25)],'steel',bias=1,n=12,steps=1)
for side in (-1,1):
 x=side*1.25
 organ(f'Gun support curved yoke {side}',[(x,-.1,1.60),(x,-.18,2.02),(x,-.55,2.66),(x,-.68,2.99)],[(.24,.30),(.30,.31),(.25,.25),(.22,.24)],bias=-1,n=12)
 organ(f'Elevation trunnion {side}',[(x-side*.46,-.50,2.79),(x+side*.47,-.50,2.79)],[.56,.56] if stage>=3 else [.43,.43],bias=-2,n=24,steps=1)
 organ(f'Trunnion recessed steel hub {side}',[(x+side*.48,-.50,2.79),(x+side*.51,-.50,2.79)],[.24,.20],'steel',bias=0,n=16,steps=1)
 organ(f'Trunnion brass pin {side}',[(x+side*.52,-.50,2.79),(x+side*.54,-.50,2.79)],[.09,.075],'tan',bias=1,n=8,steps=1)
 # Rounded white breech/casings from all32 supplied views, not box substitutes.
 organ(f'White receiver casing {side}',[(x,-1.72,2.97),(x,-1.42,3.00),(x,-.60,3.02),(x,-.08,3.04)],[(.45,.40),(.66,.56),(.66,.56),(.48,.45)],'white',bias=1,n=20)
 bore(f'Rear steel barrel collar {side}',x,3.04,-.05,.16,.48,.37,'steel')
 for k in range(6):
  t=math.pi/6+k*math.tau/6;xx=x+.35*math.cos(t);zz=3.04+.35*math.sin(t)
  bore(f'Rotary steel barrel {side}/{k}',xx,zz,.12,2.91,.095,.058,'steel')
  bore(f'Red muzzle sleeve {side}/{k}',xx,zz,2.66,3.02,.109,.061,'red')
 # Bundle braces stay thin enough to leave the individual tubes readable.
 bore(f'Forward bundle band {side}',x,3.04,2.46,2.56,.455,.408,'red')
 bore(f'Intermediate bundle clamp {side}',x,3.04,1.18,1.28,.455,.408,'shell')
 box(f'Lower ammunition cassette {side}',(x,-1.03,2.07),(.65,.87,.50),'shell',bias=0)
 if stage>=2:
  box(f'Cassette pale side plate {side}',(x+side*.34,-1.03,2.07),(.04,.68,.29),'tan',bias=-1)
  path=[Vector((x+side*.30,-1.18,2.10)),Vector((x+side*.64,-.99,2.28)),Vector((x+side*.58,-.50,2.47)),Vector((x+side*.30,-.19,2.61))]
  organ(f'Armored ammunition feed {side}',path,[.10,.14,.13,.09],'shell',bias=-1,n=8)
  for j,p in enumerate(smooth(path,3)):
   organ(f'Feed belt cartridge {side}/{j}',[p+Vector((0,-.08,0)),p+Vector((0,.08,0))],[.045,.038],'tan',bias=j%2,n=6,steps=1)
  organ(f'Elevation hydraulic cylinder {side}',[(x-side*.24,.37,1.78),(x-side*.24,.25,2.38)],[.115,.115],'steel',bias=-1,n=12,steps=1)
  organ(f'Elevation piston rod {side}',[(x-side*.24,.25,2.34),(x-side*.24,-.03,2.79)],[.055,.055],'steel',bias=2,n=10,steps=1)
  for j in range(4):
   y=-1.40+j*.26;box(f'Receiver side cooling slot {side}/{j}',(x+side*.645,y,3.0),(.025,.105,.18),'shell',bias=-2)
  for sign in (-1,1):
   organ(f'White casing protected seam {side}/{sign}',[(x+sign*.48,-1.48,3.32),(x+sign*.54,-.78,3.39),(x+sign*.39,-.12,3.30)],[.024,.033,.024],'steel',bias=-1,n=6)
 if stage>=3:
  organ(f'Rear rotary drive motor {side}',[(x,-1.61,2.34),(x,-1.42,2.34),(x,-.70,2.34)],[(.37,.36),(.51,.46),(.48,.43)],bias=-2,n=20)
  organ(f'Rear motor recessed hub {side}',[(x,-1.64,2.34),(x,-1.66,2.34)],[.22,.17],'steel',bias=-1,n=12,steps=1)
  for j in range(5):
   y=-1.45+j*.25
   organ(f'Receiver dorsal cooling fin {side}/{j}',[(x-.32,y,3.53),(x,y+.025,3.61),(x+.32,y,3.53)],[.018,.028,.018],'steel',bias=-1,n=6)
  organ(f'Yoke cable return {side}',[(x+side*.29,-.84,2.62),(x+side*.48,-.91,2.10),(x+side*.38,-.47,1.55),(side*.65,-.19,1.46)],[.035,.055,.045,.03],'shell',bias=-2,n=8)
  for j in range(8):
   t=j*math.tau/8;center=Vector((x+side*.525,-.5+.31*math.cos(t),2.79+.31*math.sin(t)))
   organ(f'Trunnion perimeter bolt {side}/{j}',[center,center+Vector((side*.025,0,0))],[.035,.03],'steel',bias=1,n=6,steps=1)
if stage>=2:
 for o in objects:
  if o.name.startswith(('Elevation trunnion','Trunnion recessed','Trunnion brass','Trunnion perimeter')):
   for v in o.data.vertices:v.co.z-=.50
   o.data.update()
# Proof-friendly separate envelopes and fixed-pixel camera.
assert max(v.co.z for o in objects if o['fixed'] for v in o.data.vertices)<PIVOT-.10
assert min(v.co.z for o in objects if not o['fixed'] for v in o.data.vertices)>=PIVOT-.001
target=Vector((0,0,(GROUND-SIZE/2)/(D*math.sqrt(3)/2)));bpy.ops.object.camera_add();camera=bpy.context.object;camera.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=SIZE/D;scene.camera=camera
names=list(mats);light=Vector((-.35,-.55,.86)).normalized();functions=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('relight','render')];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native rendering>','exec'),globals());ids={}
for index,o in enumerate(objects,1):
 m=bpy.data.materials.new('part-id '+o.name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(linear(index%256),linear(index//256),0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);ids[o.name]=m
for facing,number in [(4,1),(12,9),(20,17),(28,25)]:render(f'assembly-{number:02d}-raw.png',facing);render(f'assembly-{number:02d}-ids.png',facing,mask=True)
if a.all:
 render('base-raw.png',part='base');render('base-ids.png',part='base',mask=True)
 for i in range(32):render(f'turret-{i:02d}-raw.png',i,'turret');render(f'turret-{i:02d}-ids.png',i,'turret',True)
 render('base-control-raw.png',11,'base')
pivot.rotation_euler.z=0;bpy.context.view_layer.layer_collection.children[fixed.name].holdout=False
for o in objects:
 o.hide_render=False
 for j,key in enumerate(names):o.data.materials[j]=mats[key]
bpy.context.view_layer.update();relight();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'chinese-gatling.blend'))
meta={'pass':stage,'source_hashes':json.loads((ART/'reference-review/source-hashes.json').read_text()),'density':D,'frame_size':SIZE,'ground_origin':[SIZE/2,GROUND],'pivot':[0,0,PIVOT],'source_palette':palette,'objects':len(objects),'fixed_max_z':max(v.co.z for o in objects if o['fixed'] for v in o.data.vertices),'moving_min_z':min(v.co.z for o in objects if not o['fixed'] for v in o.data.vertices),'muzzles_model':[[-1.25,3.025,3.5775],[1.25,3.025,3.5775]],'all32':a.all,'runtime_installed':False,'inferred':'small fasteners, feed belts, internal bores; silhouette/colors taken from32 supplied views; no barrel-spin animation claimed'}
(OUT/'geometry.json').write_text(json.dumps(meta,indent=2)+'\n');print('CHINA_GATLING_PASS',stage,'OBJECTS',len(objects))
