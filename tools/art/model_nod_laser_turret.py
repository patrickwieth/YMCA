"""Native Nod laser reconstruction. Separate fixed/rotating geometry, explicit UV islands.
Background Blender only. No runtime files or original artwork are changed.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'art-source/nod laser turret';SOURCE=FOLDER/'laser3 turret.png'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--all',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=FOLDER/f'pass-{args.iteration:02d}';OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1;s.view_settings.view_transform='Standard';s.view_settings.look='None'
fixed=bpy.data.collections.new('STATIONARY BASE');moving=bpy.data.collections.new('ROTATING TURRET');s.collection.children.link(fixed);s.collection.children.link(moving);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,.40)
image=bpy.data.images.load(str(SOURCE));image.pack();raw=list(image.pixels[:]);colors={tuple(round(v*255) for v in raw[i:i+3]) for i in range(0,len(raw),4) if raw[i+3]>.99}
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(v):
 v=v/255;return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def emission(name,rgb):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(lin,rgb),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
mats={};pal={}
for family,values in {'steel':[(24,27,34),(39,44,53),(57,67,77),(78,94,104),(109,133,145),(145,168,176)],'red':[(43,22,26),(68,28,31),(99,35,38),(135,46,48),(168,58,60),(200,77,78)]}.items():
 for i,col in enumerate(values):key=f'{family}{i}';pal[key]=nearest(col);mats[key]=emission(key,pal[key])
for key,col in {'ink':(12,14,20),'lens':(234,61,91),'core':(255,130,163),'copper':(116,68,42)}.items():pal[key]=nearest(col);mats[key]=emission(key,pal[key])
m=bpy.data.materials.new('SOURCE / bounded UV island');m.use_nodes=True;n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='EXTEND';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(tex.outputs['Color'],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);mats['source']=m
objects=[];records=[];groups={}
def mesh(name,vs,fs,family='steel',bias=0,fixed_part=False,uv=None,group=None,constant=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);(fixed if fixed_part else moving).objects.link(o)
 if not fixed_part:o.parent=pivot;o.location=-pivot.location
 for m in mats.values():data.materials.append(m)
 names=list(mats);layer=data.uv_layers.new()
 for poly in data.polygons:
  poly.material_index=names.index('source' if uv and poly.index in uv else constant or family+'3')
  coords=uv[poly.index] if uv and poly.index in uv else [(80,50)]*len(poly.vertices)
  for loop,(u,v) in zip(poly.loop_indices,coords):layer.data[loop].uv=(u/161,1-v/161)
 group=group or name;gid=groups.setdefault(group,len(groups)+1);o['surface_group']=gid;o['family']=family;o['bias']=bias;o['constant']=constant or '';o['textured_faces']=list((uv or {}).keys());objects.append(o);records.append({'name':name,'stationary':fixed_part,'vertices':len(vs),'faces':len(fs),'group':gid});return o

def tube(name,a,b,r,inner=0,family='steel',bias=0,n=32,fixed_part=False,group=None,constant=None):
 a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);vs=[]
 for center,rr in [(a,r),(b,r)]+([(a,inner),(b,inner)] if inner else []):vs.extend(tuple(center+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for i in range(n))
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.append((i,j,n+j,n+i))
  if inner:fs.extend([(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
 if not inner:fs.extend([tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))])
 return mesh(name,vs,fs,family,bias,fixed_part,group=group,constant=constant)
def box(name,center,size,family='steel',bias=0,uv=None,group=None,constant=None):
 x,y,z=center;a,b,c=[v/2 for v in size];vs=[(x+xx*a,y+yy*b,z+zz*c) for zz in (-1,1) for yy in (-1,1) for xx in (-1,1)]
 return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],family,bias,uv=uv,group=group,constant=constant)
def profile(name,outline,x0,x1,family='steel',bias=0,uv=None):
 vs=[(x,y,z) for x in (x0,x1) for y,z in outline];n=len(outline);fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
 return mesh(name,vs,fs,family,bias,uv=uv)
def gear(name,x,y,z,r):
 side=1 if x>0 else -1
 tube(name+' outer drum',(x-side*(.32 if args.iteration>=4 else .20),y,z),(x,y,z),r,bias=0,n=48)
 tube(name+' raised rim',(x,y,z),(x+side*(.16 if args.iteration>=4 else .07),y,z),r*(.96 if args.iteration>=4 else .93),inner=r*(.72 if args.iteration>=4 else .77),bias=1,n=48)
 tube(name+' dark inset',(x+side*.071,y,z),(x+side*.08,y,z),r*(.72 if args.iteration>=4 else .77),bias=-1 if args.iteration>=2 else -2,n=40)
 teeth=10 if args.iteration>=4 else 12
 for i in range(teeth):
  t=i*math.tau/teeth;vs=[]
  for xx in (x+side*.08,x+side*.13):
   for rr,tt in ((r*(.52 if args.iteration>=4 else .59),t-(.14 if args.iteration>=4 else .105)),(r*.77,t-(.14 if args.iteration>=4 else .105)),(r*.77,t+(.14 if args.iteration>=4 else .105)),(r*(.52 if args.iteration>=4 else .59),t+(.14 if args.iteration>=4 else .105))):vs.append((xx,y+rr*math.cos(tt),z+rr*math.sin(tt)))
  mesh(name+f' tooth {i}',vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],bias=2 if args.iteration>=2 else 1,group=name+' teeth')
 tube(name+' hub border',(x+side*.08,y,z),(x+side*.15,y,z),r*.46,bias=-1)
 tube(name+' hub cap',(x+side*.15,y,z),(x+side*.20,y,z),r*.36,bias=1)
# Ground origin = (128,186), density 20 px/unit, 1x1 candidate foundation.
tube('Fixed red bottom rim',(0,0,.02),(0,0,.16),2.04,family='red',fixed_part=True,n=48)
tube('Fixed metal foundation',(0,0,.16),(0,0,.38),2.04,fixed_part=True,n=48)
tube('Fixed inset bearing',(0,0,.38),(0,0,.45),1.72,fixed_part=True,n=48,bias=-2)
# Curved paired load-bearing yokes, not extruded source-image silhouettes.
outline=[(-1.15,.47),(1.15 if args.iteration>=2 else .65,.47),(.90,2.05),(1.2,2.8),(1.95,3.55),(2.15,4.3),(1.40,4.3),(.85,3.55),(0,3.0),(-.85,2.35),(-1.15,1.55)]
if args.iteration>=4:outline=[(y-.30 if y<-.8 else y,z-.05 if z<.6 else z) for y,z in outline]
for side in (-1,1):
 x=side*.86;profile('Structural yoke '+str(side),outline,x-.15,x+.15)
 center=(.50,2.25);inset=[(center[0]+(y-center[0])*.91,center[1]+(z-center[1])*.94) for y,z in outline]
 arm_uv=[(80+20*(1.065+y)/math.sqrt(2),138+20*((1.065-y)/math.sqrt(8)-z*math.sqrt(3)/2)) for y,z in inset]
 profile('Red yoke armor '+str(side),inset,x+side*.16,x+side*.205,'red',uv={0:list(reversed(arm_uv)),1:arm_uv} if args.iteration>=2 else None)
 gear('Lower bearing '+str(side),side*1.08,.42,1.48,.84 if args.iteration>=2 else .64)
box('Root cross brace',(0,-1.10,.58) if args.iteration>=4 else (0,-.65,.74),(1.48,.48,.26 if args.iteration>=4 else .46),bias=0)
box('Front maintenance plate',(0,-1.35,.59) if args.iteration>=4 else (0,-.905,.74),(1.20 if args.iteration>=4 else .87,.05,.21 if args.iteration>=4 else .28),bias=1)
for xx in (-.42,0,.42):
 for j in range(3):
  pts=[(xx,-1.05,.70) if args.iteration>=4 else (xx,-.85,1.0),(xx,-.8,1.8),(xx,-.3,2.65),(xx,.65,3.4)]
  tube(f'Inner support rib {xx} {j}',pts[j],pts[j+1],.075,bias=1,n=8,group='Inner ribs')
# Chamfered receiver: explicit near-side source panel island and physical cooling fins.
body=[(-1.48,3.45),(3.65,3.45),(3.90,3.70),(3.90,5.35),(3.65,5.60),(-1.22,5.60),(-1.48,5.34)]
body_uv=[(66,98),(146,58),(152,51),(152,22),(145,18),(66,57),(61,65)]
profile('Main closed receiver',body,-.88,.88,uv={0:list(reversed(body_uv)),1:body_uv})
for side in (-1,1):
 box('Receiver red seam '+str(side),(side*.90,1.00,4.04),(.045,4.72,.115),'red',bias=1)
 # Separate vent insert under the main bearing.
 box('Rear vent frame '+str(side),(side*.94,3.17,3.85),(.10,.83,.48),bias=0)
 for yy in (2.91,3.09,3.27,3.45):box('Rear vent slot',(side*1.001,yy,3.85),(.025,.075,.31),constant='ink',group='Rear vent slots '+str(side))
 gear('Main trunnion '+str(side),side*1.04,1.98,4.30,1.35 if args.iteration>=2 else 1.28)
box('Roof radiator recess',(0,2.45,5.64),(1.60,2.50,.12),bias=-2)
for i in range(8):box('Roof cooling fin '+str(i),(0,1.40+i*.30,5.78),(1.65,.12,.22),bias=1,group=('Roof cooling fin '+str(i)) if args.iteration>=2 else 'Roof cooling fins')
if args.iteration>=2:
 box('Rear cooling frame',(0,3.925,4.45),(1.40,.055,1.22),bias=0)
 box('Rear cooling recess',(0,3.961,4.45),(1.15,.022,.96),constant='ink')
 for i in range(5):box('Rear cooling louver '+str(i),(0,3.986,4.10+i*.17),(1.08,.03,.065),bias=1)
 for xx in (-.60,.60):
  for zz in (3.95,4.95):tube('Rear cover screw',(xx,3.955,zz),(xx,4.0,zz),.055,bias=1,n=8)
box('Top sight base',(-.18,-.46,5.68),(.91,1.36,.18),bias=0)
box('Top sight housing',(-.18,-.54,5.94),(.66,1.20,.37),bias=1)
box('Top sight red lens',(-.18,-1.151,5.94),(.40,.025,.19),'red',bias=1)
tube('Sight side dial',(.18,-.08,5.95),(.29,-.08,5.95),.23,bias=0,n=20)
# Concentric barrel collars and red strips, all actual mesh surfaces.
z=4.40
tube('Barrel socket',(0,-1.4,z),(0,-1.85,z),.86,n=16,bias=-1)
tube('Barrel socket rim',(0,-1.84,z),(0,-1.97,z),.77,inner=.58,n=16,bias=1)
tube('Barrel main shroud',(0,-1.90,z),(0,-4.10,z),.62,n=16)
for yy in (-2.50,-3.82):tube('Red barrel band '+str(yy),(0,yy,z),(0,yy-.12,z),.65,family='red',bias=1,n=16)
for xx in (-.40,.40):box('Barrel red side rail',(xx,-3.10,4.04),(.12,1.44,.18),'red',bias=1)
tube('Barrel front armor',(0,-3.94,z),(0,-4.33,z),.72,inner=.56,n=12,bias=0)
# Real octagonal muzzle opening with recessed vertical red emitter, no firing flash.
cross=[(-.45,-.70),(.45,-.70),(.67,-.46),(.67,.46),(.45,.70),(-.45,.70),(-.67,.46),(-.67,-.46)]
if args.iteration>=2:cross=[(x*1.20,z*1.12) for x,z in cross]
vs=[(x,yy,z+zz) for yy,scale in ((-4.30,1),(-4.48,1),(-4.48,.75),(-4.28,.75)) for x0,z0 in cross for x,zz in [(x0*scale,z0*scale)]];fs=[]
for layer in range(3):
 for i in range(8):j=(i+1)%8;fs.append((layer*8+i,layer*8+j,(layer+1)*8+j,(layer+1)*8+i))
mesh('Octagonal muzzle armor',vs,fs,bias=0)
box('Muzzle black recess',(0,-4.27,z),(1.0,.045,1.10),constant='ink')
box('Copper emitter bezel',(0,-4.31,z),(.63,.04,1.0),constant='copper')
box('Emitter dark rim',(0,-4.337,z),(.49,.02,.87),constant='ink')
box('Recessed red emitter',(0,-4.35,z),(.25 if args.iteration>=2 else .35,.012,.65 if args.iteration>=2 else .76),constant='lens')
box('Emitter bright core',(-.07,-4.359,z),(.10,.008,.63),constant='core')
if args.iteration>=3:
 box('Lower receiver apron',(0,-1.22,3.40),(1.65,.54,.72),uv={5:[(70,102),(78,98),(78,85),(70,89)],4:[(78,98),(70,102),(70,89),(78,85)]})
 # Actual rounded edges rather than a photographed bevel on a sharp box.
 for o in list(objects):
  if o.name=='Main closed receiver' or o.name.startswith('Roof cooling fin') or o.name=='Top sight housing' or (args.iteration>=4 and o.name.endswith('raised rim')):
   bpy.context.view_layer.objects.active=o;o.select_set(True);mod=o.modifiers.new('Machined armor bevel','BEVEL');mod.width=.13 if o.name=='Main closed receiver' else .055;mod.segments=2;mod.limit_method='ANGLE';mod.angle_limit=.40;bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
   o['textured_faces']=[f.index for f in o.data.polygons if f.material_index==list(mats).index('source')]
   for rec in records:
    if rec['name']==o.name:rec['vertices']=len(o.data.vertices);rec['faces']=len(o.data.polygons)
# Fixed camera and native density; extra canvas is padding only.
target=Vector((0,0,58/(20*math.sqrt(3)/2)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=12.8;s.camera=cam
holdout=bpy.data.materials.new('EXPORT / fixed-base holdout');holdout.use_nodes=True;n=holdout.node_tree.nodes;n.clear();h=n.new('ShaderNodeHoldout');out=n.new('ShaderNodeOutputMaterial');holdout.node_tree.links.new(h.outputs[0],out.inputs[0]);names=list(mats)
ids={i:emission('SURFACE '+str(i),(i,0,0)) for i in groups.values()}
def light():
 light=Vector((-.25,-.50,.85)).normalized()
 for o in objects:
  rot=o.matrix_world.to_3x3();fixedmat=o.get('constant');family=o['family'];bias=o['bias'];exclude=set(o['textured_faces'])
  for poly in o.data.polygons:
   if poly.index in exclude:poly.material_index=names.index('source');continue
   shade=max(0,min(5,int(((rot@poly.normal).normalized().dot(light)+1)*2.7)+bias))
   poly.material_index=names.index(fixedmat or family+str(shade))
bpy.context.view_layer.update();light();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'nod-laser-model.blend'))
def render(filename,idx=0,part='turret',mask=False):
 pivot.rotation_euler.z=idx*math.pi/16;bpy.context.view_layer.update();light()
 for o in objects:
  isfixed=o in list(fixed.objects);o.hide_render=(part=='base' and not isfixed)
  for j,mat in enumerate(mats.values()):o.data.materials[j]=holdout if isfixed and part=='turret' else ids[o['surface_group']] if mask and not isfixed else mat
 s.render.filepath=str(OUT/filename);bpy.ops.render.render(write_still=True)
render('base-raw.png',part='base');render('base-control-raw.png',11,part='base');render('assembly-reference-raw.png',part='assembly')
for i in (range(32) if args.all else range(0,32,4)):
 render(f'turret-{i:03d}-raw.png',i);render(f'turret-{i:03d}-ids.png',i,mask=True)
for i in (4,12,20,28):render(f'assembly-control-{i:03d}-raw.png',i,part='assembly')
(OUT/'geometry.json').write_text(json.dumps({'kind':'nod-laser','pass':args.iteration,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'frame_size':[256,256],'native_density':20,'runtime_scale':1,'ground_origin_frame':[128,186],'pivot_world':list(pivot.location),'objects':records,'groups':groups,'palette_samples':pal,'facings':32 if args.all else 8,'base_occlusion_holdout':True,'source_projected_over_whole_model':False,'game_installed':False,'limitations':['single reference: rear/hidden mechanics inferred','no actor/weapon binding or live visual review']},indent=2)+'\n');print('NOD_LASER_RENDERED',args.iteration)
