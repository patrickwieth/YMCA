"""Native source-colored Plasma Cannon, separate fixed foundation and rotating organism."""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys,ast
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--pass',dest='stage',type=int,required=True);p.add_argument('--all',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin plasma';SRC=ART/'photon_cannon-1201896784-0.png';OUT=ART/f'pass-{a.stage:02d}';OUT.mkdir(exist_ok=True)
S=a.stage;D=28.;SIZE=320;GROUND=232;PIVOT=2.14
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=SIZE;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.filter_size=.01;scene.render.dither_intensity=0;scene.eevee.taa_render_samples=1;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
fixed=bpy.data.collections.new('FIXED / feet, lower mechanisms and grounded roots');moving=bpy.data.collections.new('YAW / complete cannon and cervical returns');scene.collection.children.link(fixed);scene.collection.children.link(moving);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,PIVOT)
colors=[tuple(v) for v in json.loads((ART/'reference-review/palette.json').read_text())];objects=[];mats={};palette={}
# Share geometry primitives only: never execute the Shard scene or import its artwork.
source=ast.parse((Path(__file__).parent/'model_scrin_shard_launcher.py').read_text());functions=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('nearest','linear','mesh','smooth','organ','vein','ring')];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native mesh primitives>','exec'),globals())
families={'shell':[(12,12,22),(22,23,33),(34,35,49),(47,48,65),(62,64,82),(78,82,100),(96,104,123),(123,136,151),(157,176,184)],'vein':[(17,13,27),(28,22,41),(43,32,57),(58,43,75),(78,58,93),(98,77,116),(123,99,139),(153,129,162),(181,161,190)],'cyan':[(4,28,53),(5,49,80),(6,76,113),(10,105,147),(14,145,182),(29,190,217),(70,224,239),(136,247,248),(211,255,251)]}
for family,tones in families.items():
 for i,rgb in enumerate(tones):
  key=family+str(i);rgb=nearest(rgb);palette[key]=rgb;m=bpy.data.materials.new(key);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(linear,rgb),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);mats[key]=m
source_image=bpy.data.images.load(str(SRC));source_image.pack();m=bpy.data.materials.new('Bounded plasma source armor');m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=source_image;t.interpolation='Closest';t.extension='CLIP';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(t.outputs['Color'],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);mats['source']=m

def orb(name,center,radii,fixed_part=False,bias=0,family='cyan'):
 vs=[];fs=[];n=24;m=12
 for j in range(m+1):
  b=math.pi*j/m
  for k in range(n):
   t=k*math.tau/n;vs.append((center[0]+radii[0]*math.sin(b)*math.cos(t),center[1]+radii[1]*math.sin(b)*math.sin(t),center[2]+radii[2]*math.cos(b)))
 for j in range(m):
  for k in range(n):fs.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
 if S>=2:fs=[tuple(reversed(f)) for f in fs]
 return mesh(name,vs,fs,family,fixed_part,bias)
