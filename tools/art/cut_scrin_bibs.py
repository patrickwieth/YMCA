"""Native-pixel cut/paste Scrin bib studies. Never resize or alter the supplied PNG."""
from pathlib import Path
import hashlib,json
from collections import deque
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/scrin bib'
SOURCE=OUT/'ground_plating-1534399694-0.png'

def sample(a,x,y):
 valid=(x>=0)&(x<a.shape[1])&(y>=0)&(y<a.shape[0]);p=a[np.clip(y,0,a.shape[0]-1),np.clip(x,0,a.shape[1]-1)].copy();p[~valid]=0;return p

def master_sample(a,x,y):
 result=sample(a,x,y)
 # The supplied patch crops the side corners of its native 3x3 floor.
 # Reconstruct those with translated interior, rear-edge and front-lip cuts.
 for left in (True,False):
  region=x<0 if left else x>=256
  sx=x+128 if left else x-128
  back=18+abs(x-128)//2
  front=210-abs(x-128)//2
  interior=sample(a,sx,y);inside=region&(y>=back)&(y<=front)
  result[inside]=interior[inside]
  rear=sample(a,sx,y-64);mask=region&(y>=back-24)&(y<=back+18)&(rear[:,:,3]>0)
  result[mask]=rear[mask]
  lip=sample(a,sx,y+64);mask=region&(y>=np.maximum(back,front-12))&(y<=front+28)&(lip[:,:,3]>0)
  result[mask]=lip[mask]
 result[(x < -64)|(x >= 320)]=0
 return result

def remove_cut_fragments(rgba):
 # A diagonal cut can leave an unattached antenna tip. Retain the main connected
 # plating, including one-pixel diagonal filaments; remove only detached scraps.
 mask=rgba[:,:,3]>0;seen=np.zeros(mask.shape,bool);components=[];h,w=mask.shape
 for y,x in zip(*np.where(mask)):
  if seen[y,x]:continue
  q=deque([(y,x)]);seen[y,x]=True;component=[]
  while q:
   yy,xx=q.popleft();component.append((yy,xx))
   for dy,dx in ((-1,-1),(-1,0),(-1,1),(0,-1),(0,1),(1,-1),(1,0),(1,1)):
    ny,nx=yy+dy,xx+dx
    if 0<=ny<h and 0<=nx<w and mask[ny,nx] and not seen[ny,nx]:seen[ny,nx]=True;q.append((ny,nx))
  components.append(component)
 components.sort(key=len,reverse=True);removed=0
 for fragment in components[1:]:
  removed+=len(fragment)
  for y,x in fragment:rgba[y,x]=0
 return removed

def main():
 from quilt_scrin_bib_seams import refine_master,refine_quadrants,bridge_left_joint
 a=np.array(Image.open(SOURCE).convert('RGBA'));records=[];images=[]
 my,mx=np.indices((272,432));mx-=88;my-=20
 master=refine_master(a,mx,my,master_sample(a,mx,my),sample)
 for n in (1,2,3):
  width=128*n+48;height=64*n+80;origin=(width//2,40+32*n)
  yy,xx=np.indices((height,width));x=xx-origin[0];y=yy-origin[1]
  u=x+2*y;v=-x+2*y;shift=64*(3-n)
  du=np.where(u<0,-shift,shift);dv=np.where(v<0,-shift,shift)
  sx=128+x+(du-dv)//2;sy=116+y+(du+dv)//4
  rgba=sample(master,sx+88,sy+20)
  if n<3:
   candidates=[]
   for su,sv in [(-shift,-shift),(-shift,shift),(shift,-shift),(shift,shift)]:
    candidates.append(sample(master,128+x+(su-sv)//2+88,116+y+(su+sv)//4+20))
   labels=(u>=0).astype(int)*2+(v>=0).astype(int)
   rgba=refine_quadrants(candidates,labels,rgba)
  removed=remove_cut_fragments(rgba)
  image=Image.fromarray(rgba);box=image.getbbox();image=image.crop(box)
  surface=(origin[0]-box[0],origin[1]-box[1]);origin=(surface[0],surface[1]+24)
  if n>=2:image=bridge_left_joint(image,a,surface[1])
  image.save(OUT/f'bib{n}x{n}.png');images.append((n,image,origin))
  records.append({'name':f'bib{n}x{n}.png','cells':[n,n],'tile_size':[128,64],'canvas':list(image.size),'top_surface_center':list(surface),'ground_center':list(origin),'inferred_plating_height':24,'ground_diamond':[[origin[0],origin[1]-32*n],[origin[0]+64*n,origin[1]],[origin[0],origin[1]+32*n],[origin[0]-64*n,origin[1]]],'bottom_anchor_offset':[0,image.height-image.width/4-origin[1]],'detached_cut_pixels_removed':removed,'native_pixels':True})
 sheet=Image.new('RGBA',(1000,440),(64,68,62,255));d=ImageDraw.Draw(sheet)
 for (n,im,origin),cx in zip(images,[120,400,770]):
  sheet.alpha_composite(im,(cx-origin[0],190-origin[1]));d.text((cx-35,25),f'{n}x{n} / native',fill='white')
 sheet.save(OUT/'bib-contact-sheet.png');sheet.resize((2000,880),Image.Resampling.NEAREST).save(OUT/'bib-contact-sheet-2x.png')
 (OUT/'cut-paste-manifest.json').write_text(json.dumps({'source':SOURCE.name,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'method':'Integer translations only; binary overlapping-piece seams follow matching dark joints instead of straight cross-detail cuts. No blur or interpolation. Reconstruct clipped side corners from native interior/back/front cuts; remove central diagonal strips for smaller footprints. Alpha-cropped PNGs retain original raised lip and organic edge details. The24px foundation height/ground center is inferred from the front lip, not engine-validated. No runtime integration.','outputs':records},indent=2)+'\n')
if __name__=='__main__':main()
