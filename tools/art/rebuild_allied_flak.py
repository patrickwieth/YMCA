"""Mechanical flak reconstruction: bevels, panel recesses, stepped gun jackets.
Uses bounded pennant artwork; base/mechanism details are independent solid meshes.
"""
import math
import bpy
from mathutils import Vector

def rebuild(c):
    for o in c['objects']:bpy.data.objects.remove(o,do_unlink=True)
    c['objects'].clear();mesh,box,tube=c['mesh'],c['box'],c['tube']
    families={}
    for family,colors in {'blue':[(18,34,57),(24,46,76),(30,61,101),(39,79,126),(66,101,139),(93,129,157)],'copper':[(48,34,27),(69,48,34),(94,67,44),(121,87,56),(146,112,77),(173,139,99)]}.items():
        families[family]=len(c['mats'])
        for color in colors:
            m=bpy.data.materials.new('Source '+family);m.use_nodes=True;n=m.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');em.inputs[0].default_value=(*map(c['lin'],c['nearest'](color)),1);m.node_tree.links.new(em.outputs[0],out.inputs[0]);c['mats'].append(m)
    def family(o,name):o['family_base']=families[name];return o
    def beveled(name,center,size,bias=0):
        x,y,z=center;a,b,h=[v/2 for v in size];cut=min(a,h)*.35
        profile=[(-a,-h+cut),(-a+cut,-h),(a-cut,-h),(a,-h+cut),(a,h-cut),(a-cut,h),(-a+cut,h),(-a,h-cut)]
        vs=[(x+xx,y+yy,z+zz) for yy in (-b,b) for xx,zz in profile]
        fs=[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,8+(i+1)%8,8+i) for i in range(8)]
        return mesh(name,vs,[tuple(reversed(f)) for f in fs],bias=bias)
    def lathe(name,levels,n=32,fixed=False,bias=0):
        vs=[(r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),z) for r,z in levels for i in range(n)]
        fs=[tuple(range(n-1,-1,-1)),tuple((len(levels)-1)*n+i for i in range(n))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(levels)-1) for i in range(n)]
        return mesh(name,vs,fs,fixed,bias)
    outline=[(-1.70,-2.02),(1.70,-2.02),(2.02,-1.70),(2.02,1.70),(1.70,2.02),(-1.70,2.02),(-2.02,1.70),(-2.02,-1.70)]
    vs=[(x*scale,y*scale,z) for scale,z in ((.98,.05),(1,.13),(1,.29),(.96,.36)) for x,y in outline]
    fs=[tuple(range(7,-1,-1)),tuple(24+i for i in range(8))]+[(j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i) for j in range(3) for i in range(8)]
    mesh('Fixed beveled octagonal plinth',vs,fs,True)
    lathe('Fixed panel pedestal',[(1.77,.33),(1.85,.44),(1.85,1.33),(1.96,1.42)],8,True)
    family(lathe('Fixed blue identification band',[(1.96,1.38),(1.99,1.46),(1.99,1.54)],8,True),'blue')
    lathe('Fixed beveled upper flange',[(1.99,1.54),(1.94,1.63),(1.77,1.66)],8,True,-1)
    for k in range(8):
        theta=(k+.5)*math.pi/4;normal=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-normal.y,normal.x,0))
        def plate(name,xc,z,w,h,depth,bias=0):
            center=normal*depth+tangent*xc+Vector((0,0,z));vs=[tuple(center+tangent*x+Vector((0,0,zz))) for x,zz in ((-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2))]
            return mesh(name+' '+str(k),vs,[(0,1,2,3)],True,bias)
        plate('Fixed deep vent recess',0,.88,.36,.85,1.72,-3)
        for z in (.53,.65,.77,.89,1.01,1.13,1.25):plate('Fixed individual vent louver',0,z,.29,.043,1.74,1)
        for x in (-.27,.27):
            plate('Fixed raised armor rib',x,.87,.11,.92,1.75,1)
            plate('Fixed rib foot',x,.49,.17,.24,1.78,0)
            a=normal*1.76+tangent*x+Vector((0,0,1.22));tube('Fixed armor screw',a,a+normal*.04,.027,n=8,stationary=True,bias=-1)
    lathe('Fixed turret bearing',[(1.26,1.66),(1.26,1.76)],32,True,-2)
    lathe('Rotating turret race',[(1.22,1.76),(1.22,1.86),(1.12,1.92)],32)
    # Dogleg yokes, thicker at the foot and curved around the receiver axle.
    profile=[(-.65,1.88),(.65,1.88),(.79,2.13),(.64,2.53),(.33,3.12),(.04,3.30),(-.25,3.24),(-.49,3.02),(-.58,2.42)]
    for side in (-1,1):
        vs=[(side*.75+d,y,z) for d in (-.15,.15) for y,z in profile];n=len(profile)
        fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
        mesh('Contoured bearing yoke '+str(side),vs,fs,bias=-1)
        inner=[(y*.73,2.57+(z-2.57)*.78) for y,z in profile]
        rim=[(side*.925,y,z) for y,z in profile+inner]
        faces=[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
        if side<0:faces=[tuple(reversed(f)) for f in faces]
        mesh('Raised curved yoke edging '+str(side),rim,faces,bias=1)
        for yy in (-.33,.34):tube('Yoke heel fastener',(side*.94,yy,2.10),(side*.98,yy,2.10),.035,n=8,bias=-1)
        tube('Axle rim '+str(side),(side*.89,-.03,2.96),(side*1.02,-.03,2.96),.37,inner=.25)
        tube('Axle recessed cap '+str(side),(side*.90,-.03,2.96),(side*.98,-.03,2.96),.235,n=16,bias=-1)
        # Bent copper service line follows the yoke and feed unit, not a photo stripe.
        points=[(side*1.05,.09,2.05),(side*1.05,.10,2.58),(side*1.15,.52,2.70),(side*1.15,.73,3.18),(side*.95,.70,3.65)]
        for a,b in zip(points,points[1:]):family(tube('Copper service conduit',a,b,.047,n=10),'copper')
    box('Split receiver lower core',(0,.10,3.10),(1.20,1.30,.76),bias=-2)
    for side in (-1,1):
        beveled('Independent breech housing '+str(side),(side*.35,.16,3.40),(.62,1.34,.53))
        beveled('Upper stepped breech armor '+str(side),(side*.35,.30,3.73),(.66,1.18,.22),bias=0)
        for yy in (-.10,.21,.52):box('Breech roof cooling inset',(side*.35,yy,3.848),(.34,.07,.025),bias=-3)
        beveled('Rear ammunition feeder '+str(side),(side*.94,.64,3.20),(.48,.70,.57),bias=-1)
        for yy in (.39,.55,.71,.87):box('Individual feed fin',(side*1.20,yy,3.22),(.07,.075,.45),bias=1)
        tube('Return piston '+str(side),(side*.43,.48,1.96),(side*.43,.76,2.76),.10,n=12,bias=-1)
        tube('Return piston shaft '+str(side),(side*.43,.66,2.45),(side*.43,.83,3.09),.055,n=12,bias=1)
    axis=Vector((0,-math.cos(math.radians(37)),math.sin(math.radians(37))));u=Vector((1,0,0));v=axis.cross(u);root=Vector((0,-.30,3.22))
    for i,(xx,zz) in enumerate(((-.43,-.34),(.43,-.34),(-.43,.34),(.43,.34))):
        p=root+u*xx+v*zz
        tube('Independent breech sleeve '+str(i),p-axis*.30,p+axis*.68,.245,n=16,bias=-1)
        tube('Hollow flak barrel '+str(i),p+axis*.56,p+axis*3.83,.145,inner=.082,n=16,bias=-1)
        for j,(start,end,radius) in enumerate(((.685 if c['args'].iteration>=12 else .66,.88,.245),(.93,1.16,.22),(1.21,1.42,.205),(1.55,2.32,.18),(2.45,3.18,.17),(3.45,3.845 if c['args'].iteration>=12 else 3.88,.20))):
            tube('Stepped barrel jacket '+str(i)+' '+str(j),p+axis*start,p+axis*end,radius,inner=.13 if c['args'].iteration>=13 else .143,n=16,bias=0)
        tube('Visible muzzle rim '+str(i),p+axis*3.85,p+axis*3.92,.20,inner=.085,n=16,bias=1)
    tube('Identification mast',(.40,.95,3.56),(.40,.95,4.84),.027,n=8,bias=-1)
    mesh('Source blue pennant',[(.43,.95,4.83),(.96,.95,4.77),(.96,.95,4.37),(.43,.95,4.47)],[(0,1,2,3)],uv={0:[(84,9),(92,10),(92,16),(84,14)]})
