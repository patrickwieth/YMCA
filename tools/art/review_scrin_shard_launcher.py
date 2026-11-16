"""Native source-palette/part-edge finishing and honest composed study review."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,argparse
p=argparse.ArgumentParser();p.add_argument('stage',type=int);a=p.parse_args();ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';D=ART/f'pass-{a.stage:02d}';OUT=D/'review';OUT.mkdir(exist_ok=True)
if a.stage>=43 and (D/'native-full-31-raw.png').exists():
 from assemble_scrin_shard_native_parts import assemble
 assemble(D)
colors=np.array(json.loads((ART/'reference-review/palette.json').read_text()),dtype=np.int32)
def nearest(c):return tuple(colors[np.argmin(((colors-np.array(c))**2).sum(axis=1))])
ink=nearest((12,13,22));blue=nearest((10,72,100))
material_report=json.loads((D/'material-report.json').read_text()) if a.stage>=34 else None
if material_report:
 source=np.array(Image.open(ART/'shard_launcher-332414650-0.png').convert('RGBA'));source_colors=np.unique(source[source[:,:,3]>250,:3],axis=0).astype(np.int32)
 cyan_colors=source_colors[(source_colors[:,1]-source_colors[:,0]>18)&(source_colors[:,2]>80)&(source_colors[:,1]+source_colors[:,2]-2*source_colors[:,0]>100)]
 vein_colors=np.array(material_report['reference']['ramps']['vein'],dtype=np.int32)
def connected_vein_crest(code,mask):
 # Topology-preserving thinning per visible vessel, not isolated distance maxima.
 crest=np.zeros(mask.shape,dtype=bool)
 for identity in np.unique(code[mask]):
  yy,xx=np.where(mask&(code==identity))
  if len(xx)<5:continue
  y0,y1=yy.min(),yy.max()+1;x0,x1=xx.min(),xx.max()+1
  im=np.pad(mask[y0:y1,x0:x1]&(code[y0:y1,x0:x1]==identity),1)
  for _ in range(40):
   changed=False
   for phase in (0,1):
    p=[np.roll(im,shift,(0,1)) for shift in [(1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1),(0,1),(1,1)]]
    count=sum(q.astype(int) for q in p);transitions=sum((~p[j]&p[(j+1)%8]).astype(int) for j in range(8))
    a,b=(p[0]&p[2]&p[4],p[2]&p[4]&p[6]) if phase==0 else (p[0]&p[2]&p[6],p[0]&p[4]&p[6])
    remove=im&(count>=2)&(count<=6)&(transitions==1)&~a&~b
    if np.any(remove):changed=True;im[remove]=False
   if not changed:break
  small=im[1:-1,1:-1];pending=set(map(tuple,np.argwhere(small)));keep=np.zeros(small.shape,dtype=bool)
  while pending:
   seed=pending.pop();component=[seed];stack=[seed]
   while stack:
    y,x=stack.pop()
    for dy,dx in [(i,j) for i in (-1,0,1) for j in (-1,0,1) if i or j]:
     q=(y+dy,x+dx)
     if q in pending:pending.remove(q);component.append(q);stack.append(q)
   if len(component)>=5:
    for y,x in component:keep[y,x]=True
  crest[y0:y1,x0:x1]|=keep
 return crest

def finish(path):
 im=Image.open(path).convert('RGBA');arr=np.array(im);ids=np.array(Image.open(path.with_name(path.name.replace('-raw','-ids'))).convert('RGBA')) if path.with_name(path.name.replace('-raw','-ids')).exists() else None
 alpha=arr[:,:,3]>127;raised_vein=np.zeros(alpha.shape,dtype=bool)
 if material_report and ids is not None:
  code=ids[:,:,0].astype(int)+256*ids[:,:,1].astype(int)
  veins=np.isin(code,[int(k) for k,v in material_report['part_roles'].items() if v=='vein'])&alpha
  neighbors=sum((np.roll(code,(dy,dx),(0,1))==code).astype(int) for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)])
  # Material-only native pixel finishing: dark tube margins and a fine raised crest.
  # Do not erode alpha, thicken geometry, paint across occlusion or add random marks.
  treatments=[(veins&(neighbors==3),.62),(veins&(neighbors==4),1.28)]
  if a.stage>=37:
   live=veins.copy();distance=np.zeros(alpha.shape,dtype=np.int32)
   for _ in range(8):
    distance[live]+=1;next_live=live.copy()
    for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:next_live&=np.roll(live,(dy,dx),(0,1))&(np.roll(code,(dy,dx),(0,1))==code)
    live=next_live
   raised_vein=veins&(((distance>np.roll(distance,1,1))&(distance>=np.roll(distance,-1,1)))|((distance>np.roll(distance,1,0))&(distance>=np.roll(distance,-1,0))))
   if a.stage>=38:raised_vein=connected_vein_crest(code,veins)
   treatments=[(veins&~raised_vein&(distance==1),.40),(veins&~raised_vein&(distance>1),.78),(raised_vein,1.7 if a.stage>=38 else 1.55)]
  for mask,factor in treatments:
   values=arr[mask,:3].astype(float)*factor
   if len(values):arr[mask,:3]=vein_colors[np.argmin(((values[:,None,:]-vein_colors[None,:,:])**2).sum(2),axis=1)]
  rgb=arr[:,:,:3].astype(int);crystal=alpha&(rgb[:,:,1]-rgb[:,:,0]>18)&(rgb[:,:,2]>80)&(rgb[:,:,1]+rgb[:,:,2]-2*rgb[:,:,0]>100)
  values=rgb[crystal]
  if len(values):arr[crystal,:3]=cyan_colors[np.argmin(((values[:,None,:]-cyan_colors[None,:,:])**2).sum(2),axis=1)]
 border=np.zeros(alpha.shape,dtype=bool);edge=np.zeros(alpha.shape,dtype=bool)
 for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
  inside=np.roll(alpha,(dy,dx),(0,1));border|=alpha&~inside
  if ids is not None:edge|=alpha&inside&np.any(ids[:,:,:2]!=np.roll(ids[:,:,:2],(dy,dx),(0,1)),axis=2)
 # Preserve thin luminous detail: cool border instead of black on the crystal facets.
 bright=(arr[:,:,1].astype(int)>arr[:,:,0].astype(int)+45)&(arr[:,:,2]>90)
 arr[border&~bright,:3]=ink;arr[border&bright,:3]=blue
 # Draw only one side of object contacts, leaving 2-3px veins legible.
 if ids is not None:
  lower=np.roll(ids[:,:,0],1,0)<ids[:,:,0]
  for y,x in zip(*np.where(edge&lower&~border&~bright&~raised_vein)):
   arr[y,x,:3]=nearest(np.array(arr[y,x,:3],dtype=float)*.64)
 arr[:,:,3]=alpha.astype('uint8')*255;result=Image.fromarray(arr);result.save(OUT/path.name.replace('-raw',''));return result
for f in sorted(D.glob('*-raw.png')):finish(f)
size=json.loads((D/'geometry.json').read_text())['frame_size']
ref=Image.new('RGBA',(size,size));src=Image.open(ART/'shard_launcher-332414650-0.png').convert('RGBA');ref.alpha_composite(src,((size-256)//2,280-184 if a.stage>=25 else (size-256)//2))
views=[ref,Image.open(OUT/'reference.png')]+[Image.open(OUT/f'assembly-{i:02d}.png') for i in (8,16,24)]
board=Image.new('RGBA',(size*5,size+28),(77,80,78,255));draw=ImageDraw.Draw(board)
for i,(label,im) in enumerate(zip(['SUPPLIED SOURCE / ground aligned' if a.stage>=25 else 'SUPPLIED SOURCE',f'PASS {a.stage:02d} / source view','SIDE 90','REAR 180','SIDE 270'],views)):
 board.alpha_composite(im,(size*i,28));draw.text((size*i+8,8),label,fill='white')
board.save(OUT/'comparison.png');board.resize((size*5*2,(size+28)*2),Image.Resampling.NEAREST).save(OUT/'comparison-2x.png')
if (OUT/'turret-00.png').exists():
 base=Image.open(OUT/'base.png');turn=Image.new('RGBA',(size*8,size*4),(77,80,78,255))
 for i in range(32):
  frame=Image.alpha_composite(base,Image.open(OUT/f'turret-{i:02d}.png'));turn.alpha_composite(frame,(i%8*size,i//8*size))
 turn.save(OUT/'all32.png')
print('Reviewed study outputs:',OUT)
