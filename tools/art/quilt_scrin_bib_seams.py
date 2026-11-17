"""Binary, native-pixel source-piece joins; no interpolation or painted pixels."""
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import maximum_flow, breadth_first_order
from scipy.ndimage import binary_dilation,binary_erosion

def join(a,b,prefer_b,band):
 h,w=a.shape[:2];size=h*w;source=size;sink=size+1;ids=np.arange(size).reshape(h,w)
 # Cut where both pieces agree, preferably in dark material joints, rather than
 # through bright conduits. The output still selects one entire source pixel.
 af=a[:,:,:3].astype(float);bf=b[:,:,:3].astype(float)
 error=np.abs(af-bf).sum(2)+.3*np.minimum(af.max(2),bf.max(2))+4
 edges=[];caps=[]
 for pa,pb,ea,eb in [(ids[:,:-1],ids[:,1:],error[:,:-1],error[:,1:]),(ids[:-1],ids[1:],error[:-1],error[1:])]:
  cost=np.maximum(1,((ea+eb)*2).astype(np.int64)).ravel();edges.extend([(pa.ravel(),pb.ravel()),(pb.ravel(),pa.ravel())]);caps.extend([cost,cost])
 invalid=(b[:,:,3]==0)&(a[:,:,3]>0);lock_a=(~band&~prefer_b)|invalid;lock_b=(~band&prefer_b)&~invalid
 for mask,left in [(lock_a,True),(lock_b,False)]:
  p=ids[mask];term=np.full(len(p),source if left else sink);edges.append((term,p) if left else (p,term));caps.append(np.full(len(p),10000000,dtype=np.int64))
 rows=np.concatenate([e[0] for e in edges]);cols=np.concatenate([e[1] for e in edges]);graph=coo_matrix((np.concatenate(caps),(rows,cols)),shape=(size+2,size+2)).tocsr()
 flow=maximum_flow(graph,source,sink);residual=graph-flow.flow;residual.data=(residual.data>0).astype(np.int64);residual.eliminate_zeros()
 reachable=breadth_first_order(residual,source,directed=True,return_predecessors=False);is_a=np.zeros(size+2,bool);is_a[reachable]=True;choose_b=~is_a[:size].reshape(h,w)
 out=a.copy();out[choose_b]=b[choose_b];return out,choose_b

def refine_master(a,x,y,base,sample):
 result=base.copy()
 for left in (True,False):
  sx=x+128 if left else x-128;boundary=0 if left else 256;prefer=x<boundary if left else x>=boundary
  back=18+abs(x-128)//2;front=210-abs(x-128)//2
  side=sample(a,sx,y)
  rear=sample(a,sx,y-64);m=(y<=back+18)&(rear[:,:,3]>0);side[m]=rear[m]
  lip=sample(a,sx,y+64);m=(y>=np.maximum(back,front-12))&(lip[:,:,3]>0);side[m]=lip[m]
  original=sample(a,x,y);band=(abs(x-boundary)<=16)&(base[:,:,3]>0)&(original[:,:,3]>0)&(side[:,:,3]>0)
  stitched,_=join(original,side,prefer,band);result[band]=stitched[band]
 # Keep the already-reviewed contour, including every fine exterior filament.
 result[:,:,3]=base[:,:,3];result[base[:,:,3]==0]=0
 return result

def bridge_left_joint(image,source,surface_y):
 # A whole source plate bridges the remaining tube/plate junction. Include its
 # rim and attached feeler instead of cutting a conduit halfway along its run.
 from PIL import Image,ImageDraw
 mask=Image.new('L',(256,256));d=ImageDraw.Draw(mask)
 d.polygon([(19,71),(73,42),(101,65),(101,77),(106,84),(107,93),(104,98),(97,102),(86,102),(68,97),(34,81)],fill=255)
 d.polygon([(37,38),(42,38),(45,47),(54,49),(57,54),(51,61),(44,61),(40,54),(39,46)],fill=255)
 a=np.array(image);original_alpha=a[:,:,3].copy();m=np.array(mask)>0;yy,xx=np.where(m&(source[:,:,3]>0));dx=xx+24;dy=yy+surface_y-61
 valid=(dx>=0)&(dx<a.shape[1])&(dy>=0)&(dy<a.shape[0]);dx=dx[valid];dy=dy[valid];xx=xx[valid];yy=yy[valid]
 solid=original_alpha[dy,dx]>0;a[dy[solid],dx[solid]]=source[yy[solid],xx[solid]];a[:,:,3]=original_alpha
 return Image.fromarray(a)

def refine_quadrants(candidates,labels,baseline):
 result=baseline.copy();current=labels.copy();visible=baseline[:,:,3]>0
 for _ in range(2):
  for i,b in enumerate(candidates):
   selected=current==i
   band=(binary_dilation(selected,iterations=12)&~binary_erosion(selected,iterations=12,border_value=1))&visible&(b[:,:,3]>0)
   stitched,chosen=join(result,b,selected,band)
   result[band]=stitched[band];current[band&chosen]=i
 result[:,:,3]=baseline[:,:,3];result[~visible]=0
 return result
