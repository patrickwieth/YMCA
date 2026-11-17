"""Native4x3 cut from Soviet4x4 bib; upper/rear corner and all retained pixels fixed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
import hashlib,json
R=Path(__file__).resolve().parents[2];src=R/'mods/ca/bits/soviet/buildings/bib4x4.png';dst=src.with_name('bib4x3.png');im=Image.open(src).convert('RGBA');assert im.size==(512,256)
mask=Image.new('L',im.size);ImageDraw.Draw(mask).polygon([(256,0),(512,128),(320,224),(64,96)],fill=255);alpha=ImageChops.multiply(im.getchannel('A'),mask);cut=im.copy();cut.putalpha(alpha);cut=cut.crop((64,0,512,224));cut.save(dst)
out=R/'art-source/soviet-service-depot';out.mkdir(exist_ok=True);(out/'bib-cut.json').write_text(json.dumps({'source':str(src.relative_to(R)),'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'destination':str(dst.relative_to(R)),'crop':[64,0,512,224],'retained_cells':'x0..3,y0..2','sequence_offset':[32,-16],'operation':'binary cell-boundary mask and integer crop; no resize/repaint; original stationary anchor retained'},indent=2)+'\n')
print(dst)
