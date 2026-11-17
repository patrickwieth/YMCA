"""Pass18+: source-shaped armored feet and fixed magazine, no edits to prior passes."""
import bpy,math
from mathutils import Vector

def apply(c):
 objects=c['objects'];mesh=c['mesh'];organ=c['organ'];vein=c['vein'];ring=c['ring'];S=c['S']
 for o in list(objects):
  if o['fixed']:objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
 # The battery and all of its harness belong to the stationary foundation now.
 for o in objects:
  if o.name.startswith(('Magazine','Rotating lower crystal','Yaw magazine','Yaw crown crystal','Yaw crown dark')):
   o.parent=None;o.location=(0,0,0);o['fixed']=True;o['fixed_battery']=True
   for col in list(o.users_collection):col.objects.unlink(o)
   c['fixed'].objects.link(o)
   for v in o.data.vertices:
    v.co.z-=.65
    if S>=21:
     w=max(0,min(1,(v.co.x-.7)/1.3));v.co.x+=.6*w;v.co.y+=.25*w;v.co.z+=1.3*w
   o.data.update()
 if S>=19:
  # The reference soil is olive-grey, absent from the old reduced chitin palette.
  rgb=(113,114,99);c['palette']['earth5']=rgb
  c['mats']['earth5'].node_tree.nodes.get('Emission').inputs[0].default_value=(*[c['linear'](v) for v in rgb],1)
 # Shorter, flared accelerator rather than the long uniform segmented pipe.
 for o in objects:
  if o['fixed']:continue
  if S>=22 and o.name.startswith('Yaw rear'):
   for vertex in o.data.vertices:vertex.co.y=-.8+(vertex.co.y+.8)*.7
   o.data.update()
  gun=any(n in o.name.lower() for n in ('accelerator','long faceted','crystal branching','terminal jaw'))
  if gun:
   for v in o.data.vertices:
    q=v.co;w=max(0,min(1,(q.y+.5)/4.9));q.x=-.25+(q.x+.25)*(1+.16*w);q.y=-.5+(q.y+.5)*.87
   o.data.update()
 c['cannon_root']=Vector((-.25,-.5+(-.65+.5)*.87,4.65))
 c['tip'].x=-.25+(c['tip'].x+.25)*1.16;c['tip'].y=-.5+(c['tip'].y+.5)*.87
 # Irregular soil/root contact, not a solid beveled metal tile.
 outline=[(-2.9,-2.45),(-2.3,-2.85),(-1.4,-2.63),(-.55,-2.88),(.35,-2.7),(1.35,-2.9),(2.6,-2.58),(2.85,-1.9),(2.65,-.6),(2.91,.5),(2.66,1.9),(2.86,2.5),(1.6,2.8),(.5,2.63),(-.7,2.8),(-2,2.6),(-2.85,2.1),(-2.65,.7),(-2.85,-.7)]
 if S>=19:
  outline=[]
  for k in range(32):
   t=k*math.tau/32;d=abs((k%8)-4);r={0:3.45,1:3.20,2:2.35,3:1.92,4:1.86}[d];outline.append((r*math.cos(t),r*math.sin(t)))
 mesh('Fixed broken soil contact',[(0,0,.025)]+[(x,y,.025) for x,y in outline],[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))],'earth',True,shade=5)
 for i in range(16):
  if S>=20 and i%4!=2:continue
  t=i*math.tau/16;r=2.4+(i%3)*.12;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  mesh(f'Fixed ground tonal chip {i}',[tuple(u*r-v*.18+Vector((0,0,.028))),tuple(u*(r+.25)+Vector((0,0,.028))),tuple(u*r+v*.23+Vector((0,0,.028)))],[(0,1,2)],'earth',True,shade=3+i%4)
 # Narrow mechanical core, exposed between four substantial armored standing legs.
 organ('Fixed ribbed mechanical plinth',[(0,0,.12),(0,0,.45),(0,0,1.35),(0,0,1.80)],[(1.22,1.22),(1.35,1.35),(1.21,1.21),(.94,.94)],fixed_part=True,bias=-2,n=16)
 ring('Fixed recessed yaw race',1.88,.85,.065,.62,True,bias=-3)
 for i in range(4):
  t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  def p(r,z,w=0):return u*r+v*w+Vector((0,0,z))
  # Lofted broad shin/knee shell: inset face and beveled perimeter, not rounded petals.
  rows=[(2.65,.12,.32,.12),(2.55,.35,.43,.20),(2.16,1.04,.36,.28),(1.85,1.58,.50,.28),(1.48,1.88,.39,.18)]
  if S>=19:rows=[(r,z,w*(1.45 if S>=21 else 1.2),d) for r,z,w,d in rows]
  vs=[]
  for r,z,w,d in rows:
   for a,b in [(-1,0),(-.72,d),(.72,d),(1,0),(.72,-.12),(-.72,-.12)]:vs.append(tuple(p(r+b,z,w*a)))
  fs=[(j*6+k,j*6+(k+1)%6,(j+1)*6+(k+1)%6,(j+1)*6+k) for j in range(len(rows)-1) for k in range(6)];fs.extend([tuple(range(5,-1,-1)),tuple((len(rows)-1)*6+k for k in range(6))])
  shell=mesh(f'Fixed source armored shin {i}',vs,fs,fixed_part=True,bias=1)
  organ(f'Fixed inset long shin plate {i}',[p(2.70,.25),p(2.58,.55),p(2.35,1.00),p(2.13,1.42)],[(.16,.06),(.25,.10),(.19,.09),(.13,.05)],fixed_part=True,bias=2,n=8)
  organ(f'Fixed dark knee recess {i}',[p(2.07,1.51),p(1.95,1.72)],[(.20,.045),(.15,.045)],fixed_part=True,bias=-3,n=8)
  for j in range(8):
   z=.33+j*.16
   organ(f'Fixed exposed mechanism slat {i}/{j}',[p(1.33,z,-.35),p(1.48,z+.015),p(1.33,z,.35)],[.035,.045,.035],fixed_part=True,bias=1+j%2,n=6)
  for side in (-1,1):
   vein(f'Fixed leg margin vessel {i}/{side}',[p(.8,1.82,side*.24),p(1.65,1.66,side*.54),p(2.08,1.05,side*.44),p(2.52,.30,side*.40),p(2.82,.10,side*.46)],.072,True,bias=1)
   vein(f'Fixed short curling terminal {i}/{side}',[p(2.34,.23,side*.36),p(2.69,.10,side*.61),p(2.96,.09,side*.66),p(3.02,.09,side*.41),p(2.87,.09,side*.37)],.065,True)
  if S>=19:
   # Bounded source armor cut on the front face only, at near-native density.
   uv=shell.data.uv_layers.new(name='Source shin armor bounded cut');textured=[]
   for f in shell.data.polygons:
    if f.normal.dot(u)<.65:continue
    pts=[(128+shell.data.vertices[k].co.dot(v)*25,232-shell.data.vertices[k].co.z*31) for k in f.vertices]
    if not all(110<=x<=147 and 169<=y<=233 for x,y in pts):continue
    for li,(x,y) in zip(f.loop_indices,pts):uv.data[li].uv=(x/256,1-y/256)
    textured.append(f.index)
   shell['texture_faces']=textured
 # Fixed battery support is a ribbed buttress into the plinth, never the turret.
 brace=organ('Fixed battery support buttress',[(1.0,-.1,.65),(2.35,-.2,1.60),(3.25,-.30,2.98)] if S>=21 else [(1.0,-.1,.65),(1.65,-.35,1.13),(2.65,-.55,1.68)],[(.28,.28),(.37,.30),(.38,.23)],fixed_part=True,bias=0,n=10);brace['fixed_battery']=True
 for j in range(5):
  organ(f'Fixed battery brace rib {j}',[(1.55+j*.17,-.70,.65+j*.13),(1.65+j*.17,-.72,1.15+j*.13)],[.045,.045],fixed_part=True,bias=2,n=6)
 if S>=22:
  # Grounded magazine pedestal, source-like armored apron and closed vascular returns.
  support=organ('Fixed magazine standing pedestal',[(3.18,-.12,.14),(3.38,-.19,.75),(3.38,-.28,1.65),(3.25,-.30,2.98)],[(.48,.38),(.50,.34),(.39,.31),(.40,.27)],fixed_part=True,bias=0,n=12);support['fixed_battery']=True
  for j in range(7):
   z=.35+j*.32
   part=organ(f'Fixed magazine pedestal armor step {j}',[(3.65,-.55,z),(3.87,-.18,z+.06),(3.64,.20,z+.12)],[.045,.085,.045],fixed_part=True,bias=2,n=6);part['fixed_battery']=True
  for side in (-1,1):
   part=vein(f'Fixed magazine descending vascular feed {side}',[(3.45,side*.44,2.75),(3.93,side*.49,2.12),(3.87,side*.54,1.32),(3.70,side*.56,.55),(3.36,side*.54,.12)],.07,True,bias=1);part['fixed_battery']=True
 if S>=20:
  # Mechanisms must be BETWEEN the legs, not hidden behind their armor.
  for i in range(4):
   t=i*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
   for j in range(10):
    z=.28+j*.145;r=1.52
    organ(f'Fixed visible interleg louver {i}/{j}',[u*r-v*.38+Vector((0,0,z)),u*(r+.12)+Vector((0,0,z+.015)),u*r+v*.38+Vector((0,0,z))],[.045,.06,.045],fixed_part=True,bias=1+j%3,n=6)
   for side in (-1,1):
    vein(f'Fixed interleg vascular feed {i}/{side}',[u*.8+v*side*.22+Vector((0,0,1.85)),u*1.63+v*side*.46+Vector((0,0,1.30)),u*1.69+v*side*.46+Vector((0,0,.55)),u*1.95+v*side*.51+Vector((0,0,.15))],.065,True,bias=1)
  for i in range(4):
   t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0));zaxis=-u*.82+Vector((0,0,.57));normal=u*.57+Vector((0,0,.82));center=u*2.04+Vector((0,0,1.73));pts=[]
   for rad,depth in [(1,.10),(.72,.11),(.68,.035)]:
    for k in range(12):
     a=k*math.tau/12;pts.append(tuple(center+v*(.26*rad*math.cos(a))+zaxis*(.20*rad*math.sin(a))+normal*depth))
   fs=[(j*12+k,j*12+(k+1)%12,(j+1)*12+(k+1)%12,(j+1)*12+k) for j in range(2) for k in range(12)]
   mesh(f'Fixed beveled knee intake frame {i}',pts,fs,fixed_part=True,bias=2)
   mesh(f'Fixed recessed knee intake {i}',pts[24:], [tuple(range(12))],fixed_part=True,bias=-3)
   for j in (-1,0,1):
    a=center+v*j*.065+normal*.05
    organ(f'Fixed knee intake slat {i}/{j}',[a-zaxis*.10,a+zaxis*.10],[.016,.016],fixed_part=True,bias=1,n=5)
  # Ribbed rear and side receiver details readable in the non-source views too.
  for side in (-1,1):
   for j in range(6):
    y=-2.25+j*.24
    organ(f'Source receiver recessed gill {side}/{j}',[(-.25+side*.80,y,4.66),(-.25+side*.88,y+.05,4.90),(-.25+side*.72,y+.08,5.13)],[.025,.045,.025],bias=-2,n=6)
   vein(f'Source receiver upper arterial return {side}',[(-.25+side*.6,-2.45,4.72),(-.25+side*.91,-2.20,5.02),(-.25+side*.8,-1.3,5.19),(-.25+side*.62,-.62,4.99)],.07,bias=1)
