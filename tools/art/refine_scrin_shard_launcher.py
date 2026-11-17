"""Reference-led pass11+: upright biomechanical feet, vascular density and shell detail.
All veins are real geometry belonging exclusively to FIXED or YAW.
"""
import bpy,math
from mathutils import Vector

def apply(c):
 objects=c['objects'];mesh=c['mesh'];organ=c['organ'];vein=c['vein'];ring=c['ring'];S=c['S'];D=c['D']
 # Replace the rejected flat radial petal study, not the supplied source artwork.
 for o in list(objects):
  if o['fixed']:objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
 outline=[(-2.90,-2.52),(-2.42,-2.88),(-1.66,-2.76),(-.82,-2.94),(.12,-2.86),(1.38,-2.96),(2.62,-2.70),(2.92,-2.17),(2.78,-1.25),(2.91,.03),(2.76,1.33),(2.96,2.40),(2.35,2.88),(1.18,2.73),(-.16,2.94),(-1.61,2.78),(-2.80,2.53),(-2.94,1.43),(-2.78,.27),(-2.94,-1.20)]
 n=len(outline);vs=[(x,y,z) for z in (.035,.105) for x,y in outline];mesh('Fixed irregular source-like ground raft',vs,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)],'earth',True,bias=-1)
 organ('Fixed armored thorax',[(0,0,.16),(0,0,.58),(0,0,1.35),(0,0,1.76)],[(1.55,1.55),(1.62,1.62),(1.40,1.40),(1.06,1.06)],fixed_part=True,bias=-1,n=32)
 for z in (.24,.46,.69,.92,1.14):ring('Fixed segmented thorax rim',z,1.64-.11*z,.055,fixed_part=True,bias=1)
 ring('Fixed lower yaw race',1.79,1.08,.11,.68,True,bias=-1)
 ring('Fixed recessed dark yaw race',1.91,.84,.065,.63,True,bias=-3)
 for i in range(4):
  t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  def p(r,z,w=0):return u*r+v*w+Vector((0,0,z))
  organ(f'Fixed standing armor limb {i}',[p(1.12,1.60),p(1.79,1.51),p(2.12,.94),p(2.46,.31),p(2.72,.14)],[(.30,.26),(.47,.39),(.36,.30),(.22,.18),(.075,.05)],fixed_part=True,bias=0,n=16,steps=5)
  for j,(r,z) in enumerate([(1.75,1.48),(2.10,.99),(2.41,.48)]):
   organ(f'Fixed convex leg plate {i}/{j}',[p(r-.12,z+.17,-.26),p(r+.19,z+.08),p(r+.24,z-.17,.26)],[(.14,.10),(.34,.24),(.085,.06)],fixed_part=True,bias=1,n=12,steps=5)
   # Narrow raised seam follows the individual plate, not a black outline on a flat fill.
   organ(f'Fixed leg plate seam {i}/{j}',[p(r+.08,z+.23,-.22),p(r+.31,z+.10),p(r+.24,z-.19,.22)],[.025,.035,.025],fixed_part=True,bias=3,n=6)
  for j in range(6):
   z=.39+j*.185
   organ(f'Fixed exposed inner rib {i}/{j}',[p(1.43,z,-.38),p(1.56,z+.03),p(1.43,z,.38)],[.035,.055,.035],fixed_part=True,bias=1,n=8)
  for side in (-1,1):
   vein(f'Fixed shoulder vessel {i}/{side}',[p(.72,1.83,side*.23),p(1.34,1.71,side*.46),p(1.90,1.23,side*.50),p(2.28,.58,side*.36),p(2.65,.15,side*.31)],.085,True,bias=1)
   vein(f'Fixed ground curling root {i}/{side}',[p(1.58,.75,side*.36),p(2.09,.27,side*.53),p(2.61,.11,side*.65),p(2.96,.10,side*.41),p(2.85,.10,side*.16)],.08,True,bias=1)
   vein(f'Fixed forked rootlet {i}/{side}',[p(2.14,.18,side*.49),p(2.55,.10,side*.89),p(2.90,.08,side*1.10)],.048,True)
  # Discrete chitin islands/scars on the ground raft, not a uniform square pedestal.
  for side in (-1,1):
   organ(f'Fixed ground armor rib {i}/{side}',[p(1.64,.13,side*.68),p(2.16,.16,side*.80),p(2.64,.11,side*.88)],[.06,.11,.025],fixed_part=True,bias=1,n=7)
 # Wider recessed muscle plates cover the neck's formerly blank rear and end faces.
 for side in (-1,1):
  organ(f'Yaw ribbed shoulder muscle {side}',[(side*.58,-.09,2.23),(side*.87,-.31,2.63),(side*.83,-.83,3.20),(side*.73,-1.32,3.84)],[(.15,.18),(.25,.25),(.23,.26),(.11,.14)],bias=0,n=14)
  for j in range(7):
   z=2.32+j*.22;y=-.14-(z-2.32)*.65
   organ(f'Yaw shoulder rib {side}/{j}',[(side*.68,y+.19,z),(side*.97,y,z+.03),(side*.78,y-.21,z+.12)],[.035,.055,.025],bias=2,n=8)
  for j in range(4):
   x=side*(.78+j*.11)
   vein(f'Yaw exposed cervical vascular loop {side}/{j}',[(side*.62,-1.84,4.27),(x,-1.69-j*.10,3.76),(x*1.16,-1.04,3.04),(x*.91,-.51,2.53),(side*.52,-.17+j*.08,2.24)],.075+(j%2)*.012,bias=1)
  for j in range(3):
   x=-.25+side*(.52+j*.11)
   vein(f'Yaw accelerator underside return {side}/{j}',[(x,1.72+j*.42,4.19),(x+side*.27,1.16+j*.17,3.79-j*.10),(x+side*.23,.20,3.54-j*.07),(x,-1.35,4.12)],.08,bias=1)
  organ(f'Yaw deep cervical energy slit {side}',[(side*.70,-.58,2.65),(side*.80,-.91,3.10),(side*.63,-1.18,3.53)],[.050,.073,.027],'cyan',bias=-1,n=6)
 # Heavy front/bottom magazine organ and interwoven socket veins, attached to yaw only.
 for j in range(4):
  y=-.53+j*.64
  organ(f'Yaw magazine scalloped breastplate {j}',[(1.99,y-.12,2.38),(2.15,y-.09,2.72),(2.12,y+.09,3.09)],[(.13,.11),(.23,.15),(.06,.04)],bias=1,n=12)
  vein(f'Yaw magazine visible cross-vein {j}',[(1.91,y+.13,3.26),(2.21,y+.10,2.94),(2.20,y-.18,2.65),(1.82,y-.32,2.36),(1.19,y*.51,2.27)],.085,bias=1)
 # Curved layered armor around the shoulder, returning to the gun cocoon.
 for side in (-1,1):
  for j in range(4):
   x=-.25+side*(.47+j*.12)
   organ(f'Yaw receiver curved overlapping armor {side}/{j}',[(x,-2.16,4.12),(x+side*.12,-2.02,4.55),(x,-1.60,4.99),(x*.8,-.94,5.04)],[.035,.09,.10,.025],bias=1,n=10)
 if S>=12:
  # Receiver armor must sit on the ACTUAL cocoon envelope (earlier ribs were buried).
  for j,y in enumerate((-2.35,-1.96,-1.55,-1.14,-.78)):
   r=.63 if j in (0,4) else .86;z=4.83 if j==0 else 4.93
   pts=[(-.25+r*math.cos(t),y,z+r*.90*math.sin(t)) for t in [math.radians(v) for v in (5,35,65,95,125,155,175)]]
   organ(f'Yaw external bowed receiver armor {j}',pts,[.06,.11,.10,.055],bias=2,n=10,steps=3)
   for side in (-1,1):
    vein(f'Yaw receiver plate intercostal artery {j}/{side}',[(-.25+side*r*.95,y-.05,z-.20),(-.25+side*r*1.06,y-.06,z+.15),(-.25+side*r*.66,y-.06,z+.55)],.07,bias=1)
  # Actual recessed rear organ; its vascular feed returns to the moving neck.
  organ('Yaw rear sensor armored organ',[(-.63,-1.55,2.72),(-.72,-2.08,3.05),(-.72,-2.35,3.37)],[(.18,.20),(.28,.31),(.23,.25)],bias=1,n=16)
  organ('Yaw rear sensor socket',[(-.72,-2.49,3.39),(-.72,-2.55,3.40)],[.16,.15],bias=-3,n=20,steps=1)
  organ('Yaw rear cyan sensory seed',[(-.72,-2.56,3.40),(-.72,-2.59,3.42)],[(.105,.07),(.08,.045)],'cyan',bias=1,n=12,steps=1)
  for side in (-1,1):
   vein(f'Yaw rear organ vessel {side}',[(-.72+side*.22,-2.29,3.32),(-.72+side*.36,-2.23,2.88),(-.44+side*.30,-1.39,2.45),(-.27+side*.25,-.49,2.23)],.075,bias=1)
  for i in range(8):
   t=(i+.5)*math.tau/8;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
   organ(f'Fixed thorax upright plate {i}',[u*1.54+Vector((0,0,.35)),u*1.72+Vector((0,0,.97)),u*1.35+Vector((0,0,1.58))],[(.20,.075),(.25,.13),(.13,.08)],fixed_part=True,bias=0,n=12)
   for j in range(5):
    z=.45+j*.20;r=1.76-abs(.95-z)*.25
    organ(f'Fixed narrow recessed vent lip {i}/{j}',[u*r-v*.16+Vector((0,0,z)),u*(r+.035)+Vector((0,0,z+.025)),u*r+v*.16+Vector((0,0,z))],[.022,.028,.022],fixed_part=True,bias=2,n=6)
   vein(f'Fixed thorax vertical vascular strand {i}',[u*.78+v*.20+Vector((0,0,1.82)),u*1.39+v*.30+Vector((0,0,1.55)),u*1.78+v*.31+Vector((0,0,.83)),u*1.96+v*.31+Vector((0,0,.16))],.06,True,bias=1)
   organ(f'Fixed engraved ground panel edge {i}',[u*2.18-v*.47+Vector((0,0,.113)),u*2.45-v*.19+Vector((0,0,.115)),u*2.63+v*.25+Vector((0,0,.113))],[.015,.017,.012],fixed_part=True,bias=-3,n=5)
 if S>=13:
  # The reference crown is forward of the neck, not raised into the accelerator.
  # Move the organ forward in ground-plane depth without lowering it into the base.
  for o in objects:
   if o.name.startswith(('Magazine','Rotating lower crystal','Yaw magazine')):
    for vertex in o.data.vertices:
     v=vertex.co;w=1 if o.name.startswith(('Magazine shard','Magazine embracing','Magazine dark','Magazine raised','Yaw magazine scalloped')) else max(0,min(1,(v.x-.65)/.90))
     v.x+=1.30*w;v.y-=.90*w
  for j in range(4):
   y=-.53+j*.64-.90;z=2.47+j*.08+.55;height=(.94+(.12 if j%2 else 0))*1.22;x=1.67+1.30
   # Physical thin crystal facet ridges, not a flat cyan disk or procedural noise.
   for k in range(3):
    xx=x+.14+k*.055
    organ(f'Yaw crown crystal refractive edge {j}/{k}',[(xx,y-.12,z+.14),(xx-.045,y-.08,z+height*.46),(x-.06,y+.12,z+height*.91)],[.012,.020,.004],'cyan',bias=2+k%2,n=5,steps=2)
   organ(f'Yaw crown dark crystal flute {j}',[(x+.25,y+.03,z+.12),(x+.18,y+.06,z+height*.55),(x+.01,y+.13,z+height*.87)],[.012,.018,.003],'cyan',bias=-2,n=5,steps=2)
 # Source-defined surface cuts: bounded armor motifs on corresponding convex plates.
 for o in objects:
  if not o.name.startswith('Fixed convex leg plate'):continue
  i,j=map(int,o.name.rsplit(' ',1)[1].split('/'));t=(i+.5)*math.tau/4;radial=Vector((math.cos(t),math.sin(t),0));tangent=Vector((-radial.y,radial.x,0));crop=[(119,164,139,187),(120,188,138,210),(115,209,135,231)][j];x0,y0,x1,y1=crop
  us=[q.co.dot(tangent) for q in o.data.vertices];zs=[q.co.z for q in o.data.vertices];uc=(max(us)+min(us))/2;zc=(max(zs)+min(zs))/2;uv=o.data.uv_layers.new(name='bounded original armor at native density');textured=[]
  for f in o.data.polygons:
   points=[((x0+x1)/2+(o.data.vertices[k].co.dot(tangent)-uc)*D,(y0+y1)/2-(o.data.vertices[k].co.z-zc)*D) for k in f.vertices]
   if f.normal.dot(radial)<.20 or not all(x0<=x<x1 and y0<=y<y1 for x,y in points):continue
   for li,(x,y) in zip(f.loop_indices,points):uv.data[li].uv=(x/256,1-y/256)
   textured.append(f.index)
  o['texture_faces']=textured;o['source_cut']=list(crop)
 # Explicit separated vertical envelopes permit a complete360-degree rotation.
 for o in objects:
  for v in o.data.vertices:
   if o['fixed']:v.co.z=min(v.co.z,2.00)
   else:v.co.z=max(v.co.z,2.14)
