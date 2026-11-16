"""Compact 1x1 Soviet SAM reconstruction, staged native Blender study. No runtime edits."""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/soviet sam site';SRC=F/'sam_site-887352960-0.png'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='stage',type=int,required=True);p.add_argument('--all',action='store_true');a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);P=a.stage;O=F/f'pass-{P:02d}';O.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=192;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.filter_size=.01;s.render.dither_intensity=0;s.eevee.taa_render_samples=1;s.view_settings.view_transform='Standard';s.view_settings.look='None'
col=bpy.data.collections.new('SOVIET SAM / modeled hardware');s.collection.children.link(col);pivot=bpy.data.objects.new('turret_pivot',None);col.objects.link(pivot);pivot.location.z=1.9
image=bpy.data.images.load(str(SRC));image.pack();raw=list(image.pixels[:]);colors=set()
# Palette samples constrained to the source launcher/pedestal, not scenery projection.
for yy in range(22,210):
 for xx in range(47,207):
  k=((255-yy)*256+xx)*4;c=tuple(round(v*255) for v in raw[k:k+3])
  if max(c)-min(c)<50 or c[0]>c[1]*1.25 or (c[0]>100 and c[1]>90 and c[2]<c[1]*.8):colors.add(c)
def near(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(x):
 x/=255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
def material(name,rgb):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(lin,rgb),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
mats={};pal={}
for family,values in {'steel':[(24,26,31),(40,43,51),(61,65,76),(83,89,101),(117,126,132),(157,167,169)],'white':[(81,86,93),(117,124,130),(153,162,167),(184,193,195),(210,216,215),(235,235,230)],'yellow':[(71,64,34),(101,90,45),(139,124,60),(175,158,79),(207,188,103),(230,213,135)],'red':[(43,24,26),(68,28,30),(104,36,35),(143,44,40),(181,57,50),(211,77,64)],'concrete':[(55,57,58),(78,81,83),(106,111,114),(132,139,141),(158,165,166),(183,190,187)]}.items():
 for i,c in enumerate(values):key=family+str(i);pal[key]=near(c);mats[key]=material(key,pal[key])
names=list(mats);objects=[];groups={};part='base'
def mesh(name,vs,fs,family='steel',bias=0,group=None):
 d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update();o=bpy.data.objects.new(name,d);col.objects.link(o)
 if part=='turret':o.parent=pivot
 for m in mats.values():d.materials.append(m)
 o['family']=family;o['bias']=bias;o['part']=part;o['surface_id']=groups.setdefault(group or name,len(groups)+1);objects.append(o);return o
def tube(name,start,end,r,inner=0,n=16,family='steel',bias=0,group=None):
 start,end=Vector(start),Vector(end);axis=(end-start).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);rings=[(start,r),(end,r)]+([(start,inner),(end,inner)] if inner else []);vs=[]
 for c,rr in rings:
  for i in range(n):vs.append(tuple(c+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))))
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.append((i,j,n+j,n+i))
  if inner:fs.extend([(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
 if not inner:fs.extend([tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))])
 return mesh(name,vs,fs,family,bias,group)
def beam(name,start,end,w,h,family='steel',bias=0):
 start,end=Vector(start),Vector(end);axis=(end-start).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);vs=[tuple(c+u*x*w/2+v*y*h/2) for c in (start,end) for x,y in ((-1,-1),(1,-1),(1,1),(-1,1))];return mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],family,bias)
def box(name,c,size,family='steel',bias=0):
 c=Vector(c);dims=Vector(size)/2;vs=[tuple(c+Vector((x*dims.x,y*dims.y,z*dims.z))) for z in (-1,1) for y in (-1,1) for x in (-1,1)];return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],family,bias)
def bevel(o,w=.04):
 bpy.context.view_layer.objects.active=o;m=o.modifiers.new('Machined edge bevel','BEVEL');m.width=w;m.segments=1;m.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=m.name);return o
