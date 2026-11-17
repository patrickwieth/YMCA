"""Bounded source palette/crystal cuts for material-only Shard studies. Never changes source."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';OUT=ART/'material-reference';REV=int(sys.argv[1]) if len(sys.argv)>1 else 32
if REV>=33:OUT=OUT/f'source{REV}'
OUT.mkdir(parents=True,exist_ok=True)
src=ART/'shard_launcher-332414650-0.png';a=np.array(Image.open(src).convert('RGBA'));rgb=a[:,:,:3].astype(int);colors=np.unique(rgb[a[:,:,3]>250],axis=0)
ramps={
 'shell':[(12,13,24),(22,23,36),(33,36,53),(46,51,71),(59,66,88),(74,83,106),(91,103,129),(112,128,151),(138,154,178)],
 'vein':[(20,13,26),(31,19,35),(44,24,43),(59,30,52),(78,40,65),(98,52,81),(121,68,103),(145,87,122),(172,113,146)],
 'cyan':[(11,40,72),(15,62,107),(22,92,144),(25,123,176),(30,164,204),(41,202,227),(89,234,239),(146,249,245),(209,255,247)]}
if REV>=33:ramps['shell']=[(12,11,23),(20,19,34),(31,28,43),(43,37,55),(54,49,68),(67,64,85),(80,82,105),(100,111,136),(123,143,166)]
for role,tones in ramps.items():
 pool=colors[(colors[:,1]>=colors[:,0]-(14 if REV>=33 else 4))&(colors[:,2]>colors[:,1])] if role=='shell' else colors[(colors[:,1]-colors[:,0]>18)&(colors[:,2]>80)&((colors[:,1]+colors[:,2]-2*colors[:,0]>100) if REV>=34 else True)] if role=='cyan' else colors[(colors[:,0]>colors[:,1]+8)&(colors[:,2]>colors[:,1]+8)]
 ramps[role]=[pool[np.argmin(((pool-np.array(t))**2).sum(axis=1))].tolist() for t in tones]
# Axial rectification at one source pixel per texel; only cyan source texels are admitted.
entries=[('spear',(111,87),(215,27),24,(108,23,218,92)),('battery0',(160,155),(156,126),16,(153,126,167,157)),('battery1',(173,147),(171,117),16,(166,117,180,149)),('battery2',(185,139),(186,110),14,(179,110,191,141)),('battery3',(196,130),(197,104),14,(190,104,202,132))];report=[]
for name,start,end,w,bounds in entries:
 start=np.array(start,dtype=float);end=np.array(end,dtype=float);axis=end-start;length=np.linalg.norm(axis);direction=axis/length;side=np.array([-direction[1],direction[0]]);h=round(length)+1;image=np.zeros((h,w,4),dtype=np.uint8);x0,y0,x1,y1=bounds
 def sample(p):
  x,y=np.round(p).astype(int)
  if not(x0<=x<x1 and y0<=y<y1):return None
  r,g,b,alpha=map(int,a[y,x]);return (r,g,b,255) if alpha>250 and g-r>18 and b>80 and (REV<34 or g+b-2*r>100) else None
 # Missing source silhouette/background is filled ONLY from the same bounded crystal,
 # never from adjoining armor. These hidden/outside faces are explicitly inferred.
 for y in range(h):
  center=start+direction*y;row=[sample(center+side*(x-(w-1)/2)) for x in range(w)]
  valid=[x for x,c in enumerate(row) if c is not None]
  for x,c in enumerate(row):
   if c is None and valid:c=row[min(valid,key=lambda v:abs(v-x))]
   if c is None:c=tuple(ramps['cyan'][4])+ (255,)
   image[h-1-y,x]=c
 Image.fromarray(image).save(OUT/(name+'.png'));report.append({'name':name,'source_bounds':bounds,'axis_start':start.tolist(),'axis_end':end.tolist(),'atlas_size':[w,h],'inferred':'non-visible cross sections use nearest same-crystal row color; no foreign source details'})
# Corresponding front shin only; exclude neighbouring conduits, soil and transparency.
polygon=[(127,160),(133,159),(139,173),(135,187),(141,215),(132,223),(121,212),(125,188),(121,175)]
mask=Image.new('1',(256,256));ImageDraw.Draw(mask).polygon(polygon,fill=1);mask=np.array(mask)&(a[:,:,3]>250)&(rgb[:,:,1]>=rgb[:,:,0]-7)&(rgb[:,:,2]>=rgb[:,:,1])
shin=np.zeros((65,25,4),dtype=np.uint8)
for y in range(65):
 valid=[x for x in range(25) if mask[y+159,x+119]]
 for x in range(25):
  if x in valid:color=tuple(a[y+159,x+119])
  elif valid:color=tuple(a[y+159,119+min(valid,key=lambda v:abs(v-x))])
  else:color=tuple(ramps['shell'][3])+(255,)
  shin[y,x]=color
Image.fromarray(shin).save(OUT/'front-shin.png')
if REV>=33:
 # One bounded sample for each corresponding upper carapace plate, not a tiled stamp.
 metal=[]
 for x,y in [(113,54),(129,43),(143,33),(156,25),(169,20)]:
  patch=[]
  for yy in range(y,y+22):
   row=[]
   for xx in range(x,x+12):
    r,g,b,alpha=map(int,a[yy,xx]);row.append([r,g,b] if alpha>250 and -14<=g-r<=12 and b>=g else None)
   patch.append(row)
  metal.append({'bounds':[x,y,x+12,y+22],'pixels':patch})
 (OUT/'armor-cuts.json').write_text(json.dumps(metal,indent=2)+'\n')
if REV>=34:
 gear=np.zeros((18,18,4),dtype=np.uint8)
 for y in range(18):
  for x in range(18):
   r,g,b,alpha=map(int,a[y+94,x+81]);color=(r,g,b,255) if alpha>250 and r-g<22 and g+b-2*r<105 else tuple(ramps['shell'][1])+(255,)
   gear[y,x]=color
 Image.fromarray(gear).save(OUT/'cheek-gear.png')
if REV>=37:
 # Source-colored crystalline planes and deliberately traced branching marks. These
 # replace the smeared cross-sectional photograph on the inferred hidden facets.
 face_base=[(44,135,206),(56,83,166),(28,72,106),(41,175,193),(45,201,204)]
 face_light=[(67,216,213),(44,135,206),(54,101,193),(78,241,227),(67,216,213) if REV>=38 else (152,250,251)]
 for entry in report:
  h=entry['atlas_size'][1];fw=10 if entry['name']=='spear' else 5;im=Image.new('RGBA',(fw*5,h));draw=ImageDraw.Draw(im)
  for face in range(5):
   x0=face*fw;draw.rectangle((x0,0,x0+fw-1,h-1),fill=face_base[face]+(255,))
   def pt(u,t):return (x0+round(u*(fw-1)),round((1-t)*(h-1)))
   draw.polygon([pt(.0,.04),pt(.70,.24),pt(.30,.47),pt(.90,.67),pt(.55,.98),pt(.0,1)],fill=face_light[face]+(255,))
   draw.polygon([pt(.85,.04),pt(.30,.33),pt(.90,.53),pt(.48,.77),pt(1,.96),pt(1,0)],fill=face_base[(face+1)%5]+(255,))
   spine=[pt(.38,.02),pt(.58,.19),pt(.34,.34),pt(.66,.49),pt(.30,.64),pt(.60,.81),pt(.42,.99)]
   color=(152,250,251,255) if face in (0,4) else face_light[face]+(255,)
   draw.line(spine,fill=color,width=2 if REV<38 and entry['name']=='spear' and face==4 else 1)
   for t,side in [( .19,.05),(.49,.96),(.81,.0)]:draw.line([pt(.56,t),pt(side,t+.09)],fill=color,width=1)
  im.save(OUT/(entry['name']+'-facets.png'))
(OUT/'reference.json').write_text(json.dumps({'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'ramps':ramps,'cuts':report},indent=2)+'\n')
board=Image.new('RGB',(600,240),(45,47,50));d=ImageDraw.Draw(board)
for j,role in enumerate(ramps):
 d.text((8,j*35+8),role,fill='white')
 for i,color in enumerate(ramps[role]):d.rectangle((70+i*38,j*35,105+i*38,j*35+30),fill=tuple(color))
for i,item in enumerate(report):
 im=Image.open(OUT/(item['name']+'.png'));board.paste(im,(i*110+8,110));d.text((i*110+8,220),item['name'],fill='white')
board.save(OUT/'material-cuts-review.png');print(OUT)