def throat(name,y,outer,inner,z=4.42):
 vs=[];n=40
 for yy,r in [(y,outer),(y+.18,outer*.97),(y+.18,inner),(y-.30,inner*.84)]:
  for k in range(n):
   t=k*math.tau/n;vs.append((r*math.cos(t),yy,z+r*math.sin(t)))
 fs=[(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(3) for k in range(n)]
 if S>=2:fs=[tuple(reversed(f)) for f in fs]
 return mesh(name,vs,fs,bias=1)
# Fixed, low rooted architecture. No huge uniform ground plate.
organ('Fixed recessed mechanical core',[(0,0,.12),(0,0,.7),(0,0,1.65),(0,0,1.91)],[(1.3,1.3),(1.5,1.5),(1.15,1.15),(.85,.85)],fixed_part=True,bias=-2,n=24)
ring('Fixed lower yaw socket',1.91,.86,.065,.62,True,bias=-2)
for i in range(4):
 t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
 def q(r,z,w=0):return u*r+v*w+Vector((0,0,z))
 rows=[(2.94,.08,.13,.02),(2.69,.33,.37,.16),(2.22,1.04,.47,.26),(1.61,1.67,.48,.23),(1.10,1.92,.31,.15)];vs=[]
 for r,z,w,d in rows:
  for x,y in [(-1,0),(-.7,d),(.7,d),(1,0),(.7,-.14),(-.7,-.14)]:vs.append(tuple(q(r+y,z,x*w)))
 faces=[(j*6+k,j*6+(k+1)%6,(j+1)*6+(k+1)%6,(j+1)*6+k) for j in range(4) for k in range(6)];faces.extend([tuple(range(5,-1,-1)),tuple(24+k for k in range(6))]);mesh(f'Fixed curved chitin leg {i}',vs,faces,fixed_part=True,bias=1)
 for j in range(4):
  z=.42+j*.29;r=2.62-j*.29
  organ(f'Fixed leg lamella {i}/{j}',[q(r,z,-.29),q(r+.18,z+.08),q(r,z,.29)],[.045,.075,.045],fixed_part=True,bias=2,n=8)
 orb(f'Fixed knee energy socket {i}',q(1.96,1.54),(.18,.18,.15),True,bias=-3,family='shell');orb(f'Fixed knee cyan core {i}',q(2.13 if S>=3 else 2.04,1.64 if S>=3 else 1.59),(.10,.10,.11),True,bias=1)
 if S>=3:
  for side in (-1,1):
   for j in range(3):
    r=1.55+j*.39;z=1.55-j*.40
    organ(f'Fixed margin hooked scale {i}/{side}/{j}',[q(r,z,side*.36),q(r+.17,z+.16,side*.59),q(r+.43,z-.08,side*.68)],[.05,.11,.003],fixed_part=True,bias=1,n=8)
 for side in (-1,1):
  vein(f'Fixed vascular leg margin {i}/{side}',[q(.66,1.82,side*.24),q(1.45,1.71,side*.45),q(2.05,1.19,side*.51),q(2.55,.43,side*.42),q(2.93,.10,side*.43)],.065,True)
  organ(f'Fixed pointed ground root {i}/{side}',[q(2.18,.48,side*.38),q(2.63,.16,side*.65),q(2.97,.08,side*.76),q(3.18,.06,side*.60)],[.065,.08,.045,.003],family='vein',fixed_part=True,n=7)
for i in range(4):
 t=i*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
 for j in range(9):
  z=.3+j*.16;organ(f'Fixed exposed mechanism rib {i}/{j}',[u*1.54-v*.4+Vector((0,0,z)),u*1.66+Vector((0,0,z+.025)),u*1.54+v*.4+Vector((0,0,z))],[.03,.055,.03],fixed_part=True,bias=1+j%2,n=6)
 for side in (-1,1):
  organ(f'Fixed overlapping side claw {i}/{side}',[u*1.05+v*side*.28+Vector((0,0,1.75)),u*1.75+v*side*.58+Vector((0,0,1.05)),u*2.45+v*side*.69+Vector((0,0,.28))],[(.21,.17),(.24,.20),(.01,.01)],fixed_part=True,bias=0,n=10)
if S>=4:
 for i in range(8):
  t=i*math.tau/8;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  organ(f'Fixed shoulder layered carapace {i}',[u*.75+Vector((0,0,1.84)),u*1.15+Vector((0,0,1.86)),u*1.65+Vector((0,0,1.54)),u*1.96+Vector((0,0,1.28))],[(.13,.10),(.30,.15),(.27,.18),(.025,.025)],fixed_part=True,bias=1,n=12)
  vein(f'Fixed shoulder protected vascular seam {i}',[u*.72+v*.1+Vector((0,0,1.91)),u*1.30+v*.22+Vector((0,0,1.82)),u*1.82+v*.28+Vector((0,0,1.20)),u*2.21+v*.3+Vector((0,0,.55))],.05,True)
# Entire upper anatomy rotates; no vascular connection crosses the bearing.
ring('Rotating recessed bearing',2.14,.73,.15,.51,bias=0)
organ('Rotating curved cervical column',[(0,0,2.34),(.10,-.10,2.74),(.15,-.55,3.45),(0,-.82,4.25)],[(.65,.60),(.60,.51),(.49,.46),(.59,.56)],n=20)
for side in (-1,1):
 organ(f'Yaw protective cervical cheek {side}',[(side*.53,-.02,2.35),(side*.69,-.27,2.83),(side*.60,-.68,3.55),(side*.56,-.85,4.18)],[(.15,.20),(.20,.22),(.18,.23),(.18,.19)],bias=1,n=12)
 organ(f'Yaw exposed cyan cervical conductor {side}',[(side*.54,.12,2.46),(side*.66,-.05,2.93),(side*.55,-.43,3.53),(side*.50,-.57,3.91)],[.055,.08,.075,.035],'cyan',bias=0,n=8)
 for j in range(3):
  x=side*(.66+j*.12);vein(f'Yaw returned neck vessel {side}/{j}',[(x,-1.14,4.12),(x*1.28,-1.16,3.51),(x*1.15,-.66,2.86),(side*.51,-.16+j*.09,2.30)],.055+j*.01)
organ('Main plasma receiver',[(0,-1.95,4.45),(0,-1.21,4.66),(0,-.25,4.62),(0,.25 if S>=5 else .80,4.43),(0,.65 if S>=5 else 1.54,4.42)],[(.36,.37),(.94,.87),(1.02,.84),(.82,.70),(.62,.52)],n=24)
# Deep hollow collar and a genuinely domed emitter, never a flat disk.
throat('Plasma throat shadow',1.53,.71,.55);throat('Plasma outer mouth armor',2.14,.97 if S>=4 else .82,.74 if S>=4 else .61)
orb('Convex blue plasma emitter',(0,1.80 if S>=5 else 2.18,4.42),(.68,.93,.68) if S>=5 else (.68,.58,.68) if S>=4 else (.56,.58,.56),bias=1)
orb('Plasma emitter hot core',(-.08,2.64,4.54),(.23,.12,.24),bias=2)
for i in range(4):
 t=i*math.tau/4+math.pi/4;u=Vector((math.cos(t),0,math.sin(t)))
 pts=[u*.70+Vector((0,.45 if S>=5 else 1.42,4.42)),u*(1.13 if S>=4 else .97)+Vector((0,1.50 if S>=5 else 2.07,4.42)),u*(.98 if S>=4 else .83)+Vector((0,2.66,4.42)),u*.57+Vector((0,2.96,4.42))]
 organ(f'Curled muzzle jaw {i}',pts,[(.13,.12),(.18,.15),(.105,.085),(.009,.009)],bias=1,n=12)
# Source's upper orb cradled between curved crescent horns.
orb('Upper energy orb dark socket',(0,-.83,5.28),(.59,.60,.35),family='shell',bias=-2)
orb('Upper domed cyan reservoir',(0,-.81,5.45),(.49,.50,.46) if S>=4 else (.43,.45,.40),bias=1)
for side in (-1,1):
 organ(f'Curved upper crescent housing {side}',[(side*.64,-.56,4.78),(side*.94,-1.05,5.10),(side*.80,-1.38,5.69),(side*.39,-1.35,6.07)],[(.17,.15),(.22,.16),(.14,.12),(.008,.008)],bias=1,n=14)
 organ(f'Rear swept horn {side}',[(side*.67,-1.52,4.49),(side*1.04,-1.97,4.90),(side*.99,-2.09,5.46)],[.15,.14,.008],bias=0,n=12)
 for j in range(5):
  y=-1.44+j*.45;organ(f'Receiver swept armor rib {side}/{j}',[(side*.55,y,5.12),(side*.99,y+.17,4.83),(side*1.01,y+.09,4.40),(side*.72,y-.04,4.13)],[.03,.09,.09,.025],bias=1+j%2,n=10)
 for j in range(3):
  x=side*(.70+j*.12);vein(f'Upper receiver closed vascular return {side}/{j}',[(x,-1.66,4.80),(x*1.14,-.78,4.11),(x*1.17,.45,3.96),(x*.83,1.57,4.20)],.055+j*.008)
 if S>=2:
  for j in range(6):
   y=-1.39+j*.38;organ(f'Recessed receiver gill {side}/{j}',[(side*.99,y,4.55),(side*1.04,y+.035,4.72),(side*.90,y+.07,4.94)],[.022,.042,.022],bias=-2,n=6)
  for j in range(6):
   z=2.45+j*.24;y=-(z-2.3)*.42
   organ(f'Cervical overlapping collar {side}/{j}',[(side*.48,y+.19,z),(side*.73,y,z+.08),(side*.53,y-.21,z+.16)],[.03,.055,.03],bias=2,n=8)
  orb(f'Side inset socket {side}',(side*1.03,-.22,4.57),(.10,.26,.27),family='shell',bias=-3)
  orb(f'Side energy node {side}',(side*1.12,-.22,4.57),(.055,.13,.14),bias=1)
if S>=2:
 for side in (-1,1):
  for j in range(3):
   x=side*(.28+j*.18)
   organ(f'Dorsal overlapping receiver petal {side}/{j}',[(x,-1.63,5.02),(x+side*.09,-.92,5.43),(x+side*.10,.03,5.34),(x*.9,.88,4.97)],[.035,.095,.11,.025],bias=1+j%2,n=10)
  vein(f'Dorsal protected energy vessel {side}',[(side*.59,-1.66,5.02),(side*.83,-.96,5.29),(side*.82,.04,5.12),(side*.55,.91,4.81)],.06,bias=1)
if S>=5:
 for side in (-1,1):
  orb(f'Cervical inset control housing {side}',(side*.66,-.05,2.94),(.14,.28,.30),family='shell',bias=-2)
  orb(f'Cervical convex cyan control {side}',(side*.80,-.05,2.98),(.08,.12,.15),bias=1)
  organ(f'Anterior serpentine cyan trunk {side}',[(side*.35,.54,3.90),(side*.68,.73,3.42),(side*.42,.72,2.89),(side*.56,.43,2.36)],[.04,.06,.06,.035],'cyan',bias=1,n=8)
if S>=3:
 for side in (-1,1):
  for j in range(3):
   vein(f'Visible anterior cervical return {side}/{j}',[(side*.55,.58,4.02),(side*(.71+j*.09),.72+j*.08,3.55),(side*(.68+j*.08),.60+j*.07,2.89),(side*.44,.32+j*.04,2.30)],.055+j*.005)
  organ(f'Visible protected anterior cyan conduit {side}',[(side*.52,.66,3.92),(side*.65,.81,3.40),(side*.58,.71,2.86),(side*.40,.41,2.36)],[.035,.055,.055,.025],'cyan',bias=1,n=7)
 for o in objects:
  if o['fixed'] or any(s in o.name.lower() for s in ('cervical','neck','bearing')):continue
  for v in o.data.vertices:v.co.y=-.3+(v.co.y+.3)*.75
  o.data.update()
# Correct actual envelopes, not a visual approximation.
for o in objects:
 if o['fixed']:
  if S>=4:
   for v in o.data.vertices:v.co.z=min(1.98,v.co.z)
   o.data.update()
  assert max(v.co.z for v in o.data.vertices)<2.05
 else:
  for v in o.data.vertices:v.co.z=max(2.14,v.co.z)
  o.data.update()
if S>=3:
 for o in objects:
  if not o.name.startswith('Fixed curved chitin leg'):continue
  i=int(o.name.rsplit(' ',1)[1]);t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0));uv=o.data.uv_layers.new(name='Bounded corresponding plasma leg armor');selected=[]
  for f in o.data.polygons:
   points=[(130+o.data.vertices[k].co.dot(v)*25,233-o.data.vertices[k].co.z*29) for k in f.vertices]
   if f.normal.dot(u)<.65 or not all(116<=x<=144 and 177<=y<=234 for x,y in points):continue
   for li,(x,y) in zip(f.loop_indices,points):uv.data[li].uv=(x/256,1-y/256)
   selected.append(f.index)
  o['texture_faces']=selected
