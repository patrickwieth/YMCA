"""Compare four supplied viewpoints with the actual native model; preserve source files."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,sys,ast
from collections import deque
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';stage=int(sys.argv[1]);D=ART/f'pass-{stage:02d}';OUT=D/'review';OUT.mkdir(exist_ok=True);colors=np.array(json.loads((ART/('reference-review/palette-expanded.json' if stage>=3 else 'reference-review/palette.json')).read_text()),dtype=np.int32)
source=ast.parse((Path(__file__).parent/'review_scrin_shard_launcher.py').read_text());functions=[n for n in source.body if isinstance(n,ast.FunctionDef)];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native finishing>','exec'),globals());ink=nearest((12,13,16));blue=nearest((30,40,44))
for f in sorted(D.glob('*-raw.png')):finish(f)
def keyed_source(number):
 im=Image.open(ART/f'{number}.png').convert('RGBA');a=np.array(im);h,w=a.shape[:2];black=np.max(a[:,:,:3],axis=2)<8;seen=np.zeros((h,w),dtype=bool);queue=deque([(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]+[(0,y) for y in range(h)]+[(w-1,y) for y in range(h)])
 while queue:
  x,y=queue.popleft()
  if x<0 or y<0 or x>=w or y>=h or seen[y,x] or not black[y,x]:continue
  seen[y,x]=True;queue.extend([(x+1,y),(x-1,y),(x,y+1),(x,y-1)])
 a[seen,3]=0;return Image.fromarray(a)
board=Image.new('RGBA',(1120,600),(77,80,74,255));draw=ImageDraw.Draw(board)
for i,n in enumerate((1,9,17,25)):
 for row,im,label in [(0,keyed_source(n),f'SOURCE {n}'),(1,Image.open(OUT/f'assembly-{n:02d}.png'),f'PASS {stage} / view {n}')]:
  im=im.crop(im.getbbox());im.thumbnail((240,240),Image.Resampling.NEAREST);x=i*280+(280-im.width)//2;y=row*300+40;board.alpha_composite(im,(x,y));draw.text((i*280+8,row*300+8),label,fill='white')
board.save(OUT/'comparison.png')
if (OUT/'base.png').exists():
 base=Image.open(OUT/'base.png');board=Image.new('RGBA',(2560,1280),(77,80,74,255))
 for i in range(32):board.alpha_composite(Image.alpha_composite(base,Image.open(OUT/f'turret-{i:02d}.png')),(i%8*320,i//8*320))
 board.save(OUT/'all32.png')
print(OUT)
