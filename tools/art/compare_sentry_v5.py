"""Palette-clean V5 and compare source/V4/V5 plus diagnostic angles."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
import json
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-05'
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
 im.save(OUT/f'turret-{angle:03d}.png');frames.append(im)
frames[0].resize((768,768),Image.Resampling.NEAREST).save(OUT/'turret-v5-4x.png')
ref=source.crop((43,58,187,161))
v4=Image.open(BASE/'attempt-04/textured-v4.png').convert('RGBA')
comparison=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate([(ref,'Reference'),(v4,'V4'),(frames[0],'V5')]):
 if i:im=im.crop(im.getbbox())
 comparison.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png');comparison.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
check=Image.new('RGB',(768,216),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip((0,45,180,315),frames)):
 check.paste(im,(i*192,0),im);d.text((i*192+5,197),f'V5 {angle} degrees',fill='white')
check.save(OUT/'rotation-check.png')
# Stationary supplied base stays byte-for-byte unchanged; composite is preview only.
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
attachment=json.loads((OUT/'attachment.json').read_text())
px,py=attachment['bearing_pivot_screen']
assembly=Image.new('RGBA',(256,256))
bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by))
# Approximate socket center read from supplied base, not a game-rule offset.
socket=(bx+73,by+16)
assembly.alpha_composite(frames[0],(round(socket[0]-px),round(socket[1]-py)))
assembly.save(OUT/'base-assembly-preview.png')
assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print('V5: four unclipped diagnostic views, fixed canvas, source-only palette verified.')
