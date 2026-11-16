"""Native Nod4x3: extend the existing3x3 by one isometric cell using two source cuts."""
from pathlib import Path
import sys,json,hashlib
import numpy as np
from PIL import Image,ImageDraw
from quilt_scrin_bib_seams import join
from cut_scrin_bibs import sample
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/nod-bib-followup';OUT.mkdir(exist_ok=True)
source=ROOT/'mods/ca/bits/nod/buildings/bib3x3.png';a=np.array(Image.open(source).convert('RGBA'));h,w=a.shape[:2];yy,xx=np.indices((h+32,w+64));u=(xx-(w+64)/2)+2*(yy-(w+64)/4)
left=sample(a,xx,yy);right=sample(a,xx-64,yy-32);prefer=u>=0;band=(abs(u)<30)&(left[:,:,3]>0)&(right[:,:,3]>0)
result,chosen=join(left,right,prefer,band);visible=np.where(prefer,right[:,:,3],left[:,:,3]);result[:,:,3]=visible;result[visible==0]=0
im=Image.fromarray(result);im.save(OUT/'bib4x3.png');im.save(ROOT/'mods/ca/bits/nod/buildings/bib4x3.png')
colors={tuple(p) for p in a.reshape(-1,4)};assert {tuple(p) for p in result[result[:,:,3]>0]}<=colors
meta={'source':'mods/ca/bits/nod/buildings/bib3x3.png','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'canvas':list(im.size),'cells':[4,3],'integer_piece_translations':[[0,0],[64,32]],'top_surface_center':[224,112],'runtime_bottom_offset':[32,16],'method':'Two unchanged native3x3 pieces; minimum-cost binary seam in overlapping plates; no scaling, blur, recoloring or painting. Offset preserves the old rear/left cell anchor while extending one cell to the right/front.'}
(OUT/'bib4x3-binding.json').write_text(json.dumps(meta,indent=2)+'\n')
board=Image.new('RGBA',(900,280),(68,71,68,255));board.alpha_composite(Image.fromarray(a),(12,45));board.alpha_composite(im,(432,29));d=ImageDraw.Draw(board);d.text((12,12),'Existing3x3 (unchanged)',fill='white');d.text((432,12),'Native4x3 / cut & paste',fill='white');board.resize((1800,560),Image.Resampling.NEAREST).save(OUT/'bib-before-after-2x.png')