if S>=3:
 sys.path.insert(0,str(Path(__file__).parent))
 from finish_scrin_shard_materials import apply as finish_chitin
 surface_cuts=[(116,63,128,75),(145,70,157,82),(119,184,132,199),(153,177,166,189)]
 coating_contrast=.38 if S>=4 else .18
 coating_targets=('Fixed shoulder layered','Main plasma receiver','Dorsal overlapping','Rotating curved cervical','Yaw protective cervical','Curved upper crescent','Curled muzzle jaw')
 finish_chitin(globals())
 if S>=5:
  o=bpy.data.objects['Main plasma receiver'];uv=o.data.uv_layers.new(name='Bounded original receiver top plate');uv.active_render=True;selected=[]
  for f in o.data.polygons:
   pts=[(123+o.data.vertices[k].co.x*26,83-(o.data.vertices[k].co.y-.3)*22) for k in f.vertices]
   if f.normal.z<.65 or not all(114<=x<=133 and 67<=y<=84 for x,y in pts):continue
   for li,(x,y) in zip(f.loop_indices,pts):uv.data[li].uv=(x/256,1-y/256)
   selected.append(f.index)
  o['texture_faces']=selected
# Fixed-pixel native camera and directional source-color shading.
target=Vector((0,0,(GROUND-SIZE/2)/(D*math.sqrt(3)/2)));bpy.ops.object.camera_add();camera=bpy.context.object;camera.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=SIZE/D;scene.camera=camera
names=list(mats);light=Vector((-.35,-.55,.86)).normalized();functions=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in ('relight','render')];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native render primitives>','exec'),globals());ids={}
for index,o in enumerate(objects,1):
 m=bpy.data.materials.new('part-id '+o.name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(linear(index%256),linear(index//256),0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);ids[o.name]=m
render('reference-raw.png',16);render('reference-ids.png',16,mask=True)
for i in (0,8,24):render(f'assembly-{i:02d}-raw.png',i);render(f'assembly-{i:02d}-ids.png',i,mask=True)
if a.all:
 render('base-raw.png',part='base');render('base-ids.png',part='base',mask=True)
 for i in range(32):render(f'turret-{i:02d}-raw.png',i,'turret');render(f'turret-{i:02d}-ids.png',i,'turret',True)
 render('base-control-raw.png',11,'base')
pivot.rotation_euler.z=0
for o in objects:
 o.hide_render=False
 for j,key in enumerate(names):o.data.materials[j]=mats[key]
bpy.context.view_layer.update();relight();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'plasma-cannon.blend'))
fixedmax=max(v.co.z for o in objects if o['fixed'] for v in o.data.vertices);movingmin=min(v.co.z for o in objects if not o['fixed'] for v in o.data.vertices)
metadata={'pass':S,'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'density':D,'frame_size':SIZE,'ground_origin':[SIZE/2,GROUND],'pivot':[0,0,PIVOT],'source_palette':palette,'objects':len(objects),'fixed_max_z':fixedmax,'moving_min_z':movingmin,'rotation_clearance':movingmin-fixedmax,'muzzle_model':[0,1.995 if S>=3 else 2.76,4.42],'reference_yaw':16,'all32':a.all,'runtime_installed':False,'inferred':'rear/hidden anatomy and bearing; source provides one view'}
(OUT/'geometry.json').write_text(json.dumps(metadata,indent=2)+'\n');print('SCRIN_PLASMA_PASS',S,'OBJECTS',len(objects),'CLEARANCE',movingmin-fixedmax)
