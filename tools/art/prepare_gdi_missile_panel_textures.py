"""Assemble bounded native source-color/panel atlases, with pixel provenance.
No generated RGB, resized source details, or repeating UV swatches on gold housings.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,shutil,argparse
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);a=p.parse_args()
R=Path(__file__).resolve().parents[2];F=R/'art-source/gdi missile tower';O=F/f'pass-{a.iteration:02d}';C=O/'source-cuts';C.mkdir(exist_ok=True)
old=F/'pass-14'
for f in (old/'source-cuts').glob('*'):
 if f.is_file():shutil.copy2(f,C/f.name)
for name in ('base.png','registration.json','geometry.json','engine-projection.json'):shutil.copy2(old/name,O/name)
source=Image.open(next(F.glob('*.png'))).convert('RGBA');src=np.array(source)
# Restrict material colors to the actual turret, not the old paving donor.
coords=np.array([(x,y) for y in range(0,38) for x in range(23,103) if src[y,x,3] and int(src[y,x,0])>int(src[y,x,1])*1.12 and int(src[y,x,1])>int(src[y,x,2])*1.25 and src[y,x,0]<245])
colors=src[coords[:,1],coords[:,0],:3].astype(float)
plans=json.loads((O/'texture-plan.json').read_text());records=[]
# These are small, bounded armor marks, never complete machinery projected on a face.
donors=[(76,17,80,22),(73,24,76,28),(80,12,83,16),(69,30,72,33)]
for plan in plans:
 w,h=plan['size'];role=plan['role'];xy=np.zeros((h,w,2),dtype=np.uint8)
 target={'goldlight':np.array([198,155,73]),'gold':np.array([169,125,60]),'goldshade':np.array([123,89,49])}[role]
 for y in range(h):
  for x in range(w):
   # Broad stepped metal shading; original model normals still select the material.
   # Native source texels, deliberately not RGB noise or a black outline + flat fill.
   u=x/max(1,w-1);v=y/max(1,h-1)
   bevel=min(u,1-u)*8;shade=min(1.,bevel)*.12-.09 + (.5-v)*.10
   desired=target*(1+shade)
   index=np.argmin(((colors-desired)**2).sum(axis=1));xy[y,x]=coords[index]
 # One small native cut per broad panel, different cuts for the three tonal roles.
 # These are sparse service/wear marks, not a repeated pattern filling the surface.
 pasted=[]
 if w>=16 and h>=12 and any(s in plan['object'] for s in ('case','lid','roof','crossbrace','tracking','gearbox')):
  index={'gold':0,'goldlight':2,'goldshade':1}[role]
  rect=donors[index];x0,y0,x1,y1=rect;pw,ph=x1-x0,y1-y0
  dx=max(2,min(w-pw-2,w//3));dy=max(2,min(h-ph-2,h//3))
  for yy in range(ph):
   for xx in range(pw):xy[dy+yy,dx+xx]=[x0+xx,y0+yy]
  pasted.append({'source_rect':list(rect),'destination':[dx,dy]})
 # Place bounded native armor fragments on the actual broad polygon interiors.
 # UV regions are preserved from the mesh, so details cannot drift to a random
 # surface or wrap around the back of the housing.
 region_cuts=[(73,17,80,28),(77,21,83,31),(72,7,79,14),(84,17,91,24)]
 for ri,region in enumerate(plan.get('regions',[])):
  if len(region)<3:continue
  polygon=np.array(region);extent=polygon.max(axis=0)-polygon.min(axis=0)
  area=abs(np.dot(polygon[:,0],np.roll(polygon[:,1],1))-np.dot(polygon[:,1],np.roll(polygon[:,0],1)))/2
  if area<42 or min(extent)<5:continue
  valid=Image.new('1',(w,h));ImageDraw.Draw(valid).polygon([tuple(q) for q in region],fill=1);valid=np.array(valid)
  rect=region_cuts[ri%len(region_cuts)];x0,y0,x1,y1=rect;pw,ph=x1-x0,y1-y0
  mid=polygon.mean(axis=0);dx=int(round(mid[0]-pw/2));dy=int(round(mid[1]-ph/2))
  for yy in range(ph):
   for xx in range(pw):
    tx,ty=dx+xx,dy+yy;c=src[y0+yy,x0+xx]
    if 0<=tx<w and 0<=ty<h and valid[ty,tx] and c[3] and c[0]>int(c[1])*1.12 and c[1]>int(c[2])*1.25:
     xy[ty,tx]=[x0+xx,y0+yy]
  pasted.append({'source_rect':list(rect),'destination':[dx,dy],'mask':'actual face interior and native gold texels only'})
 # Rounded shell strips have narrow facets: use unique, bounded wear/tonal cuts
 # spanning their continuous cylindrical UVs instead of repeating a cut per facet.
 if 'rounded armored case' in plan['object']:
  for j,rect in enumerate(region_cuts):
   x0,y0,x1,y1=rect;dx=int((j+.5)*w/4);dy=7+(j%2)*6
   for yy in range(y1-y0):
    for xx in range(x1-x0):
     tx,ty=dx+xx,dy+yy;c=src[y0+yy,x0+xx]
     if tx<w and ty<h and c[3] and c[0]>int(c[1])*1.12 and c[1]>int(c[2])*1.25:xy[ty,tx]=[x0+xx,y0+yy]
   pasted.append({'source_rect':list(rect),'destination':[dx,dy],'mask':'native gold texels only'})
 image=src[xy[:,:,1],xy[:,:,0]];assert (image[:,:,3]==255).all()
 Image.fromarray(image).save(C/(plan['name']+'.png'));np.save(C/(plan['name']+'-source-xy.npy'),xy)
 records.append({**plan,'native_detail_cuts':pasted})
(O/'texture-provenance.json').write_text(json.dumps({'source':str(next(F.glob('*.png')).relative_to(R)),'policy':'Every atlas pixel copied from original turret. Source-color tonal bands plus bounded native detail cuts. Gold UVs clamped, never tiled. Inferred rear panels are not recovered source geometry.','panels':records},indent=2)+'\n')
print('Native panel atlases:',len(records))
