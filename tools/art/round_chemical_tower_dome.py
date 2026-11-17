"""Replace pass05's oversized flat cap with a short rounded source-color crown.
Preserve every pixel at/below source row48, all earlier passes and the source.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
R=Path(__file__).resolve().parents[2];F=R/'art-source/soviet chem tower';O=F/'pass-06';O.mkdir(exist_ok=True)
src=Image.open(F/'chemtower.png').convert('RGBA');old=Image.open(F/'pass-05/chemical-tower.png').convert('RGBA');out=old.copy();d=ImageDraw.Draw(out)
d.rectangle((0,0,127,47),fill=(0,0,0,0))
# Short rounded crown bounded by the source's dark edges at x55/71 on row48.
# A flattened apex, two curvature steps, light from upper-left and continuation
# of the existing diagonal panel seam: no unshaded triangular extension.
palette={'edge':(54,48),'dark':(64,48),'mid':(63,48),'light':(61,48),'seam':(60,48)}
rows={45:(61,'eeeeee'),46:(58,'edllsmmmdde'),47:(56,'edlllsmmmmmmddde')}
roles={'e':'edge','d':'dark','m':'mid','l':'light','s':'seam'};pixels=[]
for y,(start,pattern) in rows.items():
 for i,c in enumerate(pattern):
  xy=palette[roles[c]];out.putpixel((start+i,y),src.getpixel(xy));pixels.append({'destination':[start+i,y],'source':list(xy)})
out.save(O/'chemical-tower.png')
bg=(65,69,63,255);review=Image.new('RGBA',(96,40),bg)
review.alpha_composite(old.crop((40,35,88,75)));review.alpha_composite(out.crop((40,35,88,75)),(48,0))
review.resize((960,400),Image.Resampling.NEAREST).convert('RGB').save(O/'dome-before-after-10x.png')
canvas=Image.new('RGBA',(256,288),(71,78,62,255));bib=Image.open(R/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA');cx,cy=128,230
canvas.alpha_composite(bib,(cx-bib.width//2,cy-bib.height+bib.width//4));canvas.alpha_composite(out,(cx-64,cy-208+32-4))
canvas.convert('RGB').save(O/'placement-native.png');canvas.resize((768,864),Image.Resampling.NEAREST).convert('RGB').save(O/'placement-3x.png')
(O/'crown-source-pixels.json').write_text(json.dumps({'previous':'pass-05','source_sha256':hashlib.sha256((F/'chemtower.png').read_bytes()).hexdigest(),'inferred':'Only the missing three-row crown, with native source tones and a continuation of the existing panel seam.','pixels':pixels},indent=2)+'\n')
