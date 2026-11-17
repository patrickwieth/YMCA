"""Small hand-placed tapered vein terminals; preserved original is the only input."""
from pathlib import Path
from PIL import Image,ImageDraw
import math,json,hashlib
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/scrin-headquarter';src=OUT/'headquarter-original.png';im=Image.open(src).convert('RGBA');before=im.copy()
# Root positions follow the actual truncated strokes, not the building silhouette.
tips=[((20,211),(-1,.08),6,1.4),((48,234),(-.45,1),6,1),((53,234),(-.55,1),6,1.7),((61,234),(-.1,1),6,1),((196,239),(1,.13),6,1.5),((174,239),(-.45,1),4,1),((178,239),(.8,.5),5,1),((152,248),(-1,.4),4,1),((243,199),(1,.2),4,1)]
d=ImageDraw.Draw(im);dark=(45,31,47,255);mid=(90,52,74,255);light=(116,59,84,255)
for (x,y),(dx,dy),length,r in tips:
 norm=math.hypot(dx,dy);dx/=norm;dy/=norm;nx,ny=-dy,dx;end=(round(x+dx*length),round(y+dy*length))
 p=lambda a,b:(round(x+dx*a+nx*b),round(y+dy*a+ny*b))
 d.polygon([p(-1,-r),p(-1,r),p(length*.45,r*.6),end,p(length*.45,-r*.6)],fill=dark)
 d.polygon([p(-1,-r*.45),p(-1,r*.55),p(length*.55,r*.20),p(length-.7,0),p(length*.4,-r*.25)],fill=mid)
 d.line([p(-1,-r*.25),p(length*.45,-r*.18)],fill=light,width=1)
im.save(OUT/'headquarter-finished.png')
board=Image.new('RGBA',(768,768),(70,76,68,255));board.alpha_composite(before.resize((768,768),Image.Resampling.NEAREST));after=Image.new('RGBA',(1536,768),(70,76,68,255));after.alpha_composite(board);after.alpha_composite(im.resize((768,768),Image.Resampling.NEAREST),(768,0));after.save(OUT/'before-after-3x.png')
changed=sum(a!=b for a,b in zip(before.getdata(),im.getdata()));(OUT/'finish.json').write_text(json.dumps({'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'finished_sha256':hashlib.sha256((OUT/'headquarter-finished.png').read_bytes()).hexdigest(),'changed_pixels':changed,'terminals':tips},indent=2)+'\n');print('Changed pixels:',changed)
