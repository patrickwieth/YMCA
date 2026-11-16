"""Native 1x1 Soviet flak study. Original sources and existing SAM stay untouched.
True stationary cross-foot pedestal and rotating elevated single-cannon mechanism.
Run using background Blender, never the user's open scene.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'art-source/soviet flak';SOURCE=FOLDER/'flak.png';BASE=FOLDER/'flak base.png'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--all',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=FOLDER/f'pass-{args.iteration:02d}';OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=192;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.filter_size=.01;s.render.dither_intensity=0;s.eevee.taa_render_samples=1;s.view_settings.view_transform='Standard';s.view_settings.look='None'
fixed=bpy.data.collections.new('STATIONARY 1x1 BASE');moving=bpy.data.collections.new('ROTATING CANNON');s.collection.children.link(fixed);s.collection.children.link(moving);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,.62)
colors=set()
for path in (SOURCE,BASE):
 image=bpy.data.images.load(str(path));image.pack();rgba=list(image.pixels[:])
 colors.update(tuple(round(x*255) for x in rgba[i:i+3]) for i in range(0,len(rgba),4) if min(rgba[i:i+3])<.95)
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(x):
 x=x/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
mats={}
for family,palette in {'steel':[(27,30,34),(44,49,54),(61,69,76),(84,96,104),(109,125,135),(147,164,174)],'red':[(42,23,24),(65,28,29),(95,37,35),(124,47,43),(155,67,56),(183,91,73)],'brass':[(47,37,25),(65,52,33),(94,77,47),(122,103,68),(147,128,91),(171,152,116)]}.items():
 for i,col in enumerate(palette):
  m=bpy.data.materials.new(f'{family}{i}');m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(lin,nearest(col)),1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);mats[m.name]=m
objects=[]
def mesh(name,vs,fs,stationary=False,family='steel',bias=0):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);(fixed if stationary else moving).objects.link(o)
 if not stationary:o.parent=pivot;o.location=-pivot.location
 for m in mats.values():data.materials.append(m)
 o['fixed']=stationary;o['family']=family;o['bias']=bias;objects.append(o);return o

def box(name,center,size,stationary=False,family='steel',bias=0):
 x,y,z=center;a,b,c=[v/2 for v in size];vs=[(x+xx*a,y+yy*b,z+zz*c) for zz in (-1,1) for yy in (-1,1) for xx in (-1,1)]
 return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],stationary,family,bias)
def tube(name,a,b,r,inner=0,n=32,stationary=False,family='steel',bias=0):
 a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u)
 vs=[tuple(center+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for center,rr in [(a,r),(b,r)]+([(a,inner),(b,inner)] if inner else []) for i in range(n)];fs=[]
 for i in range(n):
  j=(i+1)%n;fs.append((i,j,n+j,n+i))
  if inner:fs += [(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)]
 if not inner:fs += [tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]
 return mesh(name,vs,fs,stationary,family,bias)
def bevel(o,width=.05):
 bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Physical edge bevel','BEVEL');mod.width=width;mod.segments=2;mod.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=mod.name);return o
# Feet are inside the WORLD 1x1 diamond, not merely inside a 128px rectangle.
for i in range(4):
 t=i*math.pi/2;axis=Vector((math.cos(t),math.sin(t),0));end=axis*1.68
 tube('Fixed foot '+str(i),end+Vector((0,0,.04)),end+Vector((0,0,.17)),.31,n=16,stationary=True)
 tube('Fixed foot cap '+str(i),end+Vector((0,0,.17)),end+Vector((0,0,.22)),.23,n=16,stationary=True,bias=1)
 beam=bevel(box('Fixed radial outrigger '+str(i),(0,0,.25),(.40,1.40,.25),True));beam.rotation_euler.z=t-math.pi/2;beam.location=axis*1.00
 tube('Fixed pad bolt '+str(i),end+Vector((0,0,.23)),end+Vector((0,0,.28)),.10,n=12,stationary=True,bias=-1)
 tube('Fixed brass collar '+str(i),axis*(.94 if args.iteration>=2 else .65)+Vector((0,0,.29)),axis*(1.19 if args.iteration>=2 else .90)+Vector((0,0,.29)),.29,inner=.17,n=12,stationary=True,family='brass')
tube('Fixed center armored ring',(0,0,.15),(0,0,.49),.99,n=12,stationary=True)
tube('Fixed bearing rim',(0,0,.49),(0,0,.63),.91,inner=.69,n=32,stationary=True,bias=1)
tube('Rotating mounting race',(0,0,.53),(0,0,.68),.70,n=32,bias=-1)
# Two sculpted yokes, independent transverse axle and inset gear hubs.
profile=[(-.62,.66),(.57,.66),(.69,.92),(.48,1.35),(.21,1.67),(.18,2.07),(-.02,2.32),(-.40,2.31),(-.62,2.07)]
for side in (-1,1):
 vs=[(side*.48+d,y,z) for d in (-.12,.12) for y,z in profile];n=len(profile);fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
 bevel(mesh('Sculpted elevation yoke '+str(side),vs,fs),.045)
 tube('Axle gear rim '+str(side),(side*.60,-.20,1.92),(side*.72,-.20,1.92),.40,inner=.28,n=24)
 tube('Axle recessed hub '+str(side),(side*.60,-.20,1.92),(side*.69,-.20,1.92),.26,n=16,bias=-1)
 for j in range(8):
  t=j*math.tau/8;tube('Axle gear tooth', (side*.69,-.20+.34*math.cos(t),1.92+.34*math.sin(t)),(side*.74,-.20+.34*math.cos(t),1.92+.34*math.sin(t)),.065,n=6)
 # Real star-shaped paint plate, not an opaque crop of the white source background.
 vs=[]
 for j in range(10):
  t=math.pi/2+j*math.pi/5;r=.16 if j%2==0 else .067;vs.append((side*.615,.05+r*math.cos(t),1.18+r*math.sin(t)))
 mesh('Red star identification '+str(side),vs,[tuple(range(10))],family='red',bias=1)
elevation=42 if args.iteration>=7 else 34
axis=Vector((0,-math.cos(math.radians(elevation)),math.sin(math.radians(elevation))));u=Vector((1,0,0));v=axis.cross(u);root=Vector((0,-.10,2.14))
tube('Breech main receiver',root-axis*.86,root+axis*.73,.36,n=16)
for side in (-1,1):
 off=u*(side*(.44 if args.iteration>=12 else .30))+v*.20
 tube('Upper recoil sleeve '+str(side),root-axis*.63+off,root+axis*(.82 if args.iteration>=12 else .50)+off,.13,n=16,bias=-1)
 tube('Upper recoil rod '+str(side),root+axis*(.78 if args.iteration>=12 else .47)+off,root+axis*(1.50 if args.iteration>=12 else 1.07)+off,.085 if args.iteration>=12 else .07,inner=.045 if args.iteration>=13 else 0,n=12,bias=1)
 if args.iteration>=13:
  tube('Upper auxiliary retaining band '+str(side),root+axis*1.43+off,root+axis*1.51+off,.115,inner=.082,n=16,family='red')
 tube('Receiver retaining rib '+str(side),root-axis*.12+off,root-axis*.02+off,.17,inner=.11,n=16)
tube('Hollow main barrel',root+axis*.60,root+axis*3.84,.135,inner=.073,n=24)
for start,end,r in ((.60,.80,.38),(.84,1.10,.30),(1.12,1.55,.24),(1.57,2.05,.20),(2.08,2.20,.18),(3.62,3.88,.165)):
 tube('Stepped barrel jacket '+str(start),root+axis*start,root+axis*end,r,inner=.132,n=24)
for distance,r in ((.82,.315),(2.08,.19)):
 tube('Red barrel retaining band '+str(distance),root+axis*distance,root+axis*(distance+.07),r,inner=r-.03,n=24,family='red')
# Lower return cylinder is a closed hydraulic piston, NOT a second firing barrel.
return_axis=Vector((0,-1,-.10)).normalized() if args.iteration>=8 else axis
# The low source cylinder does not point parallel to the elevated main cannon.
off=Vector((.37,0,-.30));tube('Lower return cylinder',root-return_axis*.65+off,root+return_axis*.52+off,.13,n=16,bias=-1);tube('Lower return piston',root+return_axis*.50+off,root+return_axis*1.25+off,.062,n=12,bias=1)
if args.iteration>=8:
 tube('Lower cylinder red terminal band',root+return_axis*1.17+off,root+return_axis*1.25+off,.09,inner=.06,n=16,family='red',bias=1)
 tube('Lower cylinder rear coupling',root-return_axis*.64+off,root-return_axis*.55+off,.17,n=16)
 for d in (-.44,.36):
  p=root+return_axis*d+off;tube('Auxiliary cylinder mounting pin',p+Vector((-.06,0,0)),p+Vector((.10,0,0)),.10,n=12,bias=-1)
closure=bevel(box('Rear receiver armored closure',tuple(root-axis*.85),(.78,.40,.62)),.08)
if args.iteration>=2:
 center=root-axis*.85;rotation=Matrix.Rotation(math.radians(-elevation),3,'X')
 for vertex in closure.data.vertices:vertex.co=center+rotation@(vertex.co-center)
 for side in (-1,1):
  # Recessed lower drive, raised dogleg reinforcement, and bounded service plates.
  tube('Lower elevation drive '+str(side),(side*.61,.27,1.16),(side*.72,.27,1.16),.22,inner=.14,n=20)
  tube('Lower drive hub '+str(side),(side*.61,.27,1.16),(side*.69,.27,1.16),.12,n=12,bias=-1)
  for j in range(6):
   t=j*math.tau/6;tube('Lower drive tooth',(side*.70,.27+.185*math.cos(t),1.16+.185*math.sin(t)),(side*.75,.27+.185*math.cos(t),1.16+.185*math.sin(t)),.04,n=6)
  points=[(side*.625,-.48,.75),(side*.625,-.49,1.33),(side*.625,-.42,1.74),(side*.625,-.31,2.16)]
  for j,(a,b) in enumerate(zip(points,points[1:])):tube('Raised yoke contour '+str(side)+' '+str(j),a,b,.045,n=10,bias=1)
  bevel(box('Side maintenance plate '+str(side),(side*.63,-.27,.94),(.055,.24,.32),bias=-1),.025)
  for zz in (.84,1.04):tube('Maintenance screw',(side*.66,-.27,zz),(side*.69,-.27,zz),.025,n=8,bias=1)
  tube('Rear yoke connecting strut '+str(side),(side*.42,.49,.78),(side*.42,.65,1.61),.075,n=12,bias=-1)
  tube('Rear yoke piston '+str(side),(side*.42,.60,1.35),(side*.42,.81,1.96),.045,n=12,bias=1)
  for d in (-.55,-.22,.10):
   off=u*(side*.35)+v*.04
   tube('Breech side bolt',root+axis*d+off,root+axis*d+off+u*(side*.04),.035,n=8,bias=1)
if args.iteration>=3:
 tube('Common elevation axle',(-.61,-.20,1.92),(.61,-.20,1.92),.22,n=24,bias=-1)
 vs=[(x,y+depth,z) for depth in (0,.08) for x,y,z in [(-.38,-.65,.74),(.38,-.65,.74),(.38,-.52,1.49),(-.38,-.52,1.49)]]
 bevel(mesh('Sloped front pedestal shield',vs,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]),.035)
 vs=[]
 for j in range(10):
  t=math.pi/2+j*math.pi/5;r=.18 if j%2==0 else .075;x=r*math.cos(t);z=1.12+r*math.sin(t);vs.append((x,-.65+(z-.74)*.13/.75-.012,z))
 mesh('Front red star',vs,[tuple(range(10))],family='red',bias=2)
 for xx in (-.27,.27):
  tube('Front shield lower fastener',(xx,-.645,.84),(xx,-.67,.84),.026,n=8,bias=1)
  tube('Front shield upper fastener',(xx,-.54,1.39),(xx,-.565,1.39),.026,n=8,bias=1)
 for m in mats.values():
  if not m.name.startswith('steel'):continue
  n=m.node_tree.nodes;links=m.node_tree.links;em=next(node for node in n if node.type=='EMISSION');coord=n.new('ShaderNodeTexCoord');xyz=n.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Generated'],xyz.inputs[0]);ramp=n.new('ShaderNodeMapRange');ramp.inputs['To Min'].default_value=.85;ramp.inputs['To Max'].default_value=1.1;links.new(xyz.outputs['Z'],ramp.inputs['Value']);mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=em.inputs[0].default_value;links.new(ramp.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0])
if args.iteration>=4:
 sys.path.insert(0,str(Path(__file__).resolve().parent))
 from detail_soviet_flak import apply
 apply(globals())
if args.iteration>=11:
 from refine_soviet_flak_recoil import apply as refine_recoil
 recoil_rig=refine_recoil(globals())
# Native direct rendering with extra padding, never runtime scaling.
target=Vector((0,0,40/(20*math.sqrt(3)/2)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=9.6;s.camera=cam
hold=bpy.data.materials.new('EXPORT base holdout');hold.use_nodes=True;n=hold.node_tree.nodes;n.clear();h=n.new('ShaderNodeHoldout');out=n.new('ShaderNodeOutputMaterial');hold.node_tree.links.new(h.outputs[0],out.inputs[0]);ids={}
for i,o in enumerate(objects,1):
 assert i<256
 m=bpy.data.materials.new('Surface '+str(i));m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(lin(i),0,0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);ids[o.name]=m
names=list(mats)
def light():
 light=Vector((-.25,-.50,.85)).normalized()
 for o in objects:
  for f in o.data.polygons:
   shade=max(0,min(5,int(((o.matrix_world.to_3x3()@f.normal).normalized().dot(light)+1)*2.7)+o['bias']));f.material_index=names.index(o['family']+str(shade))
bpy.context.view_layer.update();light();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'soviet-flak-model.blend'))
# Bound all stationary vertices, including transformed outrigger beams.
points=[o.matrix_world@v.co for o in objects if o['fixed'] for v in o.data.vertices];extent=max(max(abs(p.x),abs(p.y)) for p in points)
assert extent*20*math.sqrt(2)<64,(extent,'base exceeds 1x1 diamond')
def render(name,i=0,part='turret',mask=False):
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update();light()
 for o in objects:
  o.hide_render=part=='base' and not o['fixed']
  for j,m in enumerate(mats.values()):o.data.materials[j]=hold if part=='turret' and o['fixed'] else ids[o.name] if mask else m
 s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('base-raw.png',part='base');render('base-ids.png',part='base',mask=True);render('base-control-raw.png',11,'base');render('base-control-ids.png',11,'base',True);render('assembly-reference-raw.png',part='assembly')
for i in range(32) if args.all else range(0,32,4):render(f'turret-{i:03d}-raw.png',i);render(f'turret-{i:03d}-ids.png',i,mask=True)
for i in (4,12,20,28):render(f'assembly-control-{i:03d}-raw.png',i,'assembly')
if args.iteration>=11:
 from refine_soviet_flak_recoil import render_preview
 render_preview(globals())
(OUT/'geometry.json').write_text(json.dumps({'kind':'soviet-flak','barrel_elevation_degrees':elevation,'lower_cylinder_axis':list(return_axis),'source':'flak.png','source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'base_source_sha256':hashlib.sha256(BASE.read_bytes()).hexdigest(),'frame_size':[192,192],'native_density':20,'runtime_scale':1,'ground_origin_frame':[96,136],'pivot_world':list(pivot.location),'facings':32 if args.all else 8,'ground_footprint':'1x1','ground_diamond_limit':64,'ground_diamond_max':extent*20*math.sqrt(2),'game_installed':False,'limitations':['single reference; hidden mechanics inferred','opaque source background excluded from material palette','no existing SAM gameplay modifications']},indent=2)+'\n');print('SOVIET_FLAK_RENDERED',args.iteration)
