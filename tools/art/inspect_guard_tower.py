"""Read-only guard-tower binding preview and source/selection audit."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib,math
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'mods/ca/bits/gdi/buildings/guard tower.png';OUT=ROOT/'art-source/gdi-guard-tower';OUT.mkdir(exist_ok=True)
im=Image.open(SRC).convert('RGBA');a=np.array(im);y,x=np.where(a[:,:,3]>0);sx=x+.5-65;sy=y+.5-131
assert im.size==(128,150) and np.all(abs(sx)<=48) and np.all(sy<=24-.5*abs(sx))
height=float(np.max(-24+.5*abs(sx)-sy));assert height<=111
canvas=Image.new('RGB',(200,230),'#77736e');canvas.paste(im,(100-65,180-131),im);d=ImageDraw.Draw(canvas)
d.polygon([(52,180),(100,156),(148,180),(100,204)],outline='#67ba72')
d.line([(52,180),(52,69),(100,45),(148,69),(148,180)],fill='#67ba72');d.text((6,6),'GTWR / native 128x150 / Scale 1',fill='white')
canvas.resize((600,690),Image.Resampling.NEAREST).save(OUT/'binding-preview-3x.png')
report={'actor':'gtwr','image':'gtwr.gdi.png','source':str(SRC.relative_to(ROOT)),'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'size':list(im.size),'alpha_bounds':im.getbbox(),'scale':1,'anchor':'Center','offset':[-1,-56],'ground_origin_pixel':[65,131],'selection_bounds':[1086,1086,0,0],'selection_height':111,'required_selection_height':math.ceil(height),'turret_offset':[0,0,2007],'armament_local_offset':[256,0,256],'muzzle_center_screen_y':-(2007+256)*64/1448,'status':'native static supplied PNG bound to existing GTWR; live review pending'}
(OUT/'binding.json').write_text(json.dumps(report,indent=2)+'\n');print('GUARD_TOWER_NATIVE_ART_AUDIT_PASSED',report['source_sha256'])