# Pass07 restores the reference slab/rack proportion and review pixel density.
DENSITY=26 if P>=7 else 18
SLAB=3.8 if P>=7 else 2.5
bevel(box('Fixed compact foundation',(0,0,.09),(SLAB,SLAB,.18),'concrete'),.07)
if P>=3:
 for side in (-1,1):
  for j in range(4):
   pos=(-.90+j*.60)*(SLAB/2.5)
   box('Foundation hazard X',(side*(SLAB/2-.06),pos,.186),(.08,.29*SLAB/2.5,.008),'yellow',1)
   box('Foundation hazard Y',(pos,side*(SLAB/2-.06),.186),(.29*SLAB/2.5,.08,.008),'yellow',1)
 for j in (-.65,.65):
  box('Slab expansion seam',(j,0,.185),(.015,2.25,.01),'steel',-2)
  box('Slab expansion cross seam',(0,j,.185),(2.25,.015,.01),'steel',-2)
tube('Pedestal base',(0,0,.18),(0,0,.65),.53,n=16)
tube('Pedestal lower bearing',(0,0,.56),(0,0,.69),.58,n=32,family='white',bias=-1)
tube('Fixed mast',(0,0,.68),(0,0,1.79),.46,n=12,family='white',bias=-1)
tube('Fixed traverse socket',(0,0,1.73),(0,0,1.90),.50,n=32,bias=-1)
if P>=2:
 bevel(box('Mast access plate',(0,-.451,1.17),(.61,.045,.65),'steel'),.04)
 box('Mast face inset',(0,-.480,1.17),(.48,.02,.47),'concrete',1)
if P>=6:
 for j in range(4):
  theta=j*math.pi/2+.28;r=Vector((math.cos(theta),math.sin(theta),0));foot=r*(1.60 if P>=7 else 1.03)+Vector((0,0,.22));head=r*.45+Vector((0,0,1.43))
  bevel(box('Outrigger anchor '+str(j),foot,(.30,.30,.13),'steel'),.025)
  beam('Outrigger strut '+str(j),foot+Vector((0,0,.08)),head,.105,.115,'white',-1)
  tube('Anchor pin '+str(j),foot+Vector((0,0,.06)),foot+Vector((0,0,.14)),.07,n=8,bias=1)
if P>=7:
 bevel(box('Fixed service terminal',(.60,-.23,.42),(.29,.34,.46),'steel'),.045)
 box('Terminal dark screen',(.60,-.410,.50),(.20,.015,.17),'steel',-3)
 for j in range(3):box('Terminal status lamp',(.54+j*.06,-.425,.54),(.025,.01,.025),'yellow',1)
 for j in range(4):box('Lower mast grille',(-.25+j*.15,-.508,.41),(.045,.035,.21),'steel',-2)
 for side in (-1,1):
  pts=[(side*.40,.20,1.65),(side*.53,.27,1.45),(side*.55,.27,.76)]
  for i in range(2):tube('Fixed mast conduit',pts[i],pts[i+1],.035,n=8,bias=-2)
if P>=9:
 for side in (-1,1):
  for z in (.79,1.53):tube('Access door bolt',(side*.22,-.49,z),(side*.22,-.52,z),.025,n=6,bias=2)
 for theta in range(0,360,45):
  r=Vector((math.cos(math.radians(theta)),math.sin(math.radians(theta)),0));tube('Traverse ring bolt',r*.51+Vector((0,0,.685)),r*.51+Vector((0,0,.72)),.025,n=6,bias=1)
