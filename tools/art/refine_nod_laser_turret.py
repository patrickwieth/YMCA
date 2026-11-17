"""Incremental source-fidelity refinements, invoked for Nod passes >=5.
The approved direction of pass-04 remains available, unchanged, as a comparison.
"""
import math
import bpy

def refine(c):
    iteration=c['args'].iteration
    objects,records=c['objects'],c['records']
    def remove(prefix):
        for o in list(objects):
            if o.name.startswith(prefix):
                objects.remove(o);records[:]=[r for r in records if r['name']!=o.name];bpy.data.objects.remove(o,do_unlink=True)
    def lathe(name,axis,center,sections,bias=0,family='steel',n=48):
        # Closed sectional profile: real curved shoulders and chamfers, not ink pretending to be volume.
        vs=[]
        for along,r in sections:
            for i in range(n):
                t=i*math.tau/n
                if axis=='X':vs.append((center[0]+along,center[1]+r*math.cos(t),center[2]+r*math.sin(t)))
                else:vs.append((center[0]+r*math.cos(t),center[1]+along,center[2]-r*math.sin(t)))
        fs=[(j*n+i,j*n+(i+1)%n,((j+1)%len(sections))*n+(i+1)%n,((j+1)%len(sections))*n+i) for j in range(len(sections)) for i in range(n)]
        if axis=='Y' and iteration>=6:fs=[tuple(reversed(f)) for f in fs]
        return c['mesh'](name,vs,fs,family=family,bias=bias)
    # A thicker, stepped barrel jacket. Keep the muzzle registration and native camera fixed.
    remove('Barrel main shroud');remove('Barrel front armor');remove('Red barrel band');remove('Barrel red side rail')
    lathe('Stepped armored barrel jacket','Y',(0,0,4.4),[(-1.92,.68),(-2.08,.73),(-2.25,.67),(-2.68,.67),(-2.77,.75),(-3.80,.75),(-3.96,.83),(-4.30,.83),(-4.30,.56),(-1.92,.56)])
    for yy,r in ((-2.70,.76),(-3.91,.84)):
        c['tube']('Red barrel band '+str(yy),(0,yy,4.4),(0,yy-.12,4.4),r,inner=r-.08,family='red',bias=1,n=32)
    for xx in (-.46,.46):c['box']('Barrel red side rail',(xx,-3.24,3.96),(.13,1.24,.19),'red',bias=1)
    # The source has deep rounded bearing housings, not thin coin-like rings with separate bolt dots.
    for kind,y,z,r,x in (('Main trunnion',1.98,4.30,1.35,1.04),('Lower bearing',.42,1.48,.84,1.08)):
        for side in (-1,1):
            name=kind+' '+str(side);remove(name+' outer drum');remove(name+' raised rim');remove(name+' tooth')
            sections=[(-.28,.96),(0,1.0),(.14,.98),(.28,.89),(.28,.72),(.08,.72),(-.28,.84)]
            # Reverse profile winding for the opposite side, preserving actual outward normals.
            section=[(side*d,r*rr) for d,rr in sections]
            if side<0:section.reverse()
            lathe(name+' rounded housing','X',(side*x,y,z),section,bias=1)
            c['tube'](name+' connected gear root',(side*(x+.08),y,z),(side*(x+.14),y,z),r*.59,inner=r*.42,bias=1,n=32,group=name+' teeth')
            for i in range(8):
                t=i*math.tau/8;vs=[]
                for xx in (side*(x+.08),side*(x+.16)):
                    for rr,tt in ((r*.43,t-.19),(r*.77,t-.19),(r*.77,t+.19),(r*.43,t+.19)):vs.append((xx,y+rr*math.cos(tt),z+rr*math.sin(tt)))
                fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
                if side<0:fs=[tuple(reversed(f)) for f in fs]
                c['mesh'](name+f' tooth {i}',vs,fs,bias=2,group=name+' teeth')
    remove('Inner support rib')
    pts=[(-1.05,.70),(-.8,1.8),(-.3,2.65),(.65,3.4)]
    for x in (-.42,0,.42):
        for j in range(3):c['tube'](f'Inner support rib {x} {j}',(x,*pts[j]),(x,*pts[j+1]),.12,bias=1,n=12,group=f'Inner rib {x}')
    # More of the source's pale pink core, without bloom or changing alpha.
    for name,width,height in (('Recessed red emitter',.30,.74),('Emitter bright core',.16,.66)):
        o=bpy.data.objects[name];xs=[v.co.x for v in o.data.vertices];zs=[v.co.z for v in o.data.vertices];cx=(max(xs)+min(xs))/2;cz=(max(zs)+min(zs))/2
        for v in o.data.vertices:v.co.x=cx+(v.co.x-cx)*width/(max(xs)-min(xs));v.co.z=cz+(v.co.z-cz)*height/(max(zs)-min(zs))
    for o in objects:
        if o.name.startswith('Structural yoke'):
            bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Rounded support edges','BEVEL');mod.width=.09;mod.segments=2;mod.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=mod.name)
    if iteration>=9:
        # The original has a circular outer muzzle and a recessed rectangular
        # aperture, not a second octagonal/rectangular attachment on the barrel.
        remove('Octagonal muzzle armor')
        n=64;vs=[]
        for kind,yy in (('outer',-4.30),('bevel',-4.38),('opening',-4.38),('inside',-4.17)):
            for i in range(n):
                t=i*math.tau/n;co,si=math.cos(t),math.sin(t)
                radius=.83 if kind=='outer' else .77 if kind=='bevel' else 1/max(abs(co)/.33,abs(si)/.52)
                vs.append((radius*co,yy,4.4+radius*si))
        fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(3) for i in range(n)]
        c['mesh']('Circular muzzle with rectangular aperture',vs,fs)
        for name in ('Muzzle black recess','Copper emitter bezel','Emitter dark rim','Recessed red emitter','Emitter bright core'):
            for vertex in bpy.data.objects[name].data.vertices:vertex.co.y+=.14
    for rec in records:
        o=bpy.data.objects[rec['name']];rec['vertices']=len(o.data.vertices);rec['faces']=len(o.data.polygons)
