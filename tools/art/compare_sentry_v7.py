"""Native-scale texture comparison and four-angle sanity check for V7."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
import json
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-07'
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=set(source.getdata());palette=np.array(sorted({c[:3] for c in colors if c[3]==255}),dtype=np.int32)
frames=[]
for angle in (0,45,180,315):
 a=np.array(Image.open(OUT/f'render-{angle:03d}-raw.png').convert('RGBA'))
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
 nearest=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[nearest][inverse].reshape(a.shape[0],a.shape[1],3)
 a[:,:,3]=np.where(a[:,:,3]>=128,255,0);a[a[:,:,3]==0]=0
 im=Image.fromarray(a);bbox=im.getbbox()
 assert bbox and min(bbox[:2])>0 and bbox[2]<192 and bbox[3]<192
 assert {c for c in im.getdata() if c[3]}<=colors
 previous=Image.open(BASE/f'attempt-05/turret-{angle:03d}.png').convert('RGBA')
 assert im.getchannel('A').tobytes()==previous.getchannel('A').tobytes(),f'Silhouette changed: {angle}'
 im.save(OUT/f'turret-{angle:03d}.png');frames.append(im)
v6=Image.open(BASE/'attempt-06/textured-v6.png').convert('RGBA')
comparison=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate([(source.crop((43,58,187,161)),'Reference'),(v6,'V6 textures'),(frames[0],'V7 / same model')]):
 if i:im=im.crop(im.getbbox())
 comparison.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png');comparison.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
check=Image.new('RGB',(768,216),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip((0,45,180,315),frames)):
 check.paste(im,(i*192,0),im);d.text((i*192+5,197),f'V7 {angle} degrees',fill='white')
check.resize((1536,432),Image.Resampling.NEAREST).save(OUT/'rotation-check-2x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
px,py=json.loads((BASE/'attempt-05/attachment.json').read_text())['bearing_pivot_screen']
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(frames[0],(round(bx+73-px),round(by+16-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print('V7: source palette, unclipped binary alpha, four exact V5 silhouettes verified.')