if P>=9:
 # Reference has a substantial stepped service drum, not a thin pipe on sticks.
 tube('Lower service drum',(0,0,.24),(0,0,.69),.69,n=16,family='steel',bias=0)
 tube('Service drum lower lip',(0,0,.26),(0,0,.32),.73,n=32,family='white',bias=-1)
 tube('Service drum upper lip',(0,0,.65),(0,0,.73),.71,n=32,family='white',bias=-1)
 for j in range(4):
  theta=j*math.pi/2+.28;r=Vector((math.cos(theta),math.sin(theta),0));t=Vector((-r.y,r.x,0))
  foot=r*1.60+Vector((0,0,.28));head=r*.45+Vector((0,0,1.43))
  beam('Reinforced brace sleeve '+str(j),foot,foot+(head-foot)*.43,.18,.19,'steel',0)
  beam('Brace highlight '+str(j),foot+t*.095+Vector((0,0,.04)),foot+(head-foot)*.43+t*.095+Vector((0,0,.04)),.025,.035,'white',0)
  q=r*.696+Vector((0,0,.48))
  beam('Drum recessed vent '+str(j),q-t*.18,q+t*.18,.025,.24,'steel',-3)
  for k in range(5):
   at=q+t*(k-2)*.067+r*.025
   beam('Drum vent slat '+str(j),at-Vector((0,0,.095)),at+Vector((0,0,.095)),.025,.025,'white',-1)
 # Offset front control box with inset keypad and angled rim.
 bevel(box('Front service console',(.70,-.63,.43),(.39,.31,.49),'steel'),.04)
 box('Console inset face',(.70,-.794,.46),(.29,.018,.32),'steel',-3)
 for x in range(3):
  for z in range(3):box('Console keypad',(.61+x*.085,-.81,.38+z*.07),(.045,.015,.036),'yellow' if z==2 else 'white',0)
# Rotating elevation carriage. The basis is explicitly orthogonal, with real pod depth.
part='turret';u=Vector((1,0,0));axis=Vector((0,-math.cos(math.radians(34)),math.sin(math.radians(34))));up=Vector((0,math.sin(math.radians(34)),math.cos(math.radians(34))));center=Vector((0,0,1.05))
def podshape(name,c,w,h,start,end,cut=.10,family='steel',bias=0):
 outline=[(-w/2+cut,-h/2),(w/2-cut,-h/2),(w/2,-h/2+cut),(w/2,h/2-cut),(w/2-cut,h/2),(-w/2+cut,h/2),(-w/2,h/2-cut),(-w/2,-h/2+cut)]
 vs=[tuple(c+axis*t+u*x+up*z) for t in (start,end) for x,z in outline];fs=[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,8+(i+1)%8,8+i) for i in range(8)];return mesh(name,vs,fs,family,bias)
