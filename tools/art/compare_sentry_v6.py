"""V5/V6 texture comparison on identical geometry, camera and native canvas."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
import json
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-06'
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=set(source.getdata());palette=np.array(sorted({c[:3] for c in colors if c[3]==255}),dtype=np.int32)
a=np.array(Image.open(OUT/'textured-raw.png').convert('RGBA'))
unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
nearest=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
a[:,:,:3]=palette[nearest][inverse].reshape(a.shape[0],a.shape[1],3)
a[:,:,3]=np.where(a[:,:,3]>=128,255,0);a[a[:,:,3]==0]=0
im=Image.fromarray(a);bbox=im.getbbox()
assert bbox and min(bbox[:2])>0 and bbox[2]<192 and bbox[3]<192
assert {c for c in im.getdata() if c[3]}<=colors
v5=Image.open(BASE/'attempt-05/turret-000.png').convert('RGBA')
assert im.getchannel('A').tobytes()==v5.getchannel('A').tobytes(),'Silhouette changed'
im.save(OUT/'textured-v6.png');im.resize((768,768),Image.Resampling.NEAREST).save(OUT/'textured-v6-4x.png')
comparison=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comparison)
for i,(item,label) in enumerate([(source.crop((43,58,187,161)),'Reference'),(v5,'V5 textures'),(im,'V6 / same model')]):
 if i:item=item.crop(item.getbbox())
 comparison.paste(item,(i*192+(192-item.width)//2,10),item);d.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png');comparison.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
px,py=json.loads((BASE/'attempt-05/attachment.json').read_text())['bearing_pivot_screen']
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(im,(round(bx+73-px),round(by+16-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print('V6 verified: source palette, binary alpha, no clipping, exact V5 silhouette.')
