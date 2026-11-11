"""Install reviewed native160 V10; keep all source/study files and prior exports."""
from pathlib import Path
from PIL import Image
from PIL.PngImagePlugin import PngInfo
import numpy as np,json,hashlib,shutil,math
ROOT=Path(__file__).resolve().parents[2];STUDY=ROOT/'art-source/gdi ion cannon/attempt-10-preview';SRC=STUDY/'160';EXPORT=STUDY/'export';EXPORT.mkdir(exist_ok=True);DEST=ROOT/'mods/ca/bits/gdi/buildings/experimental';SIZE=160
base=Image.open(SRC/'base.png').convert('RGBA');assert base.size==(SIZE,SIZE)
def metadata(count):
 m=PngInfo();m.add_text('FrameSize','160,160');m.add_text('FrameAmount',str(count));return m
base.save(EXPORT/'heavy-blaster-base.png',pnginfo=metadata(1));sheet=Image.new('RGBA',(1280,640));height=0
for frame in range(32):
 idx=(20+frame)%32;im=Image.open(SRC/f'turret-{idx:03d}.png').convert('RGBA');assert im.size==(SIZE,SIZE);sheet.paste(im,((frame%8)*SIZE,(frame//8)*SIZE))
 assembly=np.array(Image.alpha_composite(base,im));y,x=np.where(assembly[:,:,3]>0);sx=x+.5-87;sy=y+.5-104
 assert np.all(abs(sx)<84) and np.all(sy<=42-.5*abs(sx));height=max(height,float((-42+.5*abs(sx)-sy).max()))
sheet.save(EXPORT/'heavy-blaster-turret32.png',pnginfo=metadata(32));assets={}
for name in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):
 shutil.copyfile(EXPORT/name,DEST/name);assets[name]=hashlib.sha256((EXPORT/name).read_bytes()).hexdigest()
report={'actor':'ion_defense','image':'ion_defense.native','version':'V10 GDI gold / bright straight conductor / V9 contours','frame_size':[160,160],'facings':32,'scale':1,'base_offset':[-7,-24],'turret_offset':[-1,-10],'turret_world_offset':[0,-136,317],'muzzle_local_offset':[1631,0,675],'camera_pitch':85,'selection_bounds':[1901,1901,0,0],'selection_height':math.ceil(height)+1,'required_selection_height':height,'gameplay_footprint':'unchanged 2x2','assets':assets,'installed':True,'live_review':False}
d=STUDY/'integration';d.mkdir(exist_ok=True);(d/'binding.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
