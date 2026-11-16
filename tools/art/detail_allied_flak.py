"""Source-led detailed flak foundation and breeches, beyond the pass07 blockout.
Only paint/marks use palette colors; mechanics remain genuine independent meshes.
"""
import math
import bpy
from mathutils import Vector

def apply(c):
    mesh,box,tube=c['mesh'],c['box'],c['tube'];objects=c['objects'];iteration=c['args'].iteration
    def remove(prefixes):
        for o in list(objects):
            if o.name.startswith(prefixes):objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
    # The source exposes barrel breeches/recoil sleeves, not a flat roof with slots.
    remove(('Upper stepped breech armor','Breech roof cooling inset'))
    if iteration>=9:
        # Pass08 inspection: the original boxes still buried the new circular
        # breeches. Remove those covers and lower the supporting core only.
        remove(('Independent breech housing',))
        if iteration>=10:
            # The pass09 base was over-segmented: restore broad armor faces
            # instead of stacking thin decorative ribs over the same stiles.
            remove(('Fixed raised armor rib','Fixed rib foot','Fixed armor screw'))
        core=bpy.data.objects['Split receiver lower core']
        for vertex in core.data.vertices:vertex.co.z=3.10+(vertex.co.z-3.10)*(.50/.76)
    def panel(name,normal,tangent,center,w,h,thick=.07,bias=0,stationary=True):
        center=Vector(center);vs=[tuple(center-normal*d+tangent*x+Vector((0,0,z))) for d in (0,thick) for x,z in ((-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2))]
        return mesh(name,vs,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],stationary,bias)
    # Side panels were previously missing between the narrow vents. Stepped feet,
    # bordered hatches and screw seats now occupy their actual pedestal facets.
    for k in range(8):
        a=(k+.5)*math.pi/4;n=Vector((math.cos(a),math.sin(a),0));t=Vector((-n.y,n.x,0))
        for side in (-1,1):
            center=n*1.79+t*(side*.52)+Vector((0,0,.88))
            panel(f'Fixed broad armored stile {k} {side}',n,t,center,.34 if iteration>=10 else .22,.90,.10,1)
            panel(f'Fixed stepped stile foot {k} {side}',n,t,n*1.84+t*(side*.52)+Vector((0,0,.51)),.42 if iteration>=10 else .29,.26,.14)
            tube(f'Fixed recessed stile bolt {k} {side}',center+Vector((0,0,.33)),center+n*.025+Vector((0,0,.33)),.035,n=8,stationary=True,bias=-2)
        # Deliberately asymmetric service details, not identical photo fragments.
        x=.48 if k%2==0 else -.48
        p=n*1.81+t*x+Vector((0,0,.94))
        if iteration<10 or k in (1,4,6):
            panel('Fixed service hatch border '+str(k),n,t,p,.19,.28,.035,-2)
            panel('Fixed inset service hatch '+str(k),n,t,p+n*.008,.12,.20,.02,0)
            tube('Fixed hatch latch '+str(k),p+n*.014,p+n*.04,.025,n=6,stationary=True,bias=1)
    for x,y in ((-1.62,-1.62),(1.62,-1.62),(-1.62,1.62),(1.62,1.62)):
        box('Fixed corner reinforcing pad',(x,y,.39),(.30,.30,.065),True,bias=-1)
        tube('Fixed plinth anchor',(x,y,.425),(x,y,.46),.055,n=8,stationary=True,bias=1)
        for d in (-.08,0,.08):box('Fixed corner recessed groove',(x+d,y,.43),(.028,.23,.008),True,bias=-2)
    # Muted safety marks correspond to the supplied plinth, not a new bright bib.
    copper=next(i for i,m in enumerate(c['mats']) if m.name.startswith('Source copper'))
    for side in (-1,1):
        for j in range(5):
            y=-.65+j*.29;x=side*1.77
            o=mesh('Fixed plinth faded safety stripe',[(x-.07,y-.10,.363),(x+.07,y-.02,.363),(x+.07,y+.12,.363),(x-.07,y+.04,.363)],[(0,1,2,3)],True,bias=1);o['family_base']=copper
    axis=Vector((0,-math.cos(math.radians(37)),math.sin(math.radians(37))));u=Vector((1,0,0));v=axis.cross(u);root=Vector((0,-.30,3.22))
    for i,(xx,zz) in enumerate(((-.43,-.34),(.43,-.34),(-.43,.34),(.43,.34))):
        p=root+u*xx+v*zz
        tube('Individual rear breech cylinder '+str(i),p-axis*.76,p-axis*.24,.28,n=24)
        tube('Breech rear machined cap '+str(i),p-axis*.81,p-axis*.75,.24,n=24,bias=-1)
        tube('Breech locking collar '+str(i),p-axis*.33,p-axis*.22,.32,inner=.25,n=24,bias=1)
        for sign in (-1,1):
            off=u*sign*.21+v*.18
            tube('Breech return sleeve',p-axis*.63+off,p+axis*.16+off,.065,n=12,bias=-1)
            tube('Breech return bright rod',p+axis*.14+off,p+axis*.43+off,.035,n=10,bias=1)
        for d in (-.55,-.39):
            q=p-axis*(-d)+v*.26
            tube('Breech upper locking stud',q,q+v*.05,.028,n=8,bias=1)
    # Recessed cover and connected drive details below the elevation axle.
    panel('Rotating front service cover',Vector((0,-1,0)),Vector((1,0,0)),(0,-.60,2.32),1.08,.48,.10,-1,False)
    for x in (-.36,.36):
        for z in (2.17,2.47):tube('Front service screw',(x,-.607,z),(x,-.64,z),.032,n=8,bias=1)
    for side in (-1,1):
        tube('Lower elevation drive rim '+str(side),(side*.94,.27,2.25),(side*1.04,.27,2.25),.19,inner=.125,n=20)
        tube('Lower elevation drive inset '+str(side),(side*.945,.27,2.25),(side*1.015,.27,2.25),.12,n=16,bias=-2)
        tube('Feed guide connected elbow '+str(side),(side*.67,.36,3.11),(side*1.01,.60,3.16),.075,n=12)
        for y in (.40,.84):tube('Feeder cover screw',(side*1.22,y,3.24),(side*1.25,y,3.24),.035,n=8,bias=1)
    if iteration>=9:
        # Hand-placed small scuffs on exposed plinth paint. No random texture,
        # foreign vent/bolt projections or wholesale source-camera projection.
        for i,(x,y,w,h) in enumerate(((-1.35,-1.80,.25,.12),(-.65,-1.83,.18,.08),(.72,-1.81,.31,.10),(1.82,-.63,.09,.23),(1.81,.91,.10,.24),(-1.81,.58,.11,.20),(.85,1.81,.23,.09),(-.63,1.82,.18,.11))):
            vs=[(x-w/2,y-h/3,.365),(x-w/6,y-h/2,.365),(x+w/2,y,.365),(x+w/4,y+h/2,.365),(x-w/2,y+h/4,.365)]
            o=mesh('Fixed bounded paint scuff '+str(i),vs,[(0,1,2,3,4)],True,bias=-1 if i%2 else 0);o['surface_detail']=True
        if iteration>=11:
            # Reference-facing landmark comparison: the gun cluster sat roughly
            # five native pixels too low relative to the unchanged elevation axle.
            gun_names=('Split receiver','Rear ammunition','Individual feed fin','Independent breech sleeve','Hollow flak barrel','Stepped barrel jacket','Visible muzzle rim','Identification mast','Source blue pennant','Individual rear breech cylinder','Breech rear machined cap','Breech locking collar','Breech return sleeve','Breech return bright rod','Breech upper locking stud','Feed guide connected elbow','Feeder cover screw')
            for o in objects:
                if o.name.startswith(gun_names):
                    for vertex in o.data.vertices:vertex.co.z+=.32
                elif o.name.startswith('Copper service conduit'):
                    for vertex in o.data.vertices:vertex.co.z+=.32*max(0,min(1,(vertex.co.z-2.70)/.65))
            for side in (-1,1):
                tube('Elevated breech saddle strut '+str(side),(side*.58,-.03,2.98),(side*.58,.12,3.42),.10,n=12)
                tube('Breech saddle upper seat '+str(side),(side*.58,-.28,3.42),(side*.58,.39,3.42),.09,n=12,bias=1)
        for mat in c['mats']:
            if not mat.name.startswith(('Source steel','Source blue','Source copper')):continue
            n=mat.node_tree.nodes;links=mat.node_tree.links;em=next(node for node in n if node.type=='EMISSION')
            if em.inputs[0].links:continue
            coord=n.new('ShaderNodeTexCoord');xyz=n.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Generated'],xyz.inputs[0])
            ramp=n.new('ShaderNodeMapRange');ramp.inputs['To Min'].default_value=.76;ramp.inputs['To Max'].default_value=1.13;links.new(xyz.outputs['Z'],ramp.inputs['Value'])
            mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=em.inputs[0].default_value
            links.new(ramp.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0])
