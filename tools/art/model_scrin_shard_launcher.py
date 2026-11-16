"""Source-colored native Scrin shard launcher, split at a mechanically clear yaw bearing.
Background Blender only. Every pass is retained; no runtime installation here.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
P=argparse.ArgumentParser();P.add_argument('--pass',dest='stage',type=int,required=True);P.add_argument('--all',action='store_true');a=P.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';SRC=ART/'shard_launcher-332414650-0.png';OUT=ART/f'pass-{a.stage:02d}';OUT.mkdir(exist_ok=True)
S=a.stage;D=28.;SIZE=384 if S>=15 else 320;GROUND=280 if S>=15 else 216 if S>=11 else 224;PIVOT=2.14 if S>=8 else 1.32
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=SIZE;scene.render.resolution_percentage=100;scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.filter_size=.01;scene.render.dither_intensity=0;scene.eevee.taa_render_samples=1;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
fixed=bpy.data.collections.new('FIXED / foundation, roots and lower bearing');moving=bpy.data.collections.new('YAW / all upper organs, vascular returns and crystal magazine');scene.collection.children.link(fixed);scene.collection.children.link(moving)
pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,PIVOT)
colors=[tuple(c) for c in json.loads((ART/'reference-review/palette.json').read_text())]
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def linear(c):
 v=c/255;return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
families={
 'shell':[(13,14,23),(22,22,33),(32,31,45),(43,41,57),(54,52,70),(66,65,83),(80,80,99),(99,102,121),(124,135,150)],
 'vein':[(24,13,26),(38,21,38),(57,31,50),(78,40,63),(103,54,78),(126,69,94),(147,88,113),(174,112,134),(197,140,161)],
 'cyan':[(7,38,61),(10,62,85),(13,88,114),(19,116,143),(25,153,178),(31,191,213),(63,224,233),(122,244,245),(195,252,250)],
 'earth':[(17,17,23),(26,25,31),(38,36,40),(51,47,48),(64,58,56),(78,71,66),(94,87,76),(113,104,90),(134,126,109)]}
mats={};palette={}
for family,tones in families.items():
 for i,rgb in enumerate(tones):
  name=family+str(i);rgb=nearest(rgb);palette[name]=rgb;m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(linear,rgb),1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);mats[name]=m
source_image=bpy.data.images.load(str(SRC));source_image.pack()
texture=bpy.data.materials.new('bounded original armor cuts');texture.use_nodes=True;nodes=texture.node_tree.nodes;nodes.clear();tex=nodes.new('ShaderNodeTexImage');tex.image=source_image;tex.interpolation='Closest';tex.extension='CLIP';em=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial');texture.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);texture.node_tree.links.new(em.outputs[0],out.inputs[0]);mats['source']=texture
objects=[];notes=[]
def mesh(name,vs,fs,family='shell',fixed_part=False,bias=0,shade=None):
 d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update();o=bpy.data.objects.new(name,d);(fixed if fixed_part else moving).objects.link(o)
 if not fixed_part:o.parent=pivot;o.location=-pivot.location
 for m in mats.values():d.materials.append(m)
 o['fixed']=fixed_part;o['family']=family;o['bias']=bias;o['shade']=-1 if shade is None else shade;objects.append(o);return o

def smooth(points,steps=4):
 p=[Vector(v) for v in points];out=[]
 for j in range(len(p)-1):
  p0=p[max(0,j-1)];p1=p[j];p2=p[j+1];p3=p[min(len(p)-1,j+2)]
  for k in range(steps):
   t=k/steps;out.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
 return out+[p[-1]]

def organ(name,points,radii,family='shell',fixed_part=False,bias=0,n=12,steps=4,shade=None):
 path=smooth(points,steps);verts=[]
 for i,p in enumerate(path):
  tangent=(path[min(i+1,len(path)-1)]-path[max(0,i-1)]).normalized();ref=Vector((1,0,0))
  if abs(tangent.dot(ref))>.9:ref=Vector((0,1,0))
  u=(ref-tangent*ref.dot(tangent)).normalized();v=tangent.cross(u).normalized()
  t=i/(len(path)-1)*(len(radii)-1);j=min(int(t),len(radii)-2);f=t-j
  r0,r1=radii[j],radii[j+1];r0=(r0,r0) if isinstance(r0,(int,float)) else r0;r1=(r1,r1) if isinstance(r1,(int,float)) else r1
  ru=r0[0]*(1-f)+r1[0]*f;rv=r0[1]*(1-f)+r1[1]*f
  for k in range(n):
   th=k*math.tau/n;verts.append(tuple(p+u*ru*math.cos(th)+v*rv*math.sin(th)))
 faces=[(i*n+k,i*n+(k+1)%n,(i+1)*n+(k+1)%n,(i+1)*n+k) for i in range(len(path)-1) for k in range(n)]
 faces.extend([tuple(range(n-1,-1,-1)),tuple((len(path)-1)*n+k for k in range(n))]);return mesh(name,verts,faces,family,fixed_part,bias,shade)

def vein(name,pts,r=.065,fixed_part=False,bias=0):
 return organ(name,pts,[r*.8,r,r*.8],family='vein',fixed_part=fixed_part,bias=bias,n=8,steps=5)

def ring(name,z,r,h,inner=None,fixed_part=False,family='shell',bias=0,n=48):
 inner=inner or r*.78;vs=[(rad*math.cos(k*math.tau/n),rad*math.sin(k*math.tau/n),zz) for zz,rad in [(z,r),(z+h,r),(z,inner),(z+h,inner)] for k in range(n)];fs=[]
 for k in range(n):
  j=(k+1)%n;fs.extend([(k,j,n+j,n+k),(n+k,n+j,3*n+j,3*n+k),(2*n+k,3*n+k,3*n+j,2*n+j),(k,2*n+k,2*n+j,j)])
 return mesh(name,vs,fs,family,fixed_part,bias)

def facet(name,center,height,width,lean=(0,0),fixed_part=False):
 if S>=6:height*=1.22;width*=1.42
 c=Vector(center);vs=[];n=5
 for z,r,dx,dy in [(0,width*.68,0,0),(height*.47,width,lean[0]*.28,lean[1]*.28),(height*.79,width*.52,lean[0]*.68,lean[1]*.68)]:
  for k in range(n):
   t=k*math.tau/n;vs.append(tuple(c+Vector((r*math.cos(t)+dx,r*.62*math.sin(t)+dy,z))))
 vs.append(tuple(c+Vector((lean[0],lean[1],height))));fs=[(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(2) for k in range(n)];fs += [(2*n+k,2*n+(k+1)%n,3*n) for k in range(n)]
 return mesh(name,vs,fs,'cyan',fixed_part,2 if S<10 else 0)

def plate_arc(name,y0,y1,r=.76,center=(-.25,4.65),bias=0,lo=55,hi=305):
 if S>=2 and lo==55:lo,hi=108,320
 if S>=12:r*=1.15
 # Open longitudinal cradle, not a closed tube or a flat emitter disk.
 n=18;vs=[]
 for y,rr in [(y0,r),(y1,r*.94),(y0,r-.12),(y1,r*.94-.12)]:
  for i in range(n+1):
   t=math.radians(lo+(hi-lo)*i/n);vs.append((center[0]+rr*math.cos(t),y,center[1]+rr*math.sin(t)))
 q=n+1;fs=[]
 for i in range(n):fs.extend([(i,i+1,q+i+1,q+i),(q+i,q+i+1,3*q+i+1,3*q+i),(2*q+i,3*q+i,3*q+i+1,2*q+i+1)])
 fs.extend([(0,q,3*q,2*q),(n,2*q+n,3*q+n,q+n)])
 if S>=4:fs=[tuple(reversed(f)) for f in fs]
 return mesh(name,vs,fs,bias=bias)

# FIXED: broad, low biomechanical foot with splayed buttresses and grounded roots.
outline=[(-2.6,-2.3),(-2.3,-2.6),(2.3,-2.6),(2.6,-2.3),(2.6,2.3),(2.3,2.6),(-2.3,2.6),(-2.6,2.3)]
if S<4:
 verts=[(x,y,z) for z in (.035,.12) for x,y in outline];mesh('Fixed broken octagonal foundation',verts,[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,8+(i+1)%8,8+i) for i in range(8)],'earth',True,-1)
else:
 for i in range(4 if S>=8 else 8):
  t=(i+.5)*math.tau/(4 if S>=8 else 8);u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0));poly=[u*r+v*w for r,w in [(1.0,-.35),(2.8,-.60),(3.40,-.38),(3.47,.10),(3.10,.54),(1.0,.35)]]
  verts=[tuple(q+Vector((0,0,z))) for z in (.035,.12) for q in poly];n=len(poly)
  mesh(f'Fixed separate ground pad {i}',verts,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)],'earth',True,-1)
organ('Fixed ribbed core',[(0,0,.12),(0,0,.55),(0,0,1.02)],[(1.18,1.18),(1.13,1.13),(.95,.95)],fixed_part=True,n=24,bias=-1)
for i in range(4 if S>=8 else 8):
 t=(i+.5)*math.tau/(4 if S>=8 else 8);u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
 points=[u*.86+Vector((0,0,1.03)),u*1.3+Vector((0,0,.75)),u*1.9+Vector((0,0,.28)),u*2.3+Vector((0,0,.14))]
 organ(f'Fixed articulated talon {i}',points,[(.24,.28),(.32,.28),(.22,.18),(.08,.06)],fixed_part=True,bias=0)
 if S>=2:
  for side in (-1,1):
   pts=[u*.65+v*side*.24+Vector((0,0,.83)),u*1.25+v*side*.39+Vector((0,0,.48)),u*1.92+v*side*.48+Vector((0,0,.13)),u*2.44+v*side*.62+Vector((0,0,.10)),u*2.57+v*side*.44+Vector((0,0,.09))]
   vein(f'Fixed rooted tentacle {i}/{side}',pts,.075,True)
 if S>=7:
  for j in range(3):
   c=u*(1.12+j*.31)+Vector((0,0,.83-j*.22));organ(f'Fixed talon armor scute {i}/{j}',[c-v*.22,c+u*.18,c+v*.22],[.035,.095,.035],fixed_part=True,bias=2,n=8)
  for j in range(5):
   z=.27+j*.115;c=u*1.14+Vector((0,0,z));organ(f'Fixed radial gill {i}/{j}',[c-v*.27,c,c+v*.27],[.035,.045,.035],fixed_part=True,bias=2,n=8,steps=2)
ring('Fixed yaw annulus',1.05,.96,.12,.64,True,bias=1)
ring('Dark contact race',1.18,.80,.055,.61,True,bias=-3)
# AIR GAP: fixed <=1.235, moving starts >=1.36. Nothing bridges it.
ring('Rotating inner bearing',1.37,.73,.14,.52,bias=1)
organ('Rotating organic neck',[(0,0,1.53),(-.17,-.32,2.16),(-.35,-.80,3.12),(-.30,-1.25,4.17)],[(.79,.68),(.73,.58),(.58,.60),(.80,.72)],n=20)
for side in (-1,1):
 x=side*.66
 organ(f'Rotating curved side carapace {side}',[(x,-.02,1.64),(x*1.17,-.08,2.18),(x*.9,-.64,3.0),(x*.82,-1.29,4.07)],[(.17,.24),(.28,.35),(.24,.33),(.22,.32)],bias=1)
 if S>=3:
  for j in range(7):
   z=1.72+j*.30;y=-.04-(z-1.72)*.47
   organ(f'Neck overlapping chitin rib {side}/{j}',[(x*.8,y+.28,z),(x*1.16,y,z+.12),(x*.8,y-.25,z+.20)],[.045,.075,.045],bias=2,steps=3,n=8)
 if S>=4:
  # Upper hoses curl back into the rotating collar; never terminate on fixed feet.
  for j in range(3):
   xx=side*(.74+j*.12);vein(f'Yaw-local vascular return {side}/{j}',[(xx,-1.28,4.28),(xx*1.34,-1.52,3.48),(xx*1.08,-1.01,2.65),(xx*.80,-.42,1.83),(xx*.57,-.12,1.53)],.057+j*.01)
  organ(f'Cyan cervical slit {side}',[(side*.69,-.12,1.94),(side*.76,-.32,2.40),(side*.66,-.64,2.90)],[.035,.065,.025],'cyan',bias=1,n=6)
# High receiver: asymmetric layered organic cocoon with deep axle recesses.
organ('Main receiver cocoon',[(-.25,-2.08,4.35),(-.25,-1.55,4.63),(-.25,-.85,4.67),(-.25,-.20,4.65)],[(.51,.49),(.83,.74),(.77,.70),(.62,.58)],n=20)
for side in (-1,1):
 x=-.25+side*.85
 organ(f'Receiver side axle recess {side}',[(x-side*.07,-1.46,4.37),(x+side*.13,-1.46,4.37)],[(.56,.56),(.56,.56)],bias=-3,n=32,steps=1)
 if S>=3:
  for k in range(10):
   t=k*math.tau/10;pts=[(x+side*.15,-1.46+.40*math.cos(t),4.37+.40*math.sin(t)),(x+side*.17,-1.46+.48*math.cos(t+.22),4.37+.48*math.sin(t+.22))]
   organ(f'Azimuth cheek radial rib {side}/{k}',pts,[.035,.035],bias=2,n=6,steps=1)
  organ(f'Cheek central armored seed {side}',[(x+side*.16,-1.46,4.37),(x+side*.20,-1.46,4.37)],[.21,.16],bias=0,n=12,steps=1)
# Long split chitin accelerator with exposed luminous crystalline spine.
barrel_end=3.45 if S<2 else 3.65
plate_arc('Deep accelerator trough',-.60,barrel_end,.67,bias=-2)
for i in range(5 if S<6 or S>=22 else 8):
 count=5 if S<6 or S>=22 else 8;lo=-.48+i*(barrel_end+.45)/count;hi=lo+(barrel_end+.45)/count*.85
 plate_arc(f'Accelerator overlapping dorsal armor {i}',lo,hi,.80 if i<count-1 else .86,bias=1 if i%3==0 else 0)
 if S>=6:
  plate_arc(f'Accelerator bright beveled lip {i}',hi-.045,hi,.825,bias=2,lo=60,hi=300)
# Crystal beam is a modeled faceted spear housed inside the channel, not an effect pasted on.
base=Vector((-.25,-.65,4.65));tip=Vector((-.20,4.42 if S<2 else 4.68,4.88));axis=(tip-base).normalized();u=Vector((1,0,0));v=axis.cross(u).normalized();verts=[]
sections=[(0,.07),(1.3,.22),(3.8,.37),(4.65,.32)] if S<5 else [(0,.025),(.8,.08),(1.4,.14),(2.1,.22),(2.7,.31),(3.4,.39),(4.0,.37),(4.65,.26)]
for j,(dist,r) in enumerate(sections):
 c=base+axis*dist
 if S>=2:r*=1.65
 for k in range(5):
  t=k*math.tau/5+(0 if S<5 else (.12 if j%2 else -.04));verts.append(tuple(c+u*r*math.cos(t)+v*r*.74*math.sin(t)))
verts.append(tuple(tip));faces=[]
for j in range(len(sections)-1):
 for k in range(5):
  q=(j*5+k,j*5+(k+1)%5,(j+1)*5+(k+1)%5,(j+1)*5+k)
  faces.extend([q] if S<5 else [(q[0],q[1],q[2]),(q[0],q[2],q[3])])
last=(len(sections)-1)*5;faces.extend([(last+k,last+(k+1)%5,len(verts)-1) for k in range(5)])
mesh('Long faceted cyan shard',verts,faces,'cyan',bias=2 if S<4 else 0 if S>=12 else -1)
if S>=5:
 for j in range(7):
  y=.05+j*.53;organ(f'Crystal branching filament {j}',[(.16,y,4.66),(.34,y+.25,4.88),(.31,y+.62,4.78)],[.020,.032,.006],'cyan',bias=3,n=5)
for side in (-1,1):
 if S>=4:
  for j in range(3):
   x=-.25+side*(.62+j*.10)
   vein(f'Upper accelerator artery {side}/{j}',[(x,-1.92,4.60),(x*1.15,-1.25,4.97),(x*1.12,-.35,4.51),(x,1.0+j*.32,4.22),(x*.89,1.53+j*.28,4.35)],.055)
# Four-shard rotating side magazine, deep scalloped sockets and embracing veins.
organ('Rotating lower crystal magazine',[(1.54,-.65,2.12),(1.65,.25,2.21),(1.65,1.23,2.29),(1.55,1.68,2.38)],[(.39,.42),(.57,.51),(.54,.48),(.31,.32)],n=16)
for i in range(4):
 y=-.53+i*.64;z=2.47+i*.08
 organ(f'Magazine dark recessed socket {i}',[(1.67,y,z-.10),(1.67,y,z+.05)],[.35,.29],bias=-3,n=16,steps=1)
 facet(f'Magazine shard {i}',(1.67,y,z),.94+(.12 if i%2 else 0),.235,(-.10,.16))
 if S>=3:
  vein(f'Magazine raised socket collar {i}',[(1.36,y,z+.05),(1.46,y-.22,z-.11),(1.83,y-.22,z-.13),(1.98,y,z+.12)],.075)
 if S>=5:
  vein(f'Magazine vascular harness {i}',[(1.97,y,2.45+i*.08),(2.12,y-.17,2.08),(1.73,y-.32,1.70),(1.20,y-.22,1.66),(.65,y*.5,1.57)],.065)
organ('Magazine bridge to rotating neck',[(.45,.12,1.65),(1.02,.23,1.66),(1.55,.44,1.85)],[.20,.23,.20],bias=-1)
if S>=6:
 # Continuous shell ridges and a substantial raised ring around each recessed axle.
 for side in (-1,1):
  x=-.25+side*1.02;n=32;vs=[]
  for xx,rad in [(x,.66),(x+side*.08,.64),(x,.47),(x+side*.08,.46)]:
   for k in range(n):
    t=k*math.tau/n;vs.append((xx,-1.86+rad*math.cos(t),4.62+rad*math.sin(t)))
  fs=[]
  for k in range(n):
   j=(k+1)%n;fs.extend([(k,j,n+j,n+k),(n+k,n+j,3*n+j,3*n+k),(2*n+k,3*n+k,3*n+j,2*n+j)])
  mesh(f'Cheek raised organic annulus {side}',vs,fs,bias=2)
  organ(f'Accelerator longitudinal shell ridge {side}',[(-.25+side*.48,-.50,4.97 if side<0 else 4.10),(-.25+side*.54,1.55,5.12 if side<0 else 4.07),(-.25+side*.49,3.55,5.25 if side<0 else 4.16)],[.075,.090,.035],bias=2,n=8)
 for i in range(4):
  y=-.53+i*.64;z=2.47+i*.08
  for side in (-1,1):
   organ(f'Magazine embracing armor claw {i}/{side}',[(1.67+side*.26,y-.08,z-.30),(1.67+side*.39,y-.07,z+.05),(1.67+side*.32,y+.02,z+.39),(1.67+side*.17,y+.11,z+.55)],[.12,.13,.075,.014],bias=1,n=10)
# High-pass detail: coherent panels, grooves, spinal spikes and protected cables.
if S>=8:
 for side in (-1,1):
  for j in range(5):
   y=-1.94+j*.29;z=4.92+math.sin(j*.7)*.17
   organ(f'Rear crown layered plate {side}/{j}',[(-.25+side*.55,y,z-.08),(-.25+side*.49,y+.11,z+.14),(-.25+side*.23,y+.18,z+.19)],[.10,.15,.05],bias=1,n=8,steps=3)
  for j in range(3):
   y=-1.74+j*.35;z=3.52+j*.18
   organ(f'Receiver rear fin {side}/{j}',[(side*.68,y,z),(side*.96,y-.34,z+.24),(side*1.22,y-.53,z+.38)],[.10,.09,.006],bias=1,n=7)
  for j in range(5):
   y=-.32+j*.27
   organ(f'Magazine gill and inset rim {side}/{j}',[(1.65+side*.52,y,1.98),(1.65+side*.55,y+.11,2.12)],[.035,.038],bias=2,n=6,steps=1)
if S>=9:
 # Ribbed conduits: physical collar rings on deliberately exposed vascular segments.
 for side in (-1,1):
  for j in range(8):
   z=1.93+j*.18;x=side*(.78+(z-1.93)*.13);y=-.55-(z-1.93)*.63
   organ(f'Neck conduit armored stitch {side}/{j}',[(x-.06,y,z),(x,y-.035,z+.03),(x+.06,y,z+.04)],[.021,.028,.021],bias=3,n=6,steps=2)
 # Recessed fluted side plaques break large surfaces, instead of random wear/noise.
 for side in (-1,1):
  for j in range(6):
   yy=-1.84+j*.16;organ(f'Receiver fine longitudinal engraving {side}/{j}',[(-.25+side*.64,yy,4.90),(-.25+side*.45,yy+.13,5.12)],[.014,.015],bias=-3,n=5,steps=1)
if S>=10:
 # Terminal jaws curl around the shard without flattening/capping the bore.
 for side in (-1,1):
  organ(f'Terminal split claw {side}',[(-.25+side*.53,3.17,4.53),(-.25+side*.63,3.56,4.56),(-.25+side*.46,3.78,4.77),(-.25+side*.28,3.83,4.85)],[.095,.115,.075,.018],bias=2,n=10)
  vein(f'Closed receiver return curl {side}',[(-.25+side*.71,-1.90,4.62),(-.25+side*.95,-2.10,4.09),(-.25+side*.89,-1.81,3.56),(side*.54,-1.03,2.83)],.072)
if S>=7:
 # Four principal armored legs, with convex shin plates, inset seams and knee nodes.
 for i in range(4):
  t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  for j in range(3):
   c=u*(1.0+j*.42)+Vector((0,0,.96-j*.28))
   organ(f'Fixed principal leg armor {i}/{j}',[c-v*.28,c+u*.12+Vector((0,0,.13)),c+u*.30],[.11,(.39,.25),.025],fixed_part=True,bias=1,n=12,steps=5)
   if j<2:
    for side in (-1,1):
     pts=[c+v*side*.27+u*.06+Vector((0,0,.11)),c+v*side*.30+u*.34+Vector((0,0,-.09)),c+v*side*.18+u*.46+Vector((0,0,-.25))]
     vein(f'Fixed leg protected vessel {i}/{j}/{side}',pts,.045,True)
  for j in range(6):
   c=u*1.09+Vector((0,0,.20+j*.115))
   organ(f'Fixed central rib cage {i}/{j}',[c-v*.34,c+u*.055,c+v*.34],[.035,.05,.035],fixed_part=True,bias=2,n=8)

# Pass02 silhouette/attachment correction after reference and side/rear inspection.
if S>=2:
 for o in objects:
  for vertex in o.data.vertices:
   v=vertex.co
   if o['fixed']:
    if S>=4 and not o.name.startswith('Fixed separate ground pad'):
     radius=math.hypot(v.x,v.y);stretch=1+max(0,radius-1)*.20
     v.x*=stretch;v.y*=stretch
    v.x*=1.13;v.y*=1.13
    if not o.name.startswith('Fixed broken'):v.z*=1.50
   else:
    if o.name.startswith(('Main receiver','Receiver side','Azimuth cheek','Cheek central','Rear crown','Receiver rear','Receiver fine')):
     v.y-=.40;v.z+=.25
    if o.name.startswith(('Magazine','Rotating lower crystal')):v.z+=.55
    if o.name.startswith('Rotating inner bearing'):v.z+=.77 if S>=8 else .65
    # The low collar/neck is cut at a horizontal rotary plane; no tip crosses it.
    minimum=2.14 if S>=8 else 2.02
    if v.z<minimum:v.z=minimum

if S>=8:
 # Native-density bounded source cuts on actual convex leg armor, never reference
 # projection across unrelated surfaces. All other faces keep volumetric shading.
 for o in objects:
  if not o.name.startswith('Fixed principal leg armor'):continue
  i,j=map(int,o.name.rsplit(' ',1)[1].split('/'));t=(i+.5)*math.tau/4;radial=Vector((math.cos(t),math.sin(t),0));tangent=Vector((-radial.y,radial.x,0))
  crop=[(119,164,139,187),(120,188,138,210),(115,209,135,231)][j];x0,y0,x1,y1=crop
  us=[vertex.co.dot(tangent) for vertex in o.data.vertices];zs=[vertex.co.z for vertex in o.data.vertices];uc=(max(us)+min(us))/2;zc=(max(zs)+min(zs))/2
  uv=o.data.uv_layers.new(name='bounded source cut at native pixel density');textured=[]
  for f in o.data.polygons:
   points=[((x0+x1)/2+(o.data.vertices[k].co.dot(tangent)-uc)*D,(y0+y1)/2-(o.data.vertices[k].co.z-zc)*D) for k in f.vertices]
   if f.normal.dot(radial)<.25 or not all(x0<=x<x1 and y0<=y<y1 for x,y in points):continue
   for li,(x,y) in zip(f.loop_indices,points):uv.data[li].uv=(x/256,1-y/256)
   textured.append(f.index)
  o['texture_faces']=textured;o['source_cut']=list(crop)

if S>=11:
 sys.path.insert(0,str(Path(__file__).resolve().parent))
 from refine_scrin_shard_launcher import apply
 apply(globals())
if S>=18:
 from rebuild_scrin_shard_source import apply as rebuild_source
 rebuild_source(globals())
if S>=23:
 from refine_scrin_shard_pass23 import apply as refine_current
 refine_current(globals())
if S>=14:
 from finish_scrin_shard_materials import apply as finish_chitin
 finish_chitin(globals())
if S>=30:
 from finish_scrin_shard_pass30 import apply as finish_relief
 finish_relief(globals())
if S>=15:
 from elevate_scrin_shard_launcher import apply as elevate
 elevate(globals())
if S>=28:
 from refine_scrin_shard_pass23 import finalize
 finalize(globals())
if S>=31:
 from finish_scrin_shard_source_materials import apply as finish_source_materials
 finish_source_materials(globals())

# Orthographic native rendering. Source elevation matches rectangular-isometric sprites.
target=Vector((0,0,(GROUND-SIZE/2)/(D*math.sqrt(3)/2)));bpy.ops.object.camera_add();camera=bpy.context.object;camera.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=SIZE/D;scene.camera=camera
if S>=36:
 # Keep projection/geometry unchanged; improve depth precision for touching painted surfaces.
 camera.data.clip_start=1;camera.data.clip_end=32
names=list(mats);light=Vector((-.35,-.55,.86)).normalized()
def relight():
 for o in objects:
  for f in o.data.polygons:
   if f.index in o.get('texture_faces',[]):
    f.material_index=names.index(o.get('texture_material','source'));continue
   if o['shade']>=0:t=o['shade']
   else:
    normal=(o.matrix_world.to_3x3()@f.normal).normalized();l=normal.dot(light)
    t=max(0,min(8,int((3.0+3.8*l) if S<2 else (3.8+3.2*l))+o['bias']))
    if f.index in o.get('inner_lit_faces',[]):t=min(8,t+2)
    if f.index in o.get('rim_lit_faces',[]):t=min(8,t+4)
    if o['family']=='cyan':t=max(2,t)
    if S>=5 and o.name=='Long faceted cyan shard':t=max(2,min(7,t+(1 if f.index%3==0 else -1 if f.index%3==1 else 0)))
   f.material_index=names.index(o['family']+str(t))
ids={}
for index,o in enumerate(objects,1):
 m=bpy.data.materials.new('part-id '+o.name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(linear(index%256),linear(index//256),0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);ids[o.name]=m

def render(name,angle=0,part='assembly',mask=False):
 pivot.rotation_euler.z=angle*math.pi/16;bpy.context.view_layer.update();relight()
 bpy.context.view_layer.layer_collection.children[fixed.name].holdout=S>=18 and part=='turret'
 for o in objects:
  o.hide_render=(part=='base' and not o['fixed']) or (part=='turret' and o['fixed'] and S<18)
  for j,key in enumerate(names):o.data.materials[j]=ids[o.name] if mask else mats[key]
 scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('reference-raw.png',30 if S>=23 else 0);render('reference-ids.png',30 if S>=23 else 0,mask=True)
for i in (8,16,24):render(f'assembly-{i:02d}-raw.png',i);render(f'assembly-{i:02d}-ids.png',i,mask=True)
if a.all:
 render('base-raw.png',part='base');render('base-ids.png',part='base',mask=True)
 for i in range(32):
  render(f'turret-{i:02d}-raw.png',i,'turret');render(f'turret-{i:02d}-ids.png',i,'turret',True)
  if S>=43:
   render(f'native-full-{i:02d}-raw.png',i);render(f'native-full-{i:02d}-ids.png',i,mask=True)
 render('base-control-raw.png',11,'base')
pivot.rotation_euler.z=0
bpy.context.view_layer.layer_collection.children[fixed.name].holdout=False
for o in objects:
 o.hide_render=False
 for j,key in enumerate(names):o.data.materials[j]=mats[key]
bpy.context.view_layer.update();relight();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'shard-launcher.blend'))
fixedmax=max(v.co.z for o in objects if o['fixed'] for v in o.data.vertices);movingmin=min(v.co.z for o in objects if not o['fixed'] for v in o.data.vertices)
metadata={'pass':S,'reference_facing':30 if S>=23 else 0,'notes':notes,'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'density':D,'frame_size':SIZE,'ground_origin':[SIZE/2,GROUND],'pivot':[0,0,PIVOT],'source_palette':palette,'objects':len(objects),'fixed_max_z':fixedmax,'moving_min_z':movingmin,'rotation_clearance':movingmin-fixedmax,'bearing_clearance':movingmin-max(v.co.z for o in objects if o['fixed'] and not o.get('fixed_battery',False) for v in o.data.vertices),'veins_entirely_parented_to_own_part':True,'muzzle_model':list(tip),'elevation_degrees':globals().get('elevation_degrees'),'all32':a.all,'runtime_installed':False,'inferred':'rear/hidden faces and internal bearing; source provides one view'}
(OUT/'geometry.json').write_text(json.dumps(metadata,indent=2)+'\n');print('SCRIN_SHARD_PASS',S,'OBJECTS',len(objects),'CLEARANCE',movingmin-fixedmax)
