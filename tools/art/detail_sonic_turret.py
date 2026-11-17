"""Part-local Sonic armor, yoke and emitter detailing after reference review.
No whole-image projection. Hidden-side construction is a mechanical counterpart.
"""
import math
import bpy
from mathutils import Vector

def refine(c,sections):
    mesh,tube,block=c['mesh'],c['tube'],c['block'];lit=c['lit']
    def section(x):
        for a,b in zip(sections,sections[1:]):
            if a[0]<=x<=b[0]:
                t=(x-a[0])/(b[0]-a[0]);return tuple(a[i]+t*(b[i]-a[i]) for i in range(1,4))
        raise ValueError(x)
    # Discard the broad old roof projection, including its misplaced dark marks.
    housing=bpy.data.objects['SONIC tapered shoulder housing']
    for i,(o,family,excluded) in enumerate(lit):
        if o==housing:lit[i]=(o,family,set());housing['shaded_texture']=False
    # Two separate roof access plates, with a real narrow panel joint. Each cut
    # depicts only the corresponding gold roof, not the antenna or receiver neck.
    for i,(x0,x1,quad) in enumerate(((-1.94,-1.15,[(25,12),(35,16),(47,9),(37,6)]),(-1.12,-.10,[(35,16),(47,21),(62,13),(47,9)]))):
        w0,_,h0=section(x0);w1,_,h1=section(x1)
        vs=[(x0,-w0+.24,h0+.022),(x1,-w1+.24,h1+.022),(x1,w1-.24,h1+.022),(x0,w0-.24,h0+.022)]
        mesh('SONIC independent roof armor '+str(i),vs,[(0,1,2,3)],'armor',{0:quad},True)
    w,_,h=section(-1.135)
    tube('SONIC roof transverse joint',(-1.135,-w+.23,h+.01),(-1.135,w-.23,h+.01),.018,n=8)['shade_bias']=-2
    for side in (-1,1):
        # Native-sized access panels and protective strips, not tiny black dots.
        y=side*1.24
        vs=[(-1.93,y,4.67),(-1.32,y,4.67),(-1.32,y,4.30),(-1.93,y,4.30)]
        mesh('SONIC framed diagnostic panel '+str(side),vs,[(0,1,2,3)],'metal',{0:[(14,23),(24,23),(24,30),(14,30)]})
        # The small earlier panel is hidden by this larger, correctly sampled face.
        tube('SONIC lower rear protective rail '+str(side),(-2.18,side*1.06,4.01),(-1.23,side*1.24,3.90),.075,n=10)
        tube('SONIC rail return '+str(side),(-2.18,side*1.06,4.01),(-2.18,side*1.06,4.35),.075,n=10)
        # Separate gold inspection hatch on the lower yoke, as seen below the axle.
        block('SONIC yoke gold inspection cover '+str(side),(-.48,side*1.10,3.17),(.46,.075,.37),'armor')
        tube('SONIC cover recessed lock '+str(side),(-.48,side*1.14,3.17),(-.48,side*1.18,3.17),.055,inner=.025,n=10)
    # Round the heavy supporting cheeks rather than outlining a flat polygon.
    for o,family,excluded in list(lit):
        if not o.name.startswith('SONIC broad sculpted yoke'):continue
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Machined yoke edge','BEVEL');mod.width=.065;mod.segments=2;mod.limit_method='ANGLE'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Actual raised returns on the backs of the emitter dishes. These read in
    # side/reverse views, instead of disappearing into featureless gray plates.
    axis=Vector((1,0,.16)).normalized();u=Vector((0,1,0));v=axis.cross(u);root=Vector((.20,0,4.52))
    for i,(distance,radius) in enumerate(((.80,1.48),(1.94,1.68),(3.12,1.86))):
        center=root+axis*distance
        depth=-.36+.07*(radius-.37-.76)/(radius-.27-.76)
        back,front=(depth-.045,depth+.008) if c['iteration']>=14 else (-.265,-.225)
        tube('SONIC rear concentric stiffener '+str(i),center+axis*back,center+axis*front,radius-.34,inner=radius-.40,n=56)
        for j in range(6):
            angle=j*math.tau/6;radial=u*math.cos(angle)+v*math.sin(angle)
            tube('SONIC rear radial web '+str(i)+' '+str(j),center-axis*(.38 if c['iteration']>=14 else .30)+radial*.83,center+axis*(depth-.025 if c['iteration']>=14 else -.235)+radial*(radius-.38),.032,n=8)
    # Coherent height-dependent tonal steps supplement face-normal lighting.
    # Quantization into the original palette happens at export; no RGB noise.
    for key,mat in c['materials'].items():
        if not key.startswith(('REWORK armor ','REWORK metal ')):continue
        n=mat.node_tree.nodes;links=mat.node_tree.links;em=next(node for node in n if node.type=='EMISSION')
        if em.inputs[0].links:continue
        coord=n.new('ShaderNodeTexCoord');xyz=n.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Generated'],xyz.inputs[0])
        ramp=n.new('ShaderNodeMapRange');ramp.inputs['To Min'].default_value=.79;ramp.inputs['To Max'].default_value=1.12;links.new(xyz.outputs['Z'],ramp.inputs['Value'])
        mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=em.inputs[0].default_value
        links.new(ramp.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0])
    if c['iteration']>=21:
        from detail_sonic_reference_hardware import apply as add_reference_hardware
        add_reference_hardware(c)
    if c['iteration']>=16:
        from texture_sonic_armor import apply
        apply(c)
    for rec in c['c']['records']:
        o=bpy.data.objects[rec['name']];rec['vertices']=len(o.data.vertices);rec['faces']=len(o.data.polygons)
