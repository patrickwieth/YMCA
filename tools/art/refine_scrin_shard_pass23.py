"""Reference-led proportions/armor revision23+. Keep25-degree gun and fixed four-shard bank.
Prior passes and runtime assets are never written by this module.
"""
import bpy,math
from mathutils import Vector

def apply(c):
 S=c['S'];objects=c['objects'];mesh=c['mesh'];organ=c['organ'];vein=c['vein'];ring=c['ring']
 def remove(prefixes):
  for o in list(objects):
   if o.name.startswith(prefixes):objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
 # Reference has substantial upright legs, not a low plinth below a long bare neck.
 old=2.14;new=1.18 if S>=27 else 2.84;c['PIVOT']=new;c['pivot'].location.z=new
 def height(z):return new+(z-old)*(4.62-new)/(4.62-old) if z<4.62 else z
 for o in objects:
  if not o['fixed']:o.location=-c['pivot'].location
  for v in o.data.vertices:
   if not o['fixed']:
    rigid=S>=26 and any(s in o.name.lower() for s in ('accelerator','receiver','cheek','long faceted','crystal branching','terminal split'))
    if not rigid:v.co.z=height(v.co.z)
   elif o.get('fixed_battery',False):v.co.z-=.38*max(0,min(1,(v.co.z-.15)/2.0))
   else:v.co.z*=1.35
  o.data.update()
 # Remove flat thin shin stand-ins, preserve real recessed knee hardware and louvers.
 remove(('Fixed source armored shin','Fixed inset long shin plate','Fixed dark knee recess'))
 for i in range(4):
  t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
  def p(r,z,w=0):return u*r+v*w+Vector((0,0,z))
  rows=[(2.65,.16,.40,.15),(2.55,.48,.50,.24),(2.23,1.20,.43,.32),(1.94,2.13,.60,.34),(1.49,2.52,.46,.24)]
  vs=[];n=16
  for r,z,w,d in rows:
   if S>=26:w*=.72
   for k in range(n):
    a=k*math.tau/n;vs.append(tuple(p(r+d*math.cos(a),z,w*math.sin(a))))
  fs=[(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(len(rows)-1) for k in range(n) if not (S>=24 and j in (1,2) and k in (0,15))]
  fs.extend([tuple(range(n-1,-1,-1)),tuple((len(rows)-1)*n+k for k in range(n))])
  mesh(f'Fixed convex standing shin {i}',vs,fs,fixed_part=True,bias=1)
  if S>=24:
   organ(f'Fixed recessed shin channel {i}',[p(2.62,.55),p(2.38,1.20),p(2.03,2.02)],[(.15,.08),(.15,.10),(.18,.08)],fixed_part=True,shade=1,n=12)
   for j in range(9):
    z=.66+j*.145;r=2.66-(z-.66)*.44
    organ(f'Fixed shin channel transverse vane {i}/{j}',[p(r,z,-.14),p(r+.045,z+.025),p(r,z,.14)],[.018,.031,.018],fixed_part=True,bias=2,n=6)
   organ(f'Fixed protected shin energy conduit {i}',[p(2.64,.68,.05),p(2.43,1.18,.05),p(2.23,1.70,.05)],[.018,.027,.018],'cyan',True,bias=-2,n=6)
  # Sculpted stepped knee/shin shields, not source images stretched over flat quads.
  for j,(r,z,w) in enumerate([(2.72,.31,.27)] if S>=24 else [(2.36,1.34,.29),(2.58,.75,.32),(2.72,.31,.27)]):
   organ(f'Fixed source shin inset {i}/{j}',[p(r,z+.23,-w),p(r+.15,z+.08),p(r+.05,z-.18,w)],[(.075,.075),(.18,.12),(.045,.045)],fixed_part=True,bias=2,n=12)
  for side in (-1,1):
   vein(f'Fixed shoulder-to-toe vascular seam {i}/{side}',[p(1.45,2.54,side*.35),p(1.94,2.04,side*.64),p(2.30,1.1,side*.49),p(2.65,.35,side*.48),p(2.94,.11,side*.55)],.082,True,bias=0)
   organ(f'Fixed grounded toe scute {i}/{side}',[p(2.56,.24,side*.28),p(2.81,.13,side*.35),p(3.01,.08,side*.39)],[.105,.14,.025],fixed_part=True,bias=1,n=10)
 # Add a ribbed closed cervical rear, bringing the source's layered organic mass back.
 for j in range(7):
  z=(new+.12+j*(4.07-new-.12)/6) if S>=27 else 2.96+j*.185;y=-.24-j*.185*.78
  organ(f'Compact cervical dorsal shield {j}',[(-.65,y-.48,z),(-.20,y-.66,z+.09),(.55,y-.47,z+.07)],[.045,.13,.04],bias=1,n=10)
 for side in (-1,1):
  organ(f'Compact cervical luminous return {side}',[(side*.72,-.18,2.99),(side*.81,-.48,3.46),(side*.72,-.87,3.94)],[.028,.045,.015],'cyan',bias=1,n=8)
 c['coating_contrast']=.10
 c['coating_targets']=('Main receiver','Accelerator overlapping','Deep accelerator','Yaw external bowed','Yaw shoulder','Yaw magazine scalloped','Rotating organic neck','Rotating curved side','Fixed convex standing','Fixed source shin','Compact cervical','Receiver sculpted')
 if S>=24:
  # A compact refracting spear, not an oversized flat cyan wedge filling the trough.
  root=c['cannon_root'];axis=(c['tip']-root).normalized()
  for o in objects:
   if o.name.startswith(('Long faceted cyan shard','Crystal branching filament')):
    for vertex in o.data.vertices:
     center=root+axis*(vertex.co-root).dot(axis);vertex.co=center+(vertex.co-center)*.72
    o.data.update()
  # Replace narrow roller-like receiver straps with actual overlapping convex shell plates.
  remove(('Yaw external bowed receiver armor','Yaw receiver curved overlapping armor','Rear crown layered plate'))
  for j in range(5):
   y=-2.35+j*.34;r=.70 if j==0 else .91 if j in (1,2,3) else .77;n=20;vs=[]
   for dy,rr in [(-.20,r*.93),(-.13,r),( .17,r*.98),(.22,r*.89)]:
    for k in range(n+1):
     a=math.radians(8+164*k/n);vs.append((-.25+rr*math.cos(a),y+dy,4.88+rr*.83*math.sin(a)))
   fs=[(row*(n+1)+k,row*(n+1)+k+1,(row+1)*(n+1)+k+1,(row+1)*(n+1)+k) for row in range(3) for k in range(n)]
   mesh(f'Receiver sculpted overlapping plate {j}',vs,[tuple(reversed(f)) for f in fs] if S>=25 else fs,bias=1)
   for side in (-1,1):
    organ(f'Receiver protected interplate vent {j}/{side}',[(-.25+side*r*.93,y-.21,4.98),(-.25+side*r*.78,y-.21,5.26)],[.022,.028],bias=-3,n=6)
  # Refractive internal spine and bounded facet ridges on the modeled shard, not flat VFX.
  root=c['cannon_root'];tip=c['tip'];axis=tip-root
  for j in range(6):
   a=root+axis*(.18+j*.105);b=root+axis*(.29+j*.105);a.z+=.15+j*.025;b.z+=.17+j*.025
   organ(f'Crystal branching internal refractor {j}',[a+Vector((-.07,0,0)),(a+b)/2+Vector((.10,0,.06)),b],[.018,.032,.008],'cyan',bias=3,n=5)
  # Buttress seams and interleg stacks have intentional shaded faces, not uniform fills.
  for i in range(4):
   t=i*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
   for side in (-1,1):
    organ(f'Fixed interleg structural arch {i}/{side}',[u*1.15+v*side*.55+Vector((0,0,2.55)),u*1.59+v*side*.56+Vector((0,0,1.65)),u*1.71+v*side*.48+Vector((0,0,.31))],[.08,.13,.06],fixed_part=True,bias=1,n=12)
 if S>=25:
  # Upper organic housing now has protected recessed cheek mechanisms instead of discs.
  for side in (-1,1):
   x=-.25+side*1.10
   for j in range(7):
    angle=(j+.5)*math.tau/7;y=-1.86+.33*math.cos(angle);z=4.62+.33*math.sin(angle)
    organ(f'Receiver radial recessed vane {side}/{j}',[(x,y,z),(x+side*.025,y-.09*math.sin(angle),z+.09*math.cos(angle))],[.035,.018],bias=1,n=6)
   vein(f'Receiver closed outer vascular seam {side}',[(x,-2.27,4.22),(x+side*.10,-2.50,4.58),(x,-2.26,4.98),(x*.90,-1.85,5.16)],.065,bias=0)
  # Calf shield closes the lower channel while keeping the upper mechanism genuinely recessed.
  for i in range(4):
   t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
   organ(f'Fixed faceted lower calf shield {i}',[u*2.78+Vector((0,0,.35)),u*2.64+Vector((0,0,.85)),u*2.40+Vector((0,0,1.40))],[(.12,.08),(.23,.13),(.14,.07)],fixed_part=True,bias=2,n=12)
  # Dark vascular tissue with restrained edge highlights, not pastel plastic hoses.
  for shade,rgb in [(6,(126,69,94)),(7,(147,88,113)),(8,(158,94,120))]:
   rgb=c['nearest'](rgb);c['palette']['vein'+str(shade)]=rgb;c['mats']['vein'+str(shade)].node_tree.nodes.get('Emission').inputs[0].default_value=(*map(c['linear'],rgb),1)
  # Ground roots use smooth tapered returns and forked tips at native pixel density.
  for i in range(4):
   t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0))
   for side in (-1,1):
    vein(f'Fixed tapered root fork {i}/{side}',[u*2.4+v*side*.5+Vector((0,0,.25)),u*2.8+v*side*.7+Vector((0,0,.1)),u*3.05+v*side*.95+Vector((0,0,.07)),u*3.22+v*side*1.02+Vector((0,0,.065))],.045,True,bias=0)
 if S>=26:
  remove(('Yaw receiver plate intercostal artery',))
  for o in objects:
   if o.name.startswith('Terminal split claw'):
    for v in o.data.vertices:v.co.x=-.25+(v.co.x+.25)*1.16;v.co.y=-.5+(v.co.y+.5)*.87
   if o.name.startswith('Yaw rear'):
    for v in o.data.vertices:v.co.x-=.25;v.co.y-=.20
   if o.name.startswith(('Fixed visible interleg louver','Fixed exposed mechanism slat')):o['bias']=-1
   o.data.update()
  for side in (-1,1):
   organ(f'Exposed cyan cervical cleft {side}',[(side*1.06,-.30,3.20),(side*1.10,-.60,3.62),(side*.99,-1.02,4.14)],[.025,.055,.020],'cyan',bias=2,n=8)
 if S>=28:
  for o in objects:
   if o.name.startswith(('Accelerator overlapping dorsal armor','Deep accelerator trough')):o['inner_lit_faces']=[f.index for f in o.data.polygons if f.index%3==2]
  lip=organ('Fixed battery continuous chitin lip',[(4.03,-1.45,3.06),(4.12,-.75,3.16),(4.12,-.10,3.25),(4.04,.72,3.34)],[.045,.07,.065,.04],fixed_part=True,bias=2,n=10);lip['fixed_battery']=True
 if S>=29:
  # Rounded cut rims and inward-curled teeth replace the bare black channel edges.
  for o in list(objects):
   if not o.name.startswith('Accelerator overlapping dorsal armor'):continue
   o['rim_lit_faces']=[len(o.data.polygons)-2,len(o.data.polygons)-1];q=19
   for side,k in [(-1,0),(1,18)]:
    a=(o.data.vertices[k].co+o.data.vertices[2*q+k].co)/2;b=(o.data.vertices[q+k].co+o.data.vertices[3*q+k].co)/2
    organ(f'Accelerator rounded open rim {o.name.rsplit(" ",1)[1]}/{side}',[a,(a+b)/2,b],[.035,.065,.035],bias=2,n=10)
    delta=Vector((.30,0,-.32)) if side<0 else Vector((-.33,0,.23))
    organ(f'Accelerator hooked inner tooth {o.name.rsplit(" ",1)[1]}/{side}',[b,b+delta*.45+Vector((0,.07,0)),b+delta],[.06,.085,.012],bias=1,n=10)
 if S>=27:
  # The source's collar is recessed BETWEEN the tall knees, not perched above them.
  for o in objects:
   if o.name=='Fixed ribbed mechanical plinth':
    for v in o.data.vertices:v.co.z*=.98/2.43
   elif o.name=='Fixed recessed yaw race':
    for v in o.data.vertices:v.co.z*=1.04/(1.945*1.35)
   o.data.update()
  c['coating_targets']=()  # Let physical bevels/normal tones read; no repeating mottled finish.
  # Source-like dorsal access organ: dark inset, chamfered rim and protected cyan slit.
  rim=[(-.38,-.23),(.38,-.23),(.47,-.14),(.47,.14),(.38,.23),(-.38,.23),(-.47,.14),(-.47,-.14)];vs=[]
  for z,factor in [(5.42,1),(5.51,1),(5.51,.72),(5.45,.72)]:
   vs.extend([(-.25+x*factor,-2.36+y*factor,z) for x,y in rim])
  fs=[(row*8+k,row*8+(k+1)%8,(row+1)*8+(k+1)%8,(row+1)*8+k) for row in range(3) for k in range(8)]
  mesh('Receiver dorsal inset frame',vs,fs,bias=2)
  mesh('Receiver dorsal recessed access',vs[24:],[tuple(range(8))],shade=1)
  for j in range(4):
   y=-2.49+j*.085;organ(f'Receiver dorsal access grille {j}',[(-.50,y,5.465),(0,y,5.465)],[.022,.022],bias=2,n=6,steps=1)
  organ('Receiver dorsal cyan status slit',[(-.50,-2.57,5.48),(-.03,-2.57,5.48)],[.018,.018],'cyan',bias=1,n=6,steps=1)
 if S>=25:
  # Compact the upper mass without changing the bore angle or crushing curved housings.
  def compact(v,rigid):
   v=v.copy();drop=1.05 if S>=27 else .65;bottom=new if S>=27 else 2.84
   v.z-=drop if rigid else drop*max(0,min(1,(v.z-bottom)/(4.62-bottom)));return v
  for o in objects:
   if o['fixed']:
    if o.name=='Fixed broken soil contact':
     for v in o.data.vertices:v.co.x*=.90;v.co.y*=.90
    continue
   rigid=any(s in o.name.lower() for s in ('accelerator','receiver','cheek','long faceted','crystal branching','terminal jaw')) or (S>=26 and o.name.startswith('Terminal split claw'))
   for v in o.data.vertices:
    v.co=compact(v.co,rigid)
    if (any(s in o.name.lower() for s in ('accelerator','long faceted','crystal branching','terminal jaw')) or (S>=26 and o.name.startswith('Terminal split claw'))) and v.co.y>-.63:v.co.y=-.63+(v.co.y+.63)*.92
   o.data.update()
  c['tip']=compact(c['tip'],True);c['tip'].y=-.63+(c['tip'].y+.63)*.92;c['cannon_root']=compact(c['cannon_root'],True)
  c['elevation_origin']=Vector((-.25,-1.86,3.57 if S>=27 else 3.97));c['elevation_neck_high']=3.57 if S>=27 else 3.97
 if S>=24:
  for o in objects:
   if o['fixed'] and not o.get('fixed_battery',False):
    for vertex in o.data.vertices:vertex.co.x*=1.20;vertex.co.y*=1.20
    o.data.update()
 if S>=26:
  # Native-density bounded cuts on corresponding shin armor only; no foreign imagery.
  for o in objects:
   if not o.name.startswith('Fixed convex standing shin'):continue
   i=int(o.name.rsplit(' ',1)[1]);t=(i+.5)*math.tau/4;u=Vector((math.cos(t),math.sin(t),0));v=Vector((-u.y,u.x,0));uv=o.data.uv_layers.new(name='Bounded source shin armor');faces=[]
   for f in o.data.polygons:
    points=[(131+o.data.vertices[k].co.dot(v)*28,239-o.data.vertices[k].co.z*28) for k in f.vertices]
    if f.normal.dot(u)<.25 or not all(117<=x<=145 and 169<=y<=234 for x,y in points):continue
    for li,(x,y) in zip(f.loop_indices,points):uv.data[li].uv=(x/256,1-y/256)
    faces.append(f.index)
   o['texture_faces']=faces;o['source_cut']=[117,169,145,234]
  # Only crystal-colored source texels may enter the main crystalline spear's UVs.
  o=next(o for o in objects if o.name=='Long faceted cyan shard');uv=o.data.uv_layers.new(name='Bounded original crystal facets');src=list(c['source_image'].pixels[:]);root=c['cannon_root'];axis=c['tip']-root;length=axis.length;direction=axis.normalized();faces=[]
  def source_uv(v):
   t=(v-root).dot(direction)/length;center=root+axis*t;d=(v.x-center.x)*28;return (108+102*t+.555*d,98-68*t+.832*d)
  def cyan_at(x,y):
   if not (90<=x<219 and 20<=y<111):return False
   k=((255-int(y))*256+int(x))*4;r,g,b,a=src[k:k+4];return a>.95 and g-r>.10 and b>.35
  for f in o.data.polygons:
   pts=[source_uv(o.data.vertices[k].co) for k in f.vertices];samples=pts+[(sum(x for x,y in pts)/len(pts),sum(y for x,y in pts)/len(pts))]
   for a,b in zip(pts,pts[1:]+pts[:1]):samples.extend([(a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t) for t in (.25,.5,.75)])
   if not all(cyan_at(x,y) for x,y in samples):continue
   for li,(x,y) in zip(f.loop_indices,pts):uv.data[li].uv=(x/256,1-y/256)
   faces.append(f.index)
  o['texture_faces']=faces;o['source_cut']=[90,20,219,111];o['source_cyan_faces']=len(faces)
 if S>=28:
  src=list(c['source_image'].pixels[:])
  for o in objects:
   if not o.name.startswith('Magazine shard '):continue
   index=int(o.name.rsplit(' ',1)[1]);cut=[(144,122,163,170),(160,114,180,156),(178,106,195,147),(192,100,211,139)][index];x0,y0,x1,y1=cut
   xc=(min(v.co.x for v in o.data.vertices)+max(v.co.x for v in o.data.vertices))/2;top=max(v.co.z for v in o.data.vertices);uv=o.data.uv_layers.new(name='Bounded source shard refractions');faces=[]
   def valid(x,y):
    if not(x0<=x<x1 and y0<=y<y1):return False
    k=((255-int(y))*256+int(x))*4;r,g,b,a=src[k:k+4];return a>.95 and g-r>.10 and b>.35
   for f in o.data.polygons:
    pts=[((x0+x1)/2+(o.data.vertices[k].co.x-xc)*28,y0+(top-o.data.vertices[k].co.z)*28) for k in f.vertices]
    samples=pts+[(sum(x for x,y in pts)/len(pts),sum(y for x,y in pts)/len(pts))]
    for a,b in zip(pts,pts[1:]+pts[:1]):samples.extend([(a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t) for t in (.25,.5,.75)])
    if not all(valid(x,y) for x,y in samples):continue
    for li,(x,y) in zip(f.loop_indices,pts):uv.data[li].uv=(x/256,1-y/256)
    faces.append(f.index)
   o['texture_faces']=faces;o['source_cut']=cut
 c['notes'].append('23+: taller armored legs / compact neck;24+: layered receiver;25+: recessed mechanisms and tapered roots. Source reference facing30. No runtime integration.')


def finalize(c):
 # A recessed collar needs radial clearance, not a whole-assembly Z gap.
 for o in c['objects']:
  for vertex in o.data.vertices:
   v=vertex.co;r=math.hypot(v.x,v.y)
   minimum=3.20 if c['S']>=29 and o.get('fixed_battery',False) and v.z>2.60 else 1.80
   if o['fixed'] and v.z>1.10 and r<minimum:
    if r>.00001:v.x*=minimum/r;v.y*=minimum/r
   elif not o['fixed'] and v.z<2.95:
    limit=1.40+max(0,v.z-2.68)*5
    if r>limit:v.x*=limit/r;v.y*=limit/r
  o.data.update()
