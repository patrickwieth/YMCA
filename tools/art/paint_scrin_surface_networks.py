"""Deliberately drawn vascular loops, recesses and gills inside existing armor UVs.
No noise, no image projection, no extra meshes. Hidden decoration is inferred.
R is chitin relief; G is a shaded vascular channel; B is reserved.
"""
import math

def eligible(name):
 return any(s in name.lower() for s in ('receiver cocoon','receiver sculpted','accelerator trough','accelerator overlapping','curved side carapace','organic neck','cervical dorsal shield','convex standing shin','calf shield','lower crystal magazine','magazine scalloped','sensory hood','external bowed','shoulder carapace','standing battery','toe scute'))

def paint(pixels,W,ox,oy,w,h,name,revision=39):
 if min(w,h)<6 or w*h<100:return
 def point(x,y,r,g=None):
  x,y=int(x),int(y)
  if not (1<=x<w-1 and 1<=y<h-1):return
  k=((oy+y)*W+ox+x)*4
  if g is None:
   if pixels[k+1]==0:pixels[k]=r
  elif g>=pixels[k+1]:pixels[k]=r;pixels[k+1]=g
 def smooth(points):
  p=[(x*(w-1),y*(h-1)) for x,y in points];out=[]
  for j in range(len(p)-1):
   a=p[max(0,j-1)];b=p[j];d=p[j+1];e=p[min(len(p)-1,j+2)];steps=max(4,math.ceil(math.dist(b,d)*3))
   for k in range(steps):
    t=k/steps;out.append(tuple(.5*(2*b[c]+(-a[c]+d[c])*t+(2*a[c]-5*b[c]+4*d[c]-e[c])*t*t+(-a[c]+3*b[c]-3*d[c]+e[c])*t*t*t) for c in (0,1)))
  return out+[p[-1]]
 def stroke(points,vein=True,thin=False):
  path=smooth(points)
  for x,y in path:
   for yy in range(math.floor(y)-2,math.ceil(y)+3):
    for xx in range(math.floor(x)-2,math.ceil(x)+3):
     dist=math.hypot(xx-x,yy-y)
     if dist<=(.9 if thin else 1.35):point(xx,yy,.24,.18 if vein else None)
     elif dist<=(1.5 if thin else 2) and xx<x:point(xx,yy,.60)
  for x,y in path:
   point(round(x),round(y),.44,(.56 if thin else .69) if vein else None)
 def gills(cx,cy,span=.28,count=5):
  for j in range(count):
   y=round((cy+(j-(count-1)/2)*2/max(h,1))*(h-1));x0=round((cx-span/2)*(w-1));x1=round((cx+span/2)*(w-1))
   for x in range(x0,x1+1):point(x,y,.16);point(x,y-1,.58)
 def node(cx,cy):
  x,y=cx*(w-1),cy*(h-1)
  for yy in range(round(y)-3,round(y)+4):
   for xx in range(round(x)-3,round(x)+4):
    d=((xx-x)/2.4)**2+((yy-y)/3)**2
    if d<1:point(xx,yy,.18,.24)
  point(round(x)-1,round(y)-1,.55,.82)
 small=w<13
 if revision>=40 and 'accelerator' in name.lower():
  stroke([(.10,.34),(.32,.40),(.59,.32),(.88,.41)],thin=True)
  stroke([(.10,.72),(.36,.65),(.62,.74),(.90,.64)],thin=True)
  stroke([(.32,.40),(.50,.49),(.39,.57),(.36,.65)],thin=True)
  if w>=18:gills(.73,.51,.23,3)
  return
 if revision>=40 and min(w,h)<14:
  stroke([(.25,.10),(.40,.28),(.30,.48),(.57,.70),(.45,.92)],thin=True)
  stroke([(.30,.48),(.72,.56),(.79,.73)],thin=True)
  return
 if 'cap' in name.lower():
  stroke([(.15,.78),(.13,.36),(.36,.17),(.72,.24),(.84,.48),(.66,.72),(.40,.76),(.30,.52),(.48,.36),(.70,.41)])
  stroke([(.40,.76),(.53,.91),(.82,.83),(.88,.63)])
  gills(.51,.51,.30,4);node(.72,.25)
 elif 'accelerator' in name.lower():
  # Each curved plate has a rooted longitudinal channel and an inner return loop.
  stroke([(.24,.06),(.30,.25),(.18,.43),(.33,.61),(.24,.82),(.38,.95)])
  stroke([(.30,.25),(.65,.35),(.72,.53),(.48,.65),(.33,.61)])
  if not small:stroke([(.72,.53),(.85,.70),(.72,.89)]);gills(.65,.16,.26,3)
 elif 'shin' in name.lower() or 'calf' in name.lower():
  stroke([(.12,.15),(.29,.23),(.46,.16),(.66,.30),(.87,.22)])
  stroke([(.18,.84),(.38,.72),(.55,.80),(.74,.68),(.90,.76)])
  stroke([(.46,.16),(.48,.38),(.65,.49),(.55,.80)])
  gills(.26,.49,.28,5);node(.65,.49)
 else:
  # Connected asymmetric plexus, with a subsidiary pocket and embedded mechanisms.
  stroke([(.20,.05),(.33,.18),(.20,.35),(.36,.51),(.25,.72),(.38,.94)])
  stroke([(.33,.18),(.67,.24),(.81,.44),(.66,.62),(.36,.51)])
  stroke([(.66,.62),(.79,.79),(.64,.93)])
  if not small:
   stroke([(.20,.35),(.49,.36),(.58,.49),(.44,.65),(.25,.72)])
   gills(.59,.77,.26,4);node(.67,.24)
  else:gills(.65,.78,.28,3)
