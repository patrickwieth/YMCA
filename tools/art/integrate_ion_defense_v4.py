"""Copy preserved V4 native parts into the mod; no resampling or source edits."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,math,shutil
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'art-source/gdi ion cannon/attempt-04/export';OUT=ROOT/'mods/ca/bits/gdi/buildings/experimental';OUT.mkdir(parents=True,exist_ok=True)
report={'actor':'ion_defense','image':'ion_defense.native','scale':1,'anchor':'Center','base_offset':[-7,-25],'turret_offset':[-1,-10],'turret_world_offset':[0,-136,339],'camera_pitch':85,'muzzle_local_offset':[1631,0,675],'base_ground_center_in_canvas':[86.5,104.758],'pivot_in_canvas':[81.06066021392667,89.57754749356471],'selection_decoration_bounds':[1810,1810,0,0],'selection_decoration_height':68,'gameplay_footprint':'unchanged 2x2','muzzle':'weapon unchanged; model-derived turret/muzzle attachment replaces legacy origin; checked in all 32 engine projections, live firing still pending','damaged_art':'same supplied native parts; no separate damaged model','assets':{}}
for filename in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):
 src=SOURCE/filename;shutil.copyfile(src,OUT/filename);report['assets'][filename]=hashlib.sha256(src.read_bytes()).hexdigest()
base=Image.open(OUT/'heavy-blaster-base.png').convert('RGBA');sheet=Image.open(OUT/'heavy-blaster-turret32.png').convert('RGBA');required=0
for i in range(32):
 turret=sheet.crop(((i%8)*160,(i//8)*160,(i%8+1)*160,(i//8+1)*160));a=np.array(Image.alpha_composite(base,turret));y,x=np.where(a[:,:,3]>0);sx=x+.5-87;sy=y+.5-105
 assert np.all(np.abs(sx)<=80) and np.all(sy<=40-.5*np.abs(sx))
 required=max(required,float((-40+.5*np.abs(sx)-sy).max()))
assert required<=68;report['required_decoration_height']=math.ceil(required)
folder=SOURCE.parent/'integration';folder.mkdir(exist_ok=True);(folder/'binding.json').write_text(json.dumps(report,indent=2)+'\n')
print('ION DEFENSE native assets copied; all 32 views fit the visual selection envelope.')