tube('Rotating race',(0,0,.01),(0,0,.15),.49,n=32)
bevel(box('Elevation central yoke',(0,0,.42),(1.05,.66,.60),'white',-1),.09)
tube('Elevation transverse shaft',(-.83,0,.77),(.83,0,.77),.22,n=20)
for colidx,x in enumerate((-.66,.66)):
 for rowidx,z in enumerate((-.57,.57)):
  key=f'{colidx}{rowidx}';c=center+u*x+up*z
  if P==1:podshape('Canister block '+key,c,1.20,1.04,-1.60,1.60,.015)
  else:
   podshape('Canister armored shell '+key,c,1.20,1.04,-1.60,1.60,.12,bias=-1 if P>=8 else 0)
   podshape('Rear end reinforced flange '+key,c,1.24,1.08,-1.65,-1.49,.13,'white',-1)
  podshape('Front dark recessed socket '+key,c,1.23,1.07,1.56,1.68,.12,bias=-2)
  podshape('White launch cover '+key,c,.88,.78,1.68,1.71,.06,'white',2 if P>=2 else 0)
  if P>=2:
   for side in (-1,1):
    q=c+u*side*.555
    beam('Front silver edge frame '+key+str(side),q+axis*1.72-up*.36,q+axis*1.72+up*.36,.085,.075,'white',1)
  if P>=3:
   for side in (-1,1):
    q=c+up*side*.47
    beam('Black cover clasp '+key+str(side),q+axis*1.73-u*.46,q+axis*1.73+u*.46,.16,.11,'steel',-2)
    for j in (-1,1):
     q2=q+u*j*.25+axis*1.80
     beam('Yellow cover latch '+key+str(side)+str(j),q2-u*.09-up*.03,q2+u*.05+up*.03,.06,.04,'yellow',1)
   for t in (-.95,.50):podshape('Canister belt '+key,c,1.235,1.075,t-.045,t+.045,.13,'steel',1)
  if P>=5:
   # Longitudinal corner rails break up the deep canister, not painted scenery.
   for side in (-1,1):
    q=c+u*side*.49+up*.48
    beam('Long upper rail '+key+str(side),q-axis*1.47,q+axis*1.46,.08,.07,'white',0)
   for t in (-1.1,-.35,.45):
    q=c+u*(.615 if x>0 else -.615)+axis*t
    tube('Side armored screw '+key,q,q+u*(.04 if x>0 else -.04),.065,n=8,bias=-2)
  if P>=8:
   # Layered broad-face fields, each a coherent mechanical panel.
   for sign in (-1,1):
    q=c+u*sign*.614-axis*.43
    beam('Recessed side equipment panel '+key+str(sign),q-axis*.71,q+axis*.71,.025,.69,'steel',-1)
    for t in (-.75,-.47,-.19):
     at=c+u*sign*.636+axis*t+up*.21
     beam('Side panel cooling slit '+key+str(sign),at-up*.12,at+up*.12,.04,.02,'steel',-3)
  if P>=8:
   for sign in (-1,1):
    # Separate inset panel fields with bounded rails and access hatch outlines.
    q=c+u*sign*.641-axis*.82
    for h in (-.31,.31):
     beam('Side access horizontal rim '+key,q+up*h-axis*.36,q+up*h+axis*.36,.035,.035,'steel',1)
    for t in (-.36,.36):
     beam('Side access vertical rim '+key,q+axis*t-up*.31,q+axis*t+up*.31,.035,.035,'steel',1)
    q=c+u*sign*.65+axis*.70
    tube('Recessed circular flank port '+key,q,q+u*sign*.025,.13,inner=.085,n=16,bias=0)
 if P>=10:
   # Stepped upper armor fields instead of uninterrupted flat canister roofs.
   podshape('Raised roof armor '+key,c+up*.525,.88,.045,-1.36,1.35,.018,'steel',1)
   for t in (-.90,.43):
    q=c+up*.558+axis*t
    beam('Roof panel cross seam '+key,q-u*.42,q+u*.42,.022,.018,'steel',-2)
   # Thin rim around each ceramic-colored launch cover.
   for sign in (-1,1):
    q=c+u*sign*.421+axis*1.723
    beam('Launch cover bevel '+key,q-up*.32,q+up*.32,.032,.028,'white',0)
   for sign in (-1,1):
    q=c+u*sign*.45+axis*1.82
    for z2 in (-.34,.34):tube('Cover frame screw '+key,q+up*z2,q+up*z2+axis*.03,.025,n=6,bias=1)
   # Recessed rear closure, separated from the structural pale end band.
   podshape('Rear service closure '+key,c,.98,.83,-1.665,-1.650,.10,'steel',-1)
   for qx in (-.28,.28):
    q=c+u*qx-axis*1.68
    beam('Rear locking bar '+key,q-up*.29,q+up*.29,.07,.045,'white',-1)
# Source's two prominent round red inspection/drive ports on the visible flank.
if P>=4:
 for row,z in enumerate((-.57,.57)):
  c=center+u*(1.48 if P>=6 else 1.30)+up*z+axis*(1.65 if P>=6 else .95)
  normal=axis if P>=6 else u;horizontal=u if P>=6 else axis
  if P>=6:
   podshape('Outboard port support '+str(row),center+u*1.34+up*z,.42,.84,.40,1.63,.09,'steel',-1)
   podshape('Port chamfered pale shroud '+str(row),center+u*1.48+up*z,.80,.98,1.55,1.64,.15,'white',0)
  tube('Port outer pale rim '+str(row),c,c+normal*.12,.34,inner=.27,n=20,family='white',bias=0)
  tube('Port deep recess '+str(row),c-normal*.01,c+normal*.055,.26,n=20,bias=-3)
  mesh('Red port triangle '+str(row),[tuple(c+normal*(.135 if P>=7 else .064)+horizontal*t+up*h) for t,h in ((-.20,0),(.11,.12),(.11,-.12))],[(0,1,2)],'red',1)
  for sign in (-1,1):
   q=c+up*sign*.39
   tube('Port rim fastening '+str(row),q,q+normal*.14,.065,n=8,family='white',bias=1)
