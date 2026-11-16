"""Review-only legend. Runtime reuses build-valid-iso.png with ReplaceColor and Alpha0.55.
No supplied or runtime marker is overwritten.
"""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/placement-followup';OUT.mkdir(exist_ok=True)
normal=Image.open(ROOT/'mods/ca/bits/misc/overlays/build-valid-iso.png').convert('RGBA')
pale=normal.copy();pale.putdata([(144,255,144,round(a*.55)) if a else (r,g,b,a) for r,g,b,a in normal.getdata()])
invalid=Image.open(ROOT/'mods/ca/bits/misc/overlays/build-invalid-iso.png').convert('RGBA')
canvas=Image.new('RGBA',(480,112),(60,66,56,255));draw=ImageDraw.Draw(canvas)
for i,(name,im) in enumerate([('Building / blocked movement',normal),('Bib / passable',pale),('Invalid placement',invalid)]):
 canvas.alpha_composite(im.resize((120,60),Image.Resampling.NEAREST),(i*160+20,30));draw.text((i*160+4,8),name,fill='white')
canvas.save(OUT/'marker-legend.png')
print(OUT/'marker-legend.png')
