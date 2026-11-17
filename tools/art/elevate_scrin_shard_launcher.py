"""Pass15+: elevate the accelerator, preserve fixed roots and yaw-local magazine."""
import math
from mathutils import Vector,Matrix

def apply(c):
 stage=c['S'];objects=c['objects'];angle=25 if stage==15 or stage>=18 else 35
 origin=c.get('elevation_origin',Vector((-.25,-1.86,4.62)));old_axis=c['tip']-c.get('cannon_root',Vector((-.25,-.65,4.65)))
 delta=math.radians(angle)-math.atan2(old_axis.z,math.hypot(old_axis.x,old_axis.y))
 rotation=Matrix.Rotation(delta,3,'X')
 def raised(v):return origin+rotation@(v-origin)
 for o in objects:
  if o['fixed'] or any(s in o.name.lower() for s in ('magazine','crown crystal','crown dark')):continue
  rigid=any(s in o.name.lower() for s in ('accelerator','receiver','cheek','long faceted','crystal branching','terminal jaw')) or (stage>=26 and o.name.startswith('Terminal split claw'))
  # Stage16 preserves the hanging accelerator arteries as complete swept tubes.
  if stage==15 and 'underside return' in o.name:rigid=False
  for vertex in o.data.vertices:
   v=vertex.co.copy();low=c['PIVOT']+.20 if stage>=24 else 2.35
   w=1 if rigid else max(0,min(1,(v.z-low)/(c.get('elevation_neck_high',4.0)-low)));w=w*w*(3-2*w)
   vertex.co=v.lerp(raised(v),w)
  o.data.update()
 c['tip']=raised(c['tip']);c['elevation_degrees']=angle
 if 17<=stage<23:
  # Flexible returns join the raised receiver to the moving cervical collar only.
  for side in (-1,1):
   for j in range(3):
    x=-.25+side*(.70+j*.10);end=raised(Vector((x,-.66+j*.12,4.12)))
    c['vein'](f'Elevated receiver cervical return {side}/{j}',[tuple(end),(x+side*.12,-.45+j*.13,3.82),(side*(.85+j*.08),-.43,3.07),(side*.61,-.14+j*.07,2.39)],.055+j*.008,bias=1)
