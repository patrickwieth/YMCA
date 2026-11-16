"""Native render review, never runtime integration or source edits."""
from pathlib import Path
from PIL import Image,ImageDraw
import argparse,shutil
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--voxel',action='store_true');a=p.parse_args()
F=Path(__file__).resolve().parents[2]/'art-source/gdi missile tower';OUT=F/f'pass-{a.iteration:02d}';CUT=OUT/'source-cuts'
if not CUT.exists():shutil.copytree(F/'source-cuts',CUT)
prefix='voxel' if a.voxel else 'turret';tag='voxel-' if a.voxel else ''
base=Image.open(CUT/'base.png').convert('RGBA');paths=sorted(p for p in OUT.glob(prefix+'-*.png') if p.stem.removeprefix(prefix+'-').isdigit());assert len(paths) in (8,32),len(paths);sheet=Image.new('RGBA',(224*4,248*((len(paths)+3)//4)),(45,49,46));d=ImageDraw.Draw(sheet)
frames=[]
for i,path in enumerate(paths):
 im=Image.new('RGBA',(224,224));im.alpha_composite(base,(48,80));im.alpha_composite(Image.open(path).convert('RGBA'));im.save(OUT/path.name.replace(prefix+'-',tag+'assembly-'));x=i%4*224;y=i//4*248;sheet.alpha_composite(im,(x,y+24));d.text((x+8,y+5),path.stem,fill='white')
 frame=Image.new('RGBA',im.size,(45,49,46));frame.alpha_composite(im);frames.append(frame.convert('RGB'))
sheet.save(OUT/(tag+'rotation-review.png'));ref=Image.open(CUT/'source-transparent.png');im=Image.new('RGBA',(448,224),(45,49,46));im.alpha_composite(ref,(48,80));im.alpha_composite(Image.open(OUT/(tag+'assembly-00000.png')),(224,0));im.resize((896,448),Image.Resampling.NEAREST).save(OUT/(tag+'comparison-2x.png'))
frames[0].save(OUT/(tag+'rotation.gif'),save_all=True,append_images=frames[1:],duration=100 if len(frames)>8 else 300,loop=0)
print(OUT)
