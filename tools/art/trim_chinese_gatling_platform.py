"""Remove the hazard-band strip from the approved platform and slide its walls inward."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';OLD=ART/'platform-cut/pass-03';OUT=ART/'platform-cut/pass-04';OUT.mkdir(exist_ok=True)
a=np.array(Image.open(OLD/'platform.png').convert('RGBA'));frame=np.zeros_like(a);yy,xx=np.indices(a.shape[:2]);origins=np.full((*a.shape[:2],2),-1,dtype=np.int16)
def mask(points):
 im=Image.new('L',(128,112));ImageDraw.Draw(im).polygon(points,fill=255);return np.array(im)>0
def paste(region,dx,dy):
 sy,sx=np.where(region&(a[:,:,3]>0));tx=sx+dx;ty=sy+dy;ok=(tx>=0)&(tx<128)&(ty>=0)&(ty<112);sx=sx[ok];sy=sy[ok];tx=tx[ok];ty=ty[ok];frame[ty,tx]=a[sy,sx];origins[ty,tx]=np.stack([sx,sy],axis=1)
# Keep the deck/bearing pixels at precisely their approved positions, clipping only the rim.
paste(mask([(4,37),(41,16),(87,16),(123,37),(123,54),(87,75),(41,75),(4,54)]),0,0)
# Slide existing lower wall pieces toward the remaining deck. No painted replacement edge.
paste((xx>=44)&(xx<=84)&(yy>=80),0,-4)
paste((xx<44)&(yy>=60+xx/2),4,-5)
paste((xx>84)&(yy>=60+(127-xx)/2),-4,-5)
im=Image.fromarray(frame);im.save(OUT/'platform.png');np.save(OUT/'source-pixel-origins.npy',origins)
visible=frame[:,:,3]>0;assert np.array_equal(frame[visible],a[origins[visible,1],origins[visible,0]])
meta=json.loads((OLD/'binding.json').read_text());meta.update(method='Delete the whole hazard rim; translate the existing wall panels4px inward and4/5px up. Native interior/bearing unchanged. No replacement pixels, recoloring or resampling.',previous_platform_sha256=hashlib.sha256((OLD/'platform.png').read_bytes()).hexdigest(),visible_width=int(np.where(visible)[1].max()-np.where(visible)[1].min()+1),runtime_installed=False)
(OUT/'binding.json').write_text(json.dumps(meta,indent=2)+'\n')
board=Image.new('RGBA',(320,150),(65,70,63,255));board.alpha_composite(Image.fromarray(a),(16,30));board.alpha_composite(im,(176,30));d=ImageDraw.Draw(board);d.text((16,8),'Approved / before',fill='white');d.text((176,8),'Band cut / walls in',fill='white');board.resize((1280,600),Image.Resampling.NEAREST).save(OUT/'cut-review-4x.png')
atlas=Image.open(ROOT/'mods/ca/bits/china/buildings/gatling-turret32.png').convert('RGBA');bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA');sheet=Image.new('RGBA',(1792,896),(65,70,63,255));frames=[]
for i in range(32):
 tile=Image.new('RGBA',(224,224),(65,70,63,255));tile.alpha_composite(bib,(48,143));tile.alpha_composite(im,(48,round(175-56+meta['body_offset'][1])))
 turret=atlas.crop((i%8*320,i//8*320,i%8*320+320,i//8*320+320)).resize((208,208),Image.Resampling.NEAREST)
 tile.alpha_composite(turret,(8,round(175-160*.65-34*.65-604*64/1448)));sheet.alpha_composite(tile,(i%8*224,i//8*224));frames.append(tile.convert('RGB'))
sheet.save(OUT/'all32-native.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
print('Trimmed platform width:',meta['visible_width'])
