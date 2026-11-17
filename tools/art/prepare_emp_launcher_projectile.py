"""Extract the supplied loaded/empty difference; build a matching directional missile.
Never modifies either original PNG. Rotation is sprite art, not a 3D reconstruction.
"""
from pathlib import Path
import json,math,hashlib
import numpy as np
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'mods/ca/bits/gdi/buildings';OUT=ROOT/'art-source/gdi-emp-launcher';OUT.mkdir(exist_ok=True)
full=Image.open(ART/'emp launcher.png').convert('RGBA');empty=Image.open(ART/'emp launcher empty.png').convert('RGBA')
a=np.array(full);b=np.array(empty);mask=np.any(a!=b,axis=2)&(a[:,:,3]>0)
y,x=np.where(mask);assert (x.min(),y.min(),x.max(),y.max())==(50,13,123,58)
# Fill only enclosed coincident-color holes inside the extracted rocket.
from collections import deque
padded=np.pad(mask,1);outside=np.zeros_like(padded);outside[0,0]=True;queue=deque([(0,0)])
while queue:
    cy,cx=queue.popleft()
    for ny,nx in ((cy-1,cx),(cy+1,cx),(cy,cx-1),(cy,cx+1)):
        if 0<=ny<padded.shape[0] and 0<=nx<padded.shape[1] and not padded[ny,nx] and not outside[ny,nx]:
            outside[ny,nx]=True;queue.append((ny,nx))
inside=~outside[1:-1,1:-1]
assert not inside[59:].any(), 'Building pixels must never enter the projectile.'
rocket=a.copy();rocket[~inside]=0
center=(float(x.mean()),float(y.mean()));axis=np.linalg.eigh(np.cov(np.stack((x,y))))[1][:,-1];axis=axis if axis[0]<0 else -axis
source_angle=math.degrees(math.atan2(axis[1],axis[0]))
canvas=Image.new('RGBA',(128,128));canvas.alpha_composite(Image.fromarray(rocket),(round(64-center[0]),round(64-center[1])))
sheet=Image.new('RGBA',(128*8,128*4));board=Image.new('RGB',(128*8,148*4),'#77736e');draw=ImageDraw.Draw(board)
for i in range(32):
    frame=canvas.rotate(source_angle-(-90-i*11.25),resample=Image.Resampling.NEAREST)
    sheet.paste(frame,(i%8*128,i//8*128));board.paste(frame,(i%8*128,i//8*148+20),frame);draw.text((i%8*128+4,i//8*148+4),str(i),fill='white')
meta=PngInfo();meta.add_text('FrameSize','128,128');meta.add_text('FrameAmount','32')
sheet.save(ART/'emp-missile32.png',pnginfo=meta);board.save(OUT/'projectile-directions.png')
Image.fromarray(rocket).save(OUT/'extracted-missile.png')
report={'loaded_sha256':hashlib.sha256((ART/'emp launcher.png').read_bytes()).hexdigest(),'empty_sha256':hashlib.sha256((ART/'emp launcher empty.png').read_bytes()).hexdigest(),'source_size':list(full.size),'missile_center_source':center,'source_axis_screen':list(map(float,axis)),'source_pitch_degrees':abs(math.degrees(math.atan2(axis[1],axis[0]))+180),'runtime_launch_yaw':256,'runtime_launch_pitch':72,'body_anchor':'Bottom','body_offset':[0,16],'ground_source':[128,176],'muzzle_local':[0,-931,3249],'source_art_unchanged':True}
(OUT/'calibration.json').write_text(json.dumps(report,indent=2)+'\n')
print('EMP_MISSILE_ART_READY',center,source_angle)
