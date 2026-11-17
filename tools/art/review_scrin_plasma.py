"""Native plasma study review; uses only this turret's own source palette."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,sys,ast
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin plasma';stage=int(sys.argv[1]);D=ART/f'pass-{stage:02d}';OUT=D/'review';OUT.mkdir(exist_ok=True);colors=np.array(json.loads((ART/'reference-review/palette.json').read_text()),dtype=np.int32)
source=ast.parse((Path(__file__).parent/'review_scrin_shard_launcher.py').read_text());functions=[n for n in source.body if isinstance(n,ast.FunctionDef)];exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared native finishing>','exec'),globals());ink=nearest((12,13,22));blue=nearest((10,72,100))
for f in sorted(D.glob('*-raw.png')):finish(f)
size=json.loads((D/'geometry.json').read_text())['frame_size'];ref=Image.new('RGBA',(size,size));ref.alpha_composite(Image.open(ART/'photon_cannon-1201896784-0.png').convert('RGBA'),((size-256)//2,(size-256)//2));views=[ref,Image.open(OUT/'reference.png')]+[Image.open(OUT/f'assembly-{i:02d}.png') for i in (0,8,24)];board=Image.new('RGBA',(size*5,size+28),(77,80,78,255));draw=ImageDraw.Draw(board)
for i,(label,im) in enumerate(zip(['SUPPLIED SOURCE',f'PASS {stage} / source view','REAR','SIDE','OPPOSITE SIDE'],views)):
 board.alpha_composite(im,(i*size,28));draw.text((i*size+5,8),label,fill='white')
board.save(OUT/'comparison.png')
if (OUT/'base.png').exists():
 base=Image.open(OUT/'base.png');board=Image.new('RGBA',(size*8,size*4),(77,80,78,255))
 for i in range(32):board.alpha_composite(Image.alpha_composite(base,Image.open(OUT/f'turret-{i:02d}.png')),(i%8*size,i//8*size))
 board.save(OUT/'all32.png')
print(OUT)
