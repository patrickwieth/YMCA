"""Reference-led Soviet flak follow-up, preserving the proven 1x1 fixed XY extent."""
import math
import bpy
from mathutils import Vector

def apply(c):
    mesh,tube,box,bevel=c['mesh'],c['tube'],c['box'],c['bevel'];objects=c['objects'];iteration=c['args'].iteration
    def remove(prefix):
        for o in list(objects):
            if o.name.startswith(prefix):objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
    # Cylindrical dots were not a connected toothed gear. Replace them with solid
    # radial tooth profiles joined to a root ring, inside the original armor rim.
    remove(('Axle gear tooth','Lower drive tooth'))
    for side in (-1,1):
        for label,y,z,r,inner,x in (('Main',-.20,1.92,.35,.16,.72),('Lower',.27,1.16,.20,.09,.73)):
            tube(label+' connected gear root '+str(side),(side*x,y,z),(side*(x+.045),y,z),r*.79,inner=inner,n=32,bias=1)
            for j in range(8):
                t=j*math.tau/8;vs=[]
                for xx in (side*(x-.015),side*(x+.055)):
                    vs += [(xx,y+rr*math.cos(tt),z+rr*math.sin(tt)) for rr,tt in ((r*.67,t-.20),(r,t-.17),(r,t+.17),(r*.67,t+.20))]
                fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
                if side<0:fs=[tuple(reversed(f)) for f in fs]
                mesh(label+' connected gear tooth '+str(side)+' '+str(j),vs,fs,bias=1)
    if iteration>=5:
        # The retaining beams used to bury their pad bolts. Raise only those bolts
        # to the top of the existing beam, without growing the one-cell footprint.
        for o in objects:
            if o.name.startswith('Fixed pad bolt'):
                for v in o.data.vertices:v.co.z+=.17
        for i in range(4):
            a=i*math.pi/2;n=Vector((math.cos(a),math.sin(a),0));t=Vector((-n.y,n.x,0));center=n*1.37+Vector((0,0,.378))
            if i%2==0:
                vs=[tuple(center+n*x+t*y) for x,y in ((-.06,-.18),(.06,-.18),(.06,.18),(-.06,.18))]
                mesh('Fixed outrigger red identifier '+str(i),vs,[(0,1,2,3)],True,'red',1)
            # Low secondary seam in the armored leg, away from the top bolt seat.
            for side in (-1,1):
                tube('Fixed outrigger longitudinal seam',n*.96+t*(side*.16)+Vector((0,0,.379)),n*1.25+t*(side*.16)+Vector((0,0,.379)),.012,n=6,stationary=True,bias=-1)
            p=n*1.08+Vector((0,0,.60))
            tube('Fixed collar clamp fastener',p,p+Vector((0,0,.04)),.04,n=8,stationary=True,bias=1)
    if iteration>=6:
        axis=c['axis'];u=c['u'];v=c['v'];root=c['root']
        # Machined retainers and rear cartridge closure on the actual tilted body.
        for distance in (-.58,.27):
            tube('Receiver stepped clamp',root+axis*distance,root+axis*(distance+.09),.395,inner=.35,n=32,bias=1)
            for side in (-1,1):
                p=root+axis*(distance+.045)+u*(side*.33)+v*.21
                tube('Receiver clamp bolt',p,p+u*(side*.05),.045,n=8,bias=1)
        tube('Rear receiver inset recess',root-axis*.96,root-axis*.91,.23,n=24,bias=-2)
        tube('Rear receiver locking ring',root-axis*.98,root-axis*.93,.25,inner=.19,n=24)
        for side in (-1,1):
            # Feed/return guide runs between its brackets, not across empty air.
            a=Vector((side*.40,.46,.85));b=Vector((side*.40,.66,1.40));d=root-axis*.63+u*(side*.40)
            tube('Rear articulated lower linkage '+str(side),a,b,.06,n=12)
            tube('Rear articulated upper linkage '+str(side),b,d,.055,n=12,bias=1)
            tube('Rear linkage pivot '+str(side),b-u*.07,b+u*.07,.095,n=12,bias=-1)
    if iteration>=9:
        # Reverse-view inspection: the rear ring was inside the armored end cap.
        # Seat it on that cap and keep the center slightly recessed behind its lip.
        axis=c['axis'];root=c['root']
        for o in objects:
            if o.name.startswith(('Rear receiver inset recess','Rear receiver locking ring')):
                for vertex in o.data.vertices:vertex.co-=axis*.10
        # The original also has a red identification star on the breech armor.
        for side in (-1,1):
            center=root-axis*.85+Vector((side*.401,0,0));vs=[]
            for j in range(10):
                t=math.pi/2+j*math.pi/5;r=.15 if j%2==0 else .062
                vs.append(tuple(center+c['rotation']@Vector((0,r*math.cos(t),r*math.sin(t)))))
            face=tuple(range(10)) if side>0 else tuple(range(9,-1,-1))
            mesh('Rear breech red identification '+str(side),vs,[face],family='red',bias=1)
        for o in objects:
            if o.name=='Red star identification -1':
                # The original left-side paint face had the right-side winding.
                import bmesh
                bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    if iteration>=10:
        # The source base carries more visual weight relative to the gun. Use
        # part of the spare one-cell margin for the feet/legs, without widening
        # the bearing socket or changing the rotating assembly registration.
        for o in objects:
            if not o['fixed'] or o.name.startswith(('Fixed center armored ring','Fixed bearing rim')):continue
            for vertex in o.data.vertices:vertex.co.x*=1.08;vertex.co.y*=1.08
            o.location.x*=1.08;o.location.y*=1.08
