"""Replace photographed vent cutouts with continuous physical louvers.
The existing stationary foundation and its source paint remain unchanged.
"""
import math
import bpy
from mathutils import Vector

def apply(c):
    removed=[]
    for o in list(c['fixed'].objects):
        if o.name.startswith('SONIC pedestal vent'):
            removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)
    c['records'][:]=[r for r in c['records'] if r['name'] not in removed]
    for angle in (-90,0,90,180):
        t=math.radians(angle);normal=Vector((math.cos(t),math.sin(t),0));tangent=Vector((-normal.y,normal.x,0))
        def panel(label,x,z,w,h,depth,role):
            center=normal*depth+tangent*x+Vector((0,0,z))
            vs=[tuple(center+tangent*xx+Vector((0,0,zz))) for xx,zz in ((-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2))]
            return c['mesh'](f'SONIC continuous vent {angle} {label}',vs,[(0,1,2,3)],rotating=False,material=role)
        panel('recess',0,1.00,.70,1.20,1.949,'dark')
        for x in (-.36,.36):panel('vertical frame '+str(x),x,1.00,.07,1.27,1.970,'steel')
        for z in (.38,1.62):panel('horizontal frame '+str(z),0,z,.79,.07,1.970,'lightsteel')
        for i in range(11):
            z=.48+i*.10
            panel('louver '+str(i),0,z,.62,.038,1.975,'steel')
            panel('louver highlight '+str(i),0,z+.020,.62,.012,1.978,'lightsteel')
