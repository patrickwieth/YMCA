"""Source-palette/alpha cleanup and honest native-scale V3/V4 review."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-04'
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
palette=np.array(sorted({c[:3] for c in source.getdata() if c[3]==255}),dtype=np.int32)
raw=Image.open(OUT/'textured-raw.png').convert('RGBA');a=np.array(raw)
# Emission/color-management roundtrip can introduce a +/-1 color difference.
# Match source colors without dithering, smoothing, or resizing.
unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
idx=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
a[:,:,:3]=palette[idx][inverse].reshape(a.shape[0],a.shape[1],3)
a[:,:,3]=np.where(a[:,:,3]>=128,255,0);a[a[:,:,3]==0]=0
image=Image.fromarray(a);image.save(OUT/'textured-v4.png')
assert {c for c in image.getdata() if c[3]} <= set(source.getdata())
image.resize((768,768),Image.Resampling.NEAREST).save(OUT/'textured-v4-4x.png')
items=[(source.crop((43,58,187,161)),'Reference'),
       (Image.open(BASE/'attempt-03/geometry-neutral.png').convert('RGBA'),'V3 neutral'),
       (Image.open(OUT/'neutral-raw.png').convert('RGBA'),'V4 geometry'),
       (image,'V4 source textures')]
comparison=Image.new('RGB',(768,152),'#77736e');draw=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate(items):
 if i:
  bbox=im.getbbox();assert bbox and bbox[0]>0 and bbox[1]>0 and bbox[2]<im.width and bbox[3]<im.height
  im=im.crop(bbox)
 comparison.paste(im,(i*192+(192-im.width)//2,10),im);draw.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png')
comparison.resize((2304,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
print('V4 render verified: source-palette colors, binary alpha, no clipping. Comparison saved.')
