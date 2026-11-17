"""Requested shorter, deeper-seated barrels; other turret meshes unchanged."""
import re,math
from mathutils import Vector

def apply(c):
    axis=Vector((0,-math.cos(math.radians(37)),math.sin(math.radians(37))))
    u=Vector((1,0,0));v=axis.cross(u);root=Vector((0,-.30,3.54))
    offsets=((- .43,-.34),(.43,-.34),(-.43,.34),(.43,.34))
    for o in c['objects']:
        match=re.match(r'(?:Hollow flak barrel|Stepped barrel jacket|Visible muzzle rim) (\d+)',o.name)
        if match:
            xx,zz=offsets[int(match[1])];p=root+u*xx+v*zz
            for vertex in o.data.vertices:
                t=(vertex.co-p).dot(axis);vertex.co+=axis*((.56+(t-.56)*.80-.16)-t)
        elif o.name.startswith(('Individual rear breech cylinder','Breech rear machined cap','Breech locking collar','Breech return sleeve','Breech return bright rod','Breech upper locking stud')):
            for vertex in o.data.vertices:vertex.co-=axis*.16
        elif o.name.startswith('Independent breech sleeve'):
            # Extend the rear into the suspension rather than leave a front-only stub.
            center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
            for vertex in o.data.vertices:
                if (vertex.co-center).dot(axis)<0:vertex.co-=axis*.32
    if c['args'].iteration>=15:
        # Requested follow-up: move the complete barrel mechanism aft, then
        # expose more of the thick loading ends beyond the stationary cradle.
        group=('Hollow flak barrel','Stepped barrel jacket','Visible muzzle rim',
               'Independent breech sleeve','Individual rear breech cylinder',
               'Breech rear machined cap','Breech locking collar',
               'Breech return sleeve','Breech return bright rod','Breech upper locking stud')
        for o in c['objects']:
            if not o.name.startswith(group):continue
            for vertex in o.data.vertices:vertex.co-=axis*.28
            if o.name.startswith('Individual rear breech cylinder'):
                center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
                for vertex in o.data.vertices:
                    if (vertex.co-center).dot(axis)<0:vertex.co-=axis*.32
            elif o.name.startswith('Breech rear machined cap'):
                for vertex in o.data.vertices:vertex.co-=axis*.32