if P>=5:
 for side in (-1,1):
  q=center+up*.98+u*side*.20+(axis*.85 if P>=11 else Vector((0,0,0)))
  podshape('Upper armored cable junction '+str(side),q,.46,.25,-.15,.65,.05,bias=-1)
  for j in range(3):
   r=q+axis*(j*.22-.07)+up*.135
   beam('Junction caution bar',r-u*.18,r+u*.18,.07,.014,'yellow',0)
if P>=6:
 for sign in (-1,1):
  c=Vector((sign*.80,0,.77));tube('Yoke trunnion',c,c+u*sign*.10,.28,n=20,bias=-1)
  tube('Yoke trunnion cover',c+u*sign*.10,c+u*sign*.13,.21,n=20,family='white',bias=0)
  start=Vector((sign*.35,.20,.16));end=center-axis*.48+Vector((sign*.35,0,-.40));mid=start+(end-start)*.60
  tube('Elevation ram cylinder',start,mid,.10,n=12,bias=-1);tube('Elevation ram chrome rod',mid,end,.045,n=12,family='white',bias=1)
if P>=7:
 for sign in (-1,1):
  pts=[(sign*.53,.3,.2),(sign*.69,.45,.48),(sign*.77,.55,.80),(sign*.77,.65,1.12)]
  for k in range(3):tube('Flexible elevation harness '+str(sign),pts[k],pts[k+1],.036,n=8,bias=-3)
if P>=9:
 for sign in (-1,1):
  beam('Elevation yoke inset seam',(sign*.48,-.35,.20),(sign*.48,-.35,.56),.025,.025,'steel',-2)
  for y in (-.18,.18):tube('Elevation cover fastener',(sign*.92,y,.77),(sign*.96,y,.77),.035,n=6,bias=2)
if P>=11:
 part='base'
 # A continuous recessed border with alternating yellow tabs, like the source slab.
 for side in (-1,1):
  for j in range(12):
   v=-1.70+j*.30
   box('Perimeter hazard X',(side*1.80,v,.192),(.11,.22,.012),'yellow' if j%2==0 else 'steel',0)
   box('Perimeter hazard Y',(v,side*1.80,.192),(.22,.11,.012),'yellow' if j%2==0 else 'steel',0)
 part='turret'
 for x in (-.66,.66):
  for z in (-.57,.57):
   c=center+u*x+up*z
   # Inset lower shade and bright edge describe ceramic cover thickness.
   podshape('Cover inset tonal foot',c-up*.315,.70,.09,1.715,1.725,.018,'white',-1)
   for sign in (-1,1):
    q=c+u*sign*.36+up*.31+axis*1.76
    beam('Cover corner hinge',q-u*.055,q+u*.055,.055,.05,'steel',1)
 # Prominent elevation pivot cap and clevises are distinct from the canisters.
 for sign in (-1,1):
  c=Vector((sign*.94,0,.77))
  tube('Elevation boss concentric ring',c,c+u*sign*.06,.16,inner=.11,n=20,family='white',bias=-1)
  for j in range(4):
   q=c+Vector((0,math.cos(j*math.pi/2)*.20,math.sin(j*math.pi/2)*.20))
   tube('Elevation boss screw',q,q+u*sign*.06,.03,n=6,bias=1)
if P>=10:
 # Source-like wraparound mast panels and service conduits below the yoke.
 part='base'
 for j in range(6):
  theta=j*math.tau/6;r=Vector((math.cos(theta),math.sin(theta),0));t=Vector((-r.y,r.x,0))
  at=r*.47+Vector((0,0,1.15))
  beam('Mast panel upper trim',at+Vector((0,0,.27))-t*.19,at+Vector((0,0,.27))+t*.19,.04,.035,'steel',0)
  beam('Mast panel lower trim',at-Vector((0,0,.27))-t*.19,at-Vector((0,0,.27))+t*.19,.04,.035,'steel',0)
 part='turret'
