"""Source-led Sonic receiver/yoke and volumetric emitter reconstruction.
Hidden sides are inferred mechanical counterparts, not projected source artwork.
"""
import math
from mathutils import Vector

def build(c):
    mesh,tube,block=c['mesh'],c['tube'],c['block'];lit=c['lit'];materials=c['materials']
    # Unequal cross sections: raised rear shoulders and a narrowing emitter neck.
    sections=[(-2.12,.68,3.82,5.12),(-1.86,.96,3.67,5.46),(-.88,1.04,3.65,5.55),(-.15,.99,3.83,5.44),(.45,.70,4.02,5.14)]
    if c['iteration']>=12:
        # Broader swept shoulders and lowered forward armor skirts, not a small
        # rectangular gearbox behind oversized discs. The bearing remains exposed.
        sections=[(-2.42,.76,3.84,5.12),(-2.25,.96,3.70,5.38),(-1.95,1.13,3.66,5.61),(-1.10,1.18,3.70,5.71),(-.55,1.17,3.91,5.66),(-.08,1.12,3.72,5.48),(.42,.98,3.42,5.17),(.72,.79,3.47,4.91)]
    vs=[]
    for x,w,lo,hi in sections:
        b=.22
        vs += [(x,y,z) for y,z in [(-w,lo+b),(-w+b,lo),(w-b,lo),(w,lo+b),(w,hi-b),(w-b,hi),(-w+b,hi),(-w,hi-b)]]
    fs=[tuple(range(7,-1,-1)),tuple((len(sections)-1)*8+i for i in range(8))]
    fs += [(j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i) for j in range(len(sections)-1) for i in range(8)]
    tex={}
    for fi,face in enumerate(fs):
        if fi%8!=7:continue
        tex[fi]=[]
        for vi in face:
            x,y,z=vs[vi];a=(x+2.42)/3.14 if c['iteration']>=12 else (x+2.12)/2.57;b=(y+1.18)/2.36 if c['iteration']>=12 else (y+1.04)/2.08
            tex[fi].append((26+20*a+b*(20+2*a),10+12*a+b*(-8-2*a)))
    mesh('SONIC tapered shoulder housing',vs,fs,'armor',tex,True)
    rear=-2.43 if c['iteration']>=12 else -2.13
    block('SONIC rear inset service cover',(rear,0,4.40),(.055,1.02,.60))
    for z in (4.20,4.34,4.48,4.62):block('SONIC rear recessed cooling louver',(rear-.04,0,z),(.07,.85,.045))
    # The broad structural yoke carries the receiver axle, instead of a thin box leg.
    tube('SONIC circular rotating saddle',(0,0,2.42),(0,0,2.66),1.42,n=48)
    tube('SONIC saddle edge',(0,0,2.60),(0,0,2.70),1.46,inner=1.30,n=48)
    outline=[(-.98,2.65),(.58,2.65),(.77,2.91),(.57,3.18),(.12,3.40),(-.10,3.80),(-.12,4.23),(-.34,4.49),(-.80,4.49),(-1.10,4.20),(-1.08,3.77),(-.82,3.28)]
    for side in (-1,1):
        yy=side*.89;vs=[(x,yy+d,z) for d in (-.18,.18) for x,z in outline];n=len(outline)
        fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)]
        if c['iteration']>=14:fs=[tuple(reversed(face)) for face in fs]
        mesh('SONIC broad sculpted yoke '+str(side),vs,fs)
        for z in (3.12,3.40):block('SONIC yoke stepped reinforcement '+str(side),(0,side*1.085,z),(.57,.08,.13))
        a=Vector((-.58,side*1.08,4.10));axis=Vector((0,side,0))
        tube('SONIC axle outer shoulder '+str(side),a,a+axis*.15,.66,inner=.46)
        tube('SONIC axle recessed hub '+str(side),a-axis*.02,a+axis*.105,.44,n=24)
        tube('SONIC axle center boss '+str(side),a+axis*.106,a+axis*.16,.28,n=12)
    axis=Vector((1,0,.16)).normalized();u=Vector((0,1,0));v=axis.cross(u);root=Vector((.20,0,4.52))
    tube('SONIC continuous emitter spine',root-axis*.2,root+axis*2.98,.70,n=40)
    tube('SONIC receiver neck collar',root-axis*.12,root+axis*.30,.85,inner=.69,n=40)
    for distance in (1.16,1.42,2.34,2.60):tube('SONIC intermediate spine collar',root+axis*distance,root+axis*(distance+.10),.76,inner=.69,n=40)
    def turned(name,center,profile):
        n=64;vs=[tuple(center+axis*d+r*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for d,r in profile for i in range(n)]
        fs=[(j*n+i,j*n+(i+1)%n,((j+1)%len(profile))*n+(i+1)%n,((j+1)%len(profile))*n+i) for j in range(len(profile)) for i in range(n)]
        return mesh(name,vs,fs)
    for i,(distance,radius) in enumerate(((.80,1.48),(1.94,1.68),(3.12,1.86))):
        center=root+axis*distance
        # Thick rear cone, beveled outer lip, recessed sloping dish and actual bore.
        profile=[(-.30,.76),(-.25,radius-.25),(-.08,radius-.04),(.02,radius),(.16,radius),(.25,radius-.09),(.24,radius-.24),(.05,.88),(-.08,.76)]
        if c['iteration']>=12:
            profile=[(-.36,.76),(-.29,radius-.27),(-.10,radius-.05),(.02,radius),(.18,radius),(.28,radius-.09),(.25,radius-.24),(.12,radius-.43),(-.13,.90),(-.13,.76)]
        turned('SONIC profiled emitter dish '+str(i),center,profile)['shade_bias']=1
        turned('SONIC dark recessed rim channel '+str(i),center,[(.245,radius-.26),(.245,radius-.32),(.19,radius-.34),(.19,radius-.26)])['shade_bias']=-2
        turned('SONIC inner machined lip '+str(i),center,[(.055,.90),(.095,.87),(.095,.77),(.025,.77)])
        for j in range(4):
            radial=(radius-.36)*(u*math.cos(j*math.pi/2+math.pi/4)+v*math.sin(j*math.pi/2+math.pi/4))
            tube('SONIC dish recessed fastener',center+radial+axis*.15,center+radial+axis*.19,.045,n=8)
    if c['iteration']>=12:
        # The reference has a substantial inset gray neck and longitudinal ribs
        # between its gold shoulder armor and the first emitter, not a bare rod.
        neck=root+axis*.13
        turned('SONIC recessed neck shoulder',neck,[(-.33,.72),(-.25,.96),(.02,1.02),(.26,.90),(.30,.72)])
        for angle in (-math.pi/2,-math.pi/4,0,math.pi/4,math.pi/2):
            radial=.94*(u*math.cos(angle)+v*math.sin(angle))
            tube('SONIC neck longitudinal reinforcement',neck+radial-axis*.20,neck+radial+axis*.18,.055,n=8)
    center=root+axis*3.12
    if c['iteration']>=15:
        # Explicit user feedback: nested physical dishes INSIDE the front emitter,
        # not only the three large external discs or painted concentric circles.
        turned('SONIC front inner dish outer',center,[(.055,1.40),(.20,1.40),(.26,1.34),(.21,1.21),(.10,1.02),(-.10,1.02)])['shade_bias']=1
        turned('SONIC front inner dish middle',center,[(.08,1.04),(.30,1.04),(.36,.98),(.31,.88),(.23,.74),(.08,.74)])['shade_bias']=1
        turned('SONIC inner dish outer recessed separator',center,[(.255,1.33),(.255,1.28),(.22,1.28),(.22,1.33)])['shade_bias']=-1
        turned('SONIC inner dish middle recessed separator',center,[(.35,.97),(.35,.92),(.32,.92),(.32,.97)])['shade_bias']=-1
    turned('SONIC lens deep socket',center,[(-.08,.77),(.10,.77),(.16,.66),(.12,.58),(-.08,.58)])
    for name,radius,depth,role in [('SONIC recessed cyan glass',.575,-.035,'cyan'),('SONIC luminous inner core',.37,-.029,'lightcyan')]:
        o=tube(name,center+axis*(depth-.015),center+axis*depth,radius,n=48)
        for p in o.data.polygons:p.material_index=list(materials).index(role)
        lit[:]=[item for item in lit if item[0]!=o]
    # Small external fittings stay mechanically separate from the receiver paint.
    for side in (-1,1):
        side_y=1.18 if c['iteration']>=12 else .98
        block('SONIC side sensor backing '+str(side),(-1.63,side*side_y,4.48),(.40,.10,.27))
        quad=[(-1.82,side*(side_y+.055),4.60),(-1.44,side*(side_y+.055),4.60),(-1.44,side*(side_y+.055),4.36),(-1.82,side*(side_y+.055),4.36)]
        mesh('SONIC bounded source status panel '+str(side),quad,[(0,1,2,3)],'metal',{0:[(14,23),(23,23),(23,29),(14,29)]})
        tube('SONIC side cable '+str(side),(-1.78,side*1.04,4.13),(-1.05,side*1.11,3.90),.055,n=8)
    dish=Vector((-1.58,.20,6.07 if c['iteration']>=12 else 5.72));normal=Vector((.30,-.85,.35)).normalized()
    tube('SONIC radar pedestal',dish-Vector((0,0,.30)),dish,.055,n=8)
    tube('SONIC radar ring',dish-normal*.04,dish+normal*.035,.27,inner=.21,n=24)
    tube('SONIC radar inset',dish-normal*.045,dish-normal*.035,.21,n=24)
    if c['iteration']>=13:
        from detail_sonic_turret import refine
        refine(c,sections)
