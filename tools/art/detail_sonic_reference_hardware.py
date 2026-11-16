"""Pass21: reference pipes, flexible leads and real small roof reflector.
Large emitters, armor and yoke are unchanged. Hidden-side pipe routing is inferred.
"""
import math
import bpy
from mathutils import Vector

def apply(c):
    mesh,tube,block=c['mesh'],c['tube'],c['block']
    def fixed(o):
        c['c']['moving'].objects.unlink(o);c['c']['fixed'].objects.link(o)
        o.parent=None;o.location=(0,0,0)
        next(r for r in c['c']['records'] if r['name']==o.name)['stationary']=True
        return o
    def hose(name,points,radius=.065,stationary=False,bias=-1):
        # Catmull-Rom bends with genuine round cross sections, not flat strips.
        p=list(map(Vector,points));samples=[]
        for i in range(len(p)-1):
            a,b,d,e=p[max(0,i-1)],p[i],p[i+1],p[min(len(p)-1,i+2)]
            for j in range(5):
                t=j/5;samples.append(.5*((2*b)+(-a+d)*t+(2*a-5*b+4*d-e)*t*t+(-a+3*b-3*d+e)*t*t*t))
        samples.append(p[-1]);n=10;vs=[]
        for i,center in enumerate(samples):
            axis=(samples[min(i+1,len(samples)-1)]-samples[max(0,i-1)]).normalized()
            u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.85 else Vector((0,1,0))).normalized();v=axis.cross(u)
            vs.extend(tuple(center+radius*(u*math.cos(k*math.tau/n)+v*math.sin(k*math.tau/n))) for k in range(n))
        fs=[(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(len(samples)-1) for k in range(n)]
        fs.extend([tuple(range(n-1,-1,-1)),tuple((len(samples)-1)*n+k for k in range(n))])
        o=mesh(name,vs,fs);o['shade_bias']=bias
        return fixed(o) if stationary else o
    for side in (-1,1):
        for i,z in enumerate((4.13,4.46)):
            hose(f'SONIC reference twin return pipe {side} {i}',[(-2.48,side*.52,z),(-2.62,side*.70,z),(-2.60,side*1.16,z),(-2.40,side*1.36,z),(-1.20,side*1.36,z)],.085,bias=0)
            tube(f'SONIC return pipe coupling {side} {i}',(-1.31,side*1.36,z),(-1.13,side*1.36,z),.12,n=12)
        hose(f'SONIC receiver hanging cable {side}',[(-1.28,side*1.34,4.05),(-1.36,side*1.35,3.66),(-1.12,side*1.32,3.40),(-.78,side*1.22,3.51)],.057)
        block(f'SONIC twin pipe anchor {side}',(-2.48,side*1.28,4.30),(.12,.16,.55))
    # Fixed supply leads enter below the rotating bearing. They do not rotate
    # with the gun or stretch between independently moving parts.
    for i,dx in enumerate((0,.22)):
        hose(f'SONIC fixed supply loop {i}',[(-.22+dx,-1.34,2.39),(-.42+dx,-1.68,2.18),(-.47+dx,-1.97,1.75),(-.55+dx,-2.10,.78),(-.78+dx,-2.25,.40),(-1.28+dx,-2.15,.35),(-1.65+dx,-1.85,.52),(-1.45+dx,-1.35-dx,.73)],.068,True)
        fixed(tube(f'SONIC fixed lower cable gland {i}',(-1.49+dx,-1.39-dx,.73),(-1.39+dx,-1.29-dx,.73),.105,n=12))
        fixed(tube(f'SONIC fixed cable socket {i}',(-.22+dx,-1.34,2.27),(-.22+dx,-1.34,2.40),.105,n=12))
    names={'SONIC radar pedestal','SONIC radar ring','SONIC radar inset'}
    for name in names:
        o=bpy.data.objects[name];c['lit'][:]=[entry for entry in c['lit'] if entry[0]!=o]
        c['c']['records'][:]=[r for r in c['c']['records'] if r['name']!=name];bpy.data.objects.remove(o,do_unlink=True)
    center=Vector((-1.58,.20,6.07));axis=Vector((.30,-.85,.35)).normalized();u=axis.cross(Vector((0,0,1))).normalized();v=axis.cross(u)
    tube('SONIC reference radar stalk',(-1.58,.20,5.58),center,.06,n=10)
    profile=[(-.13,.02),(-.10,.14),(-.035,.27),(.045,.32),(.075,.30),(.01,.25),(-.055,.12),(-.07,.02)]
    n=32;vs=[tuple(center+axis*d+r*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for d,r in profile for i in range(n)]
    fs=[(j*n+i,j*n+(i+1)%n,((j+1)%len(profile))*n+(i+1)%n,((j+1)%len(profile))*n+i) for j in range(len(profile)) for i in range(n)]
    mesh('SONIC reference concave radar reflector',vs,fs)
    tube('SONIC reference radar feed',center-axis*.07,center+axis*.14,.026,n=8)
