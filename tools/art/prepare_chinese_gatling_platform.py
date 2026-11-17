"""Native cut/paste platform from supplied art. Source and installed turret untouched."""
from pathlib import Path
from collections import deque
from PIL import Image,ImageDraw
import numpy as np,json,hashlib,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';SOURCE=ART/'mount_base_flat_plat...-1843695069-0.png';stage=int(sys.argv[1]) if len(sys.argv)>1 else 3;OUT=ART/f'platform-cut/pass-{stage:02d}';OUT.mkdir(parents=True,exist_ok=True)
a=np.array(Image.open(SOURCE).convert('RGBA'));original=a.copy();h,w=a.shape[:2];bg=a[0,0,:3];seen=np.zeros((h,w),bool);q=deque()
for y in range(h):q.extend([(y,0),(y,w-1)])
for x in range(w):q.extend([(0,x),(h-1,x)])
while q:
 y,x=q.popleft()
 if not (0<=x<w and 0<=y<h) or seen[y,x] or not np.array_equal(a[y,x,:3],bg):continue
 seen[y,x]=True;a[y,x,3]=0;q.extend([(y-1,x),(y+1,x),(y,x-1),(y,x+1)])
frame=np.zeros((112,128,4),dtype=np.uint8);yy,xx=np.indices(frame.shape[:2])
def polygon(points):
 im=Image.new('L',(128,112));ImageDraw.Draw(im).polygon(points,fill=255);return np.array(im)>0
def paste(sx,sy,mask):
 ok=mask&(sx>=0)&(sx<128)&(sy>=0)&(sy<128);p=a[np.clip(sy,0,127),np.clip(sx,0,127)];ok&=p[:,:,3]>0;frame[ok]=p[ok]
# Shorten the foundation by removing an interior wall strip. No squashing/resampling.
wall=polygon([(0,54),(44,76),(84,76),(127,54),(127,65),(84,88),(44,88),(0,65)])
paste(xx,yy+19,wall) # Mid-wall cuts avoid the grass visible at the original foot.
# Replace the source's cropped staircase/ground clutter with bounded native wall panels.
sx=32+xx%12;sy=np.rint(yy+18+(sx-xx)/2).astype(int)
paste(sx,sy,wall&(xx<32))
# Move the rear and front aprons toward the intact central mounting ring.
deck=polygon([(0,35),(40,13),(88,13),(127,35),(127,57),(88,78),(40,78),(0,57)])
edge=np.maximum(np.maximum(40-xx,xx-88),0);rear=13+.55*edge;front=78-.55*edge
sy=np.where(yy<rear+11,yy-6,np.where(yy>front-11,yy+6,yy));paste(xx,sy,deck)
# Reattach complete hazard-strip runs, including the formerly cropped side facets.
strip=Image.fromarray(a).crop((44,82,66,86))
for x,rotation in [(0,Image.Transpose.ROTATE_90),(124,Image.Transpose.ROTATE_270)]:
 p=np.array(strip.transpose(rotation));frame[35:57,x:x+4]=p
# Preserve the complete machined bearing and central mount, not a cut through its middle.
ring=polygon([(63,18),(80,20),(97,27),(105,36),(107,48),(99,59),(82,68),(63,71),(44,67),(29,60),(22,49),(23,37),(33,27),(47,21)])
paste(xx,yy,ring)
# Reattach upper ladder rungs; the old lowest openings showed grass behind them.
paste(xx,yy+7,(xx>=61)&(xx<=70)&(yy>=79)&(yy<=88)&wall)
im=Image.fromarray(frame);im.save(OUT/'platform.png')
scale=1.;mount=604;body_offset=56-39-mount*64/1448
meta={'source':SOURCE.name,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'frame_size':[128,112],'scale':1,'body_offset':[0,body_offset],'mount_pixel':[64,39],'turret_world_mount':[0,0,mount],'method':'Unscaled native pixel cuts; rear/front apron translations; shortened walls; preserved central bearing; removed background/ground/stair clutter','runtime_installed':False}
(OUT/'binding.json').write_text(json.dumps(meta,indent=2)+'\n')
board=Image.new('RGBA',(512,224),(65,70,63,255));board.alpha_composite(Image.fromarray(original),(32,40));board.alpha_composite(im,(236,40));d=ImageDraw.Draw(board);d.text((12,12),'SOURCE / untouched',fill='white');d.text((220,12),'NATIVE CUT / 128px',fill='white');board.resize((1536,672),Image.Resampling.NEAREST).save(OUT/'cut-review-3x.png')
atlas=Image.open(ROOT/'mods/ca/bits/china/buildings/gatling-turret32.png').convert('RGBA');bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA');sheet=Image.new('RGBA',(1792,896),(65,70,63,255));frames=[]
for i in range(32):
 tile=Image.new('RGBA',(224,224),(65,70,63,255));tile.alpha_composite(bib,(48,143));tile.alpha_composite(im,(48,round(175-56+body_offset)))
 turret=atlas.crop((i%8*320,i//8*320,i%8*320+320,i//8*320+320)).resize((208,208),Image.Resampling.NEAREST)
 tile.alpha_composite(turret,(8,round(175-160*.65-34*.65-mount*64/1448)));sheet.alpha_composite(tile,(i%8*224,i//8*224));frames.append(tile.convert('RGB'))
sheet.save(OUT/'all32-native.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