if P>=12:
 part='turret'
 q=center-up*1.24+axis*.70
 podshape('Underslung armored elevation apron',q,1.0,.30,-.20,.42,.08,'steel',0)
 for j in range(4):
  at=q+u*(j-1.5)*.19+axis*.435
  beam('Apron warning stripe',at-up*.085-u*.025,at+up*.085+u*.025,.075,.025,'yellow',1)
 for sign in (-1,1):
  pts=[Vector((sign*.53,.20,.25)),Vector((sign*.65,.55,.48)),Vector((sign*.65,.75,.90)),center-axis*.8+u*sign*.65-up*.55]
  for k in range(3):tube('Rear flexible power conduit',pts[k],pts[k+1],.045,n=10,bias=-2)
  for k in (1,2):tube('Harness metal collar',pts[k]-Vector((0,0,.035)),pts[k]+Vector((0,0,.035)),.059,n=10,family='white',bias=-1)
 part='base'
 for sign in (-1,1):
  bevel(box('Mast recessed equipment housing',(sign*.51,.12,1.06),(.12,.42,.48),'steel'),.035)
  for j in range(4):
   beam('Mast housing vent',(sign*.58,-.01, .92+j*.08),(sign*.58,.24,.92+j*.08),.025,.025,'white',-1)
 part='turret'
# Orthographic native-density export; common ground center96,146.
bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=(7,-7,math.sqrt(98)/math.sqrt(3));cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=192/DENSITY;cam.data.shift_y=(146-96)/192;s.camera=cam
# Positive camera shift moves origin down in the image.
ids={i:material('Surface '+str(i),(i%256,i//256,0)) for i in groups.values()}
def light(mask):
 key=Vector((-.35,-.55,.88)).normalized()
 for o in objects:
  for j,m in enumerate(mats.values()):o.data.materials[j]=ids[o['surface_id']] if mask else m
  for poly in o.data.polygons:
   shade=max(0,min(5,int(((o.matrix_world.to_3x3()@poly.normal).normalized().dot(key)+1)*2.65)+o['bias']));
   if P>=8:
    # Preserve red inserts, and keep broad dark steel readable as in the source.
    if o['family']=='red':shade=max(3,shade)
    if o['family']=='steel' and o['bias']>=-1:shade=max(2,shade)
   poly.material_index=names.index(o['family']+str(shade))
def render(name,which):
 for o in objects:o.hide_render=o['part']!=which
 for mask in (False,True):
  light(mask);s.render.filepath=str(O/(name+('-ids.png' if mask else '-raw.png')));bpy.ops.render.render(write_still=True)
YAW=0 if P>=12 else 7
pivot.rotation_euler.z=math.radians(YAW);bpy.context.view_layer.update();light(False);bpy.ops.wm.save_as_mainfile(filepath=str(O/'soviet-sam.blend'))
render('base','base');facings=list(range(32)) if a.all else list(range(0,32,4))
for i in facings:
 pivot.rotation_euler.z=math.radians(YAW)+i*math.pi/16;bpy.context.view_layer.update();render(f'turret-{i:03d}','turret')
g={'pass':P,'yaw_start_degrees':YAW,'frame_size':[192,192],'ground_pixel':[96,146],'density':DENSITY,'base_diamond_width':SLAB*math.sqrt(2)*DENSITY,'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'facings':facings,'groups':groups,'palette':pal,'objects':[{'name':o.name,'part':o['part'],'vertices':len(o.data.vertices),'faces':len(o.data.polygons)} for o in objects],'runtime_installed':False,'inferred':'All 3D geometry; rear/underside inferred from visible mechanisms. Source preserved.'}
(O/'geometry.json').write_text(json.dumps(g,indent=2)+'\n');print('SOVIET_SAM_PASS_COMPLETE',P,len(objects),flush=True)
