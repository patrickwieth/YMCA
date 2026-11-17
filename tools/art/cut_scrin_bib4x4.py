"""Extend the reviewed native3x3 plating by translated, seam-cut quadrants only."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image,ImageDraw
from cut_scrin_bibs import sample,remove_cut_fragments
from quilt_scrin_bib_seams import refine_quadrants
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin bib';OUT=ART/'four-by-four';OUT.mkdir(exist_ok=True)
source=ART/'ground_plating-1534399694-0.png';base=ART/'bib3x3.png'
a=np.array(Image.open(base).convert('RGBA'));yy,xx=np.indices((282,512));x=xx-256;y=yy-131;u=x+2*y;v=-x+2*y
candidates=[sample(a,192+x-(du-dv)//2,99+y-(du+dv)//4) for du,dv in [(-64,-64),(-64,64),(64,-64),(64,64)]]
labels=(u>=0).astype(int)*2+(v>=0).astype(int);result=np.zeros_like(candidates[0])
for i,c in enumerate(candidates):result[labels==i]=c[labels==i]
result=refine_quadrants(candidates,labels,result);removed=remove_cut_fragments(result)
im=Image.fromarray(result);im.save(ART/'bib4x4.png')
original=np.array(Image.open(source).convert('RGBA'));colors={tuple(p) for p in original.reshape(-1,4)}
assert {tuple(p) for p in result[result[:,:,3]>0]}<=colors
meta={'source':source.name,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'cut_source':base.name,'cut_source_sha256':hashlib.sha256(base.read_bytes()).hexdigest(),'output':'bib4x4.png','cells':[4,4],'canvas':[512,282],'top_surface_center':[256,131],'ground_center':[256,155],'runtime_bottom_anchor_offset':[0,23],'native_pixels':True,'method':'Four integer-translated native3x3 pieces; binary minimum-cost seams through matching dark plate joints. No rescaling, interpolation, new colors or painting.','detached_pixels_removed':removed,'runtime_installed':False}
(ART/'bib4x4-cut-paste.json').write_text(json.dumps(meta,indent=2)+'\n')
board=Image.new('RGBA',(1000,350),(64,68,62,255));board.alpha_composite(Image.fromarray(a),(24,95));board.alpha_composite(im,(464,63));d=ImageDraw.Draw(board);d.text((24,16),'Existing 3x3 / unchanged',fill='white');d.text((464,16),'New 4x4 / native cut & paste',fill='white');board.save(OUT/'before-after-native.png');board.resize((2000,700),Image.Resampling.NEAREST).save(OUT/'before-after-2x.png')
print('Created native512x282 Scrin4x4 plating; previous bibs and runtime bindings untouched.')
