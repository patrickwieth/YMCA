"""Reviewed ground-contact rear alignment; towers/antennas are not ground contacts."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,math
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/scrin-rear-alignment'
CONTACTS={'sfac':[(22,191),(238,193)],'reac':[(26,195),(231,193)],'rea2':[(21,192),(236,195)],'proc.scrin':[(27,194),(232,196)],'port':[(37,208),(226,211)],'wsph':[(38,188),(229,205)],'nerv':[(12,204),(243,202)],'scrt':[(9,217),(238,212)],'grav':[(38,217),(222,218)],'sign':[(24,204),(233,207)],'srep':[(18,197),(238,194)],'mani':[(22,214),(232,215)],'rift':[(36,202),(225,198)]}
entries=json.loads((OUT/'before.json').read_text());records=[]
for e in entries:
 name=e['Actor']
 if name not in CONTACTS:continue
 im=Image.open(ROOT/'mods/ca'/e['File']).convert('RGBA');a=np.array(im);s=float(e['Scale']);ox,oy=map(float,e['Offset'].split(',')[:2]);origin=np.array([ox-im.width/2,oy-im.height+im.width/4]);n=int(e['Bibs'][0]['Image'][-3]);points=[]
 for x,y in CONTACTS[name]:
  ys,xs=np.where(a[max(0,y-10):y+11,max(0,x-10):x+11,3]>100);assert len(xs),name
  xs+=max(0,x-10);ys+=max(0,y-10);k=np.argmin((xs-x)**2+(ys-y)**2);points.append([int(xs[k]),int(ys[k])])
 c=(np.array(points)+origin)*s;rear=min((c[:,0]+2*c[:,1]).min(),(-c[:,0]+2*c[:,1]).min());dy=math.ceil((-64*n+2-rear)/2)
 y,x=np.where(a[:,:,3]>100);front=float(np.max((y+origin[1])*s+.5*np.abs((x+origin[0])*s)))+dy
 # Grow only forward from the SAME rear grid corner if front roots cannot fit.
 size=n
 while front>32*n+64*(size-n)+2:size+=1
 assert size<=4,(name,size)
 records.append({'actor':e['RuleKey'],'screen_delta':[0,dy],'contacts':points,'old_bib_cells':n,'new_bib_cells':size,'bib_down_pixels':32*(size-n),'front_extent':front,'rear_u_before':float(rear),'rear_u_after':float(rear+2*dy)})
(OUT/'plan.json').write_text(json.dumps(records,indent=2)+'\n')
print([(e['actor'],e['screen_delta'],e['old_bib_cells'],e['new_bib_cells']) for e in records])
