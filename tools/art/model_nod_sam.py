"""Nod SAM study: real rotating four-missile rack, source-extracted fixed base.
Background Blender only. Native renders, no source overwrite or runtime changes.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/nod sam site';SRC=F/'sam_site-1179307279-0.png'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--all',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);P=a.iteration;O=F/f'pass-{P:02d}';O.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.filter_size=.01;s.render.dither_intensity=0;s.eevee.taa_render_samples=1;s.view_settings.view_transform='Standard';s.view_settings.look='None'
col=bpy.data.collections.new('ROTATING SAM TURRET');s.collection.children.link(col);pivot=bpy.data.objects.new('turret_pivot',None);col.objects.link(pivot)
image=bpy.data.images.load(str(SRC));image.pack();raw=list(image.pixels[:]);colors=set()
# Bounded material sampling from the actual launcher, not background scenery.
for yy in range(24,126):
 for xx in range(65,162):
  k=((255-yy)*256+xx)*4;colors.add(tuple(round(v*255) for v in raw[k:k+3]))
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(x):
 x=x/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
def material(name,rgb):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(lin,rgb),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
mats={};pal={}
for family,values in {'steel':[(21,20,20),(35,33,32),(49,47,45),(65,64,59),(87,87,79),(113,113,102)],'red':[(42,21,22),(67,27,29),(96,34,37),(126,41,46),(160,54,59),(198,73,76)],'cream':[(62,50,40),(83,67,53),(110,91,70),(139,116,90),(173,146,116),(202,180,145)]}.items():
 for i,c in enumerate(values):key=family+str(i);pal[key]=nearest(c);mats[key]=material(key,pal[key])
# Authored machining fields rather than whole-image projection or RGB noise.
if P>=9:
 tex=bpy.data.images.new('Authored body coating 48x16',48,16,alpha=True);pixels=[]
 for y in range(16):
  for x in range(48):
   v=(.75 if P>=11 else .91) if (x//12+y//8)%2 else 1.0
   if x in (11,35):v=.80
   if y in (3,12) and x in (6,7,8,24,25,40):v=1.12
   pixels.extend([v,v,v,1])
 tex.pixels=pixels;tex.pack()
 for i in range(6):
  key='steelcoat'+str(i);m=mats['steel'+str(i)].copy();m.name=key;n=m.node_tree.nodes;e=next(x for x in n if x.type=='EMISSION');t=n.new('ShaderNodeTexImage');t.image=tex;t.interpolation='Closest';mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=e.inputs[0].default_value;m.node_tree.links.new(t.outputs[0],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],e.inputs[0]);mats[key]=m
names=list(mats);objects=[];groups={}
def mesh(name,vs,fs,family='steel',bias=0,group=None,uv=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);col.objects.link(o);o.parent=pivot
 for m in mats.values():data.materials.append(m)
 o['family']=family;o['bias']=bias;o['surface_id']=groups.setdefault(group or name,len(groups)+1);objects.append(o)
 layer=data.uv_layers.new()
 for poly in data.polygons:
  for loop in poly.loop_indices:layer.data[loop].uv=uv[data.loops[loop].vertex_index] if uv else (0,0)
 return o
def tube(name,start,end,r,inner=0,n=20,family='steel',bias=0,group=None):
 start,end=Vector(start),Vector(end);axis=(end-start).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);rings=[(start,r),(end,r)]+([(start,inner),(end,inner)] if inner else []);vs=[];uv=[]
 for j,(c,rr) in enumerate(rings):
  for i in range(n):vs.append(tuple(c+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))));uv.append((j%2,i/n))
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.append((i,j,n+j,n+i))
  if inner:fs.extend([(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
 if not inner:fs.extend([tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))])
 return mesh(name,vs,fs,family,bias,group,uv)
def box(name,c,size,family='steel',bias=0,group=None):
 c=Vector(c);dims=Vector(size)/2;vs=[tuple(c+Vector((x*dims.x,y*dims.y,z*dims.z))) for z in (-1,1) for y in (-1,1) for x in (-1,1)]
 return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],family,bias,group)
def profile(name,outline,x0,x1,family='steel',bias=0):
 n=len(outline);vs=[(x,y,z) for x in (x0,x1) for y,z in outline];return mesh(name,vs,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)],family,bias)
def beam(name,start,end,width,depth,family='steel',bias=0):
 start,end=Vector(start),Vector(end);axis=(end-start).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);vs=[tuple(c+u*x*width/2+v*y*depth/2) for c in (start,end) for x,y in ((-1,-1),(1,-1),(1,1),(-1,1))];return mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],family,bias)
def bevel(o,width=.035):
 bpy.context.view_layer.objects.active=o;m=o.modifiers.new('Machined bevel','BEVEL');m.width=width;m.segments=2;m.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=m.name);return o
# The fixed pedestal is NOT rebuilt. This small race belongs to the rotating part.
tube('Rotating lower race',(0,0,.01),(0,0,.20),1.08,n=40,bias=-1)
tube('Race red seal',(0,0,.16),(0,0,.23),1.07,inner=.98,n=40,family='red',bias=-1)
tube('Rotating platform',(0,0,.22),(0,0,.32),.99,n=32)
for side in (-1,1):
 x=side*.58
 if P<6:profile('Main yoke '+str(side),[(-.75,.30),(.80,.30),(.28,2.23),(-.15,2.26)],x-.10,x+.10)
 else:
  legs=[[(-.85,.30),(-.52,.30),(.20,2.20),(-.06,2.30),(-.25,2.05)],[(.46,.30),(.86,.30),(.31,2.30),(.07,2.30),(.11,1.90)]]
  for j,outline in enumerate(legs):
   bevel(profile(f'Open sculpted yoke {side} leg {j}',outline,x-.11,x+.11),.035)
   cy=sum(v[0] for v in outline)/len(outline);cz=sum(v[1] for v in outline)/len(outline);inside=[(cy+(y-cy)*.70,cz+(z-cz)*.87) for y,z in outline];profile(f'Recessed red yoke armor {side} {j}',inside,x+side*.12,x+side*.14,'red')
 beam('Yoke foot cross brace '+str(side),(x,-.8,.35),(x,.8,.35),.30,.24)
tube('Elevation axle',(-.83,.14,2.15),(.83,.14,2.15),.16,n=24,bias=-1)
axis=Vector((0,math.cos(math.radians(18)),math.sin(math.radians(18))));u=Vector((1,0,0));v=axis.cross(u).normalized();tails=[]
def from_pixel(px,py,z):
 total=(px-119)*math.sqrt(2)/20;diff=(py-115+z*20*math.sqrt(3)/2)*math.sqrt(8)/20;return Vector(((total+diff)/2,(total-diff)/2,z))
for k,(px,py) in enumerate(((71,85),(94,77),(79,97),(102,89))):
 tail=from_pixel(px,py,2.555 if P>=12 else 1.65);tails.append(tail)
 tube(f'Missile {k} main machined body',tail,tail+axis*2.96,.215 if P>=11 else .235,n=24 if P>=3 else 12,family='steelcoat' if P>=9 else 'steel',bias=-1 if P>=3 else 0,group=f'Missile {k} body')
 # Ogive, not flat disks or a painted source silhouette.
 rings=[(2.94,.25),(4.30,.008)] if P<2 else [(2.94,.265),(3.12,.29),(3.43,.255),(3.76,.18),(4.05,.10),(4.30,.008)]
 vs=[tuple(tail+axis*t+rr*(u*math.cos(j*math.tau/24)+v*math.sin(j*math.tau/24))) for t,rr in rings for j in range(24)];fs=[]
 for z in range(len(rings)-1):
  for j in range(24):fs.append((z*24+j,z*24+(j+1)%24,(z+1)*24+(j+1)%24,(z+1)*24+j))
 fs.append(tuple(range(23,-1,-1)));mesh(f'Missile {k} red ogive',vs,fs,'red',group=f'Missile {k} cone')
 if P>=3:
  for j,(t,r,depth) in enumerate(((.10,.25,.08),(.65,.25,.07),(1.85,.246,.055),(2.87,.26,.09))):tube(f'Missile {k} retaining band {j}',tail+axis*t,tail+axis*(t+depth),r,inner=.232,n=24,bias=-1)
  for angle in (.5,3.7):
   radial=u*math.cos(angle)+v*math.sin(angle);tube(f'Missile {k} longitudinal jacket seam',tail+axis*.75+radial*(.217 if P>=11 else .237),tail+axis*2.7+radial*(.217 if P>=11 else .237),.018,n=6,bias=-1,group=f'Missile {k} seams')
 if P>=4:
  tube(f'Missile {k} rear exhaust lip',tail-axis*.10,tail+axis*.05,.23,inner=.14,n=24,bias=1)
  tube(f'Missile {k} recessed exhaust',tail-axis*.03,tail,.135,n=24,bias=-3)
  for j in range(4):
   radial=u*math.cos(j*math.pi/2)+v*math.sin(j*math.pi/2);normal=axis.cross(radial)*.025;vs=[tuple(tail+axis*t+radial*r+normal*sign) for sign in (-1,1) for t,r in ((-.18,.23),(-.18,.43),(.67,.23))];mesh(f'Missile {k} tail fin {j}',vs,[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],bias=0,group=f'Missile {k} fins')
 if P>=5:
  for sign in (-1,1):
   off=Vector((sign*.19,0,-.35));bevel(beam(f'Launch rail {k} {sign}',tail-axis*.35+off,tail+axis*2.55+off,.10,.14,bias=0),.025)
  for t in (.50,2.25):
   pos=tail+axis*t+Vector((0,0,-.34));beam(f'Rail saddle {k} {t}',pos-u*.35,pos+u*.35,.15,.12,bias=-1)
   for sign in (-1,1):tube(f'Saddle fastener {k} {t} {sign}',pos+u*sign*.29, pos+u*sign*.29+Vector((0,0,.10)),.045,n=8,bias=1)
 else:beam('Simple launch rail '+str(k),tail+Vector((0,0,-.32)),tail+axis*2.5+Vector((0,0,-.32)),.18,.18)
# Cross members connect all four individual staggered cradles to the common axle.
for j,(ka,kb) in enumerate(((0,2),(1,3),(0,1),(2,3))):beam('Rack connecting member '+str(j),tails[ka]+axis*1.6+Vector((0,0,-.40)),tails[kb]+axis*1.6+Vector((0,0,-.40)),.18,.18,bias=-1)
for side,k in ((-1,0),(1,3)):beam('Axle to rack support '+str(side),(side*.58*(1.45 if P>=12 else 1),.14,2.70 if P>=12 else 2.15),tails[k]+axis*1.8+Vector((0,0,-.40)),.20,.20)
if P>=6:
 for side in (-1,1):
  box('Lower side armor '+str(side),(side*.78,0,.48),(.12,1.55,.48),bias=-1)
  # Cream warning strokes on the camera-visible side only; rear has service panel.
  if side==1:
   for i in range(5):
    y=-.60+i*.26;mesh('Cream identification stroke '+str(i),[(.849,y,.32),(.849,y+.14,.32),(.849,y+.31,.65),(.849,y+.17,.65)],[(0,1,2,3)],'cream',bias=1,group='Cream identification')
  else:box('Rear electronics access',(-.849,0,.49),(.025,.65,.30),'red',bias=-1)
if P>=7:
 for side in (-1,1):
  x=side*.73;tube('Trunnion drum '+str(side),(x-side*.12,.14,2.15),(x+side*.10,.14,2.15),.27,n=24)
  tube('Trunnion recessed rim '+str(side),(x+side*.10,.14,2.15),(x+side*.15,.14,2.15),.26,inner=.18,n=24,bias=1)
  tube('Trunnion hub '+str(side),(x+side*.12,.14,2.15),(x+side*.16,.14,2.15),.12,n=16,bias=-1)
  for i in range(6):
   t=i*math.tau/6;z=2.15+.22*math.sin(t);y=.14+.22*math.cos(t);tube('Trunnion rim bolt',(x+side*.15,y,z),(x+side*.19,y,z),.035,n=6,bias=1,group='Trunnion bolts '+str(side))
  start=Vector((side*.43,-.65,.55));end=Vector((side*.43,.04,1.96));mid=start+(end-start)*.62;tube('Elevation hydraulic cylinder '+str(side),start,mid,.095,n=16,bias=-1);tube('Elevation chrome piston '+str(side),mid,end,.045,n=12,bias=2)
if P>=8:
 for side in (-1,1):
  points=[(side*.8,-.5,.55),(side*.88,-.68,.8),(side*.88,-.50,1.3),(side*.75,-.14,1.8)]
  for j,(q,r) in enumerate(zip(points,points[1:])):tube('Actuator cable '+str(side)+' '+str(j),q,r,.034,n=8,bias=-2,group='Actuator cable '+str(side))
  for y in (-.6,.55):tube('Foot anchor bolt',(side*.60,y,.38),(side*.60,y,.45),.055,n=8,bias=1)
 bevel(box('Rear junction box',(0,-.76,.60),(.68,.24,.48),bias=0))
 for x in (-.22,-.11,0,.11,.22):box('Junction ventilation',(x,-.889,.60),(.045,.015,.27),bias=-3,group='Junction vent')
 tube('Rear harness socket',(.38,-.76,.60),(.48,-.76,.60),.10,n=16,bias=-1)
if P>=10:
 # Coherent mechanical edge detailing, not random speckling on every surface.
 for side in (-1,1):
  for j,(y,z) in enumerate(((-.53,.7),(-.19,1.7),(.60,.7),(.30,1.8))):tube('Yoke armor screw '+str(side)+' '+str(j),(side*.72,y,z),(side*.76,y,z),.028,n=8,bias=1,group='Yoke screws '+str(side))
  for j in range(3):box('Service panel narrow slots '+str(side)+' '+str(j),(side*.849,-.52+j*.13,.49),(.02,.045,.12),bias=-3,group='Side slots '+str(side))
 for i in range(12):
  t=i*math.tau/12;pt=Vector((.92*math.cos(t),.92*math.sin(t),.33));tube('Platform bolt '+str(i),pt,pt+Vector((0,0,.055)),.035,n=8,bias=1,group='Platform bolts')
if P>=9:
 # Native pass08 review: the support was too narrow for the supplied bearing.
 # Broaden support/drive assembly, NOT the matched missile or rail positions.
 for o in objects:
  if o.name.startswith('Rotating') or o.name.startswith('Race red'):
   for vertex in o.data.vertices:vertex.co.x*=1.25;vertex.co.y*=1.25
  elif not o.name.startswith(('Missile','Launch rail','Rail saddle','Saddle fastener','Simple launch rail','Rack connecting member','Axle to rack support')):
   for vertex in o.data.vertices:vertex.co.x*=1.45
 if P>=10:
  for j in range(5):tube('Rear drive cooling collar '+str(j),(-.77,-.68,.57+j*.12),(-.77,-.68,.63+j*.12),.25,n=20,bias=0,group='Rear drive cooling collars')
  tube('Rear drive cap',(-.77,-.68,1.13),(-.77,-.68,1.18),.21,n=20,bias=1)
  for j in range(8):
   t=j*math.tau/8;pt=Vector((-.77+.20*math.cos(t),-.68+.20*math.sin(t),1.17));tube('Rear drive cap tooth '+str(j),pt,pt+Vector((0,0,.045)),.04,n=6,bias=-1,group='Rear drive teeth')
if P>=11:
 # Final native review: reduce broad bare plate glare and add useful drive details.
 next(o for o in objects if o.name=='Rotating platform')['bias']=-2
 bevel(box('Central traverse gearbox',(0,.16,.49),(.72,.88,.30),bias=-1),.045)
 box('Traverse access red panel',(.0,-.291,.49),(.46,.025,.18),'red',bias=-1)
 for x in (-.22,0,.22):box('Traverse cooling groove',(x,.18,.651),(.045,.48,.014),bias=-3,group='Traverse cooling grooves')
 for i in range(4):
  t=i*math.pi/2+.2;ray=Vector((math.cos(t),math.sin(t),0));tube('Platform panel seam '+str(i),ray*.70+Vector((0,0,.325)),ray*1.15+Vector((0,0,.325)),.018,n=6,bias=-2,group='Platform panel seams')
 for side in (-1,1):
  y=-.3;z=.45;tube('Lower drive feed '+str(side),(side*.40,y,z),(side*.88,y,z),.07,n=12,bias=-1)
 for k,tail in enumerate(tails):
  tube('Missile jacket red witness '+str(k),tail+axis*.8+Vector((.217,0,0)),tail+axis*1.7+Vector((.217,0,0)),.012,n=6,family='red',bias=-1,group='Witness stripes')
if P>=12:
 # Full-rotation review exposed a lateral overhang hidden in the source view.
 # Moving the launch cradles along camera depth preserves their exact reference
 # projection while centering the four missiles above the bearing in3D.
 # Raise the corresponding yoke/actuator heads to the corrected cradle height.
 for o in objects:
  if o.name.startswith(('Open sculpted yoke','Recessed red yoke','Elevation axle','Trunnion','Elevation hydraulic','Elevation chrome','Actuator cable','Yoke armor screw')):
   for vertex in o.data.vertices:vertex.co.z+=.55*max(0,min(1,(vertex.co.z-.35)/1.80))
# Camera / 20px per model unit; original source pixels remain on a256 frame.
bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=(7,-7,math.sqrt(98)/math.sqrt(3));cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=12.8;cam.data.shift_x=9/256;cam.data.shift_y=-13/256;s.camera=cam
# Fixed radar silhouette is a depth-only occluder: no new rendered base geometry.
# Its inferred depth allows near-side missiles in front and far-side missiles behind.
if P>=10:
 normal=Vector((7,-7,math.sqrt(98)/math.sqrt(3))).normalized();right=Vector((1,1,0)).normalized();up=normal.cross(right).normalized();outline=[(167,74),(171,66),(176,62),(182,62),(188,66),(194,71),(195,88),(192,93),(187,96),(187,102),(193,105),(194,116),(187,121),(174,119),(166,114),(166,103),(171,97),(171,92),(167,85)]
 vs=[tuple(right*((x-119)/20)+up*((115-y)/20)+normal*(-.75 if P==11 else 1.75)) for x,y in outline];data=bpy.data.meshes.new('Fixed radar depth proxy');data.from_pydata(vs,[],[tuple(range(len(vs)))]);data.update();o=bpy.data.objects.new('Fixed radar / HOLDOUT ONLY',data);s.collection.objects.link(o);hold=bpy.data.materials.new('Radar holdout');hold.use_nodes=True;n=hold.node_tree.nodes;n.clear();h=n.new('ShaderNodeHoldout');out=n.new('ShaderNodeOutputMaterial');hold.node_tree.links.new(h.outputs[0],out.inputs[0]);data.materials.append(hold)
ids={i:material('Surface '+str(i),(i%256,i//256,0)) for i in groups.values()}
def light(mask=False):
 light=Vector((-.25,-.5,.85)).normalized()
 for o in objects:
  for j,m in enumerate(mats.values()):o.data.materials[j]=ids[o['surface_id']] if mask else m
  for poly in o.data.polygons:
   shade=max(0,min(5,int(((o.matrix_world.to_3x3()@poly.normal).normalized().dot(light)+1)*2.7)+o['bias']));poly.material_index=names.index(o['family']+str(shade))
bpy.context.view_layer.update();light();bpy.ops.wm.save_as_mainfile(filepath=str(O/'nod-sam.blend'))
facings=list(range(32)) if a.all else list(range(0,32,4))
for i in facings:
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update()
 for mask in (False,True):light(mask);s.render.filepath=str(O/(f'turret-{i:03d}-'+('ids.png' if mask else 'raw.png')));bpy.ops.render.render(write_still=True)
pivot.rotation_euler.z=0;bpy.context.view_layer.update();q=world_to_camera_view(s,cam,Vector((0,0,0)))
report={'pass':P,'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'frame_size':[256,256],'density':20,'pivot_pixel':[q.x*256,(1-q.y)*256],'camera_elevation':30,'missile_elevation':18,'missile_count':4,'missile_length':4.3,'missile_tail_positions':[list(t) for t in tails],'facings':facings,'groups':groups,'palette':pal,'objects':[{'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'family':o['family']} for o in objects],'fixed_base':'../source-study/base.png','radar_holdout':P>=10,'runtime_installed':False,'hidden_surfaces':'Inferred: rear mechanisms, underside, staggered rail spacing, radar depth.'}
(O/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print('NOD_SAM_PASS_COMPLETE',P,len(objects))
