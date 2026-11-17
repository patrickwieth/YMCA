"""Palette-valid first-study reviews; not a finished game sprite export."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-01'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};palette=np.array(sorted(c[:3] for c in colors),dtype=np.int32)
ink=palette[palette.sum(axis=1).argmin()];frames=[];report=[]
for angle in (0,45,180,315):
 a=np.array(Image.open(OUT/f'render-{angle:03d}-raw.png').convert('RGBA'));solid=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
 indices=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[indices][inverse].reshape(a.shape[:2]+(3,));a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 edge=np.zeros_like(solid)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 a[edge,:3]=ink
 im=Image.fromarray(a);assert {c for c in im.getdata() if c[3]}<=colors
 box=im.getbbox();assert min(box[:2])>0 and max(box[2:])<320
 im.save(OUT/f'study-{angle:03d}.png');frames.append(im)
 report.append({'angle':angle,'size':list(im.size),'bounds':box,'source_palette_errors':0,'status':'geometry/material study, not game ready'})
reference=Image.new('RGBA',(320,320));reference.alpha_composite(source.resize((256,256),Image.Resampling.NEAREST),(32,32))
review=Image.new('RGB',(320*3,352),'#77736e');draw=ImageDraw.Draw(review)
for i,(im,label) in enumerate(((reference,'Source / study reduction'),(frames[0],'V1 reconstruction / front'),(frames[2],'V1 / unseen rear extrapolated'))):
 review.paste(im,(i*320,24),im);draw.text((i*320+6,6),label,fill='white')
review.resize((1920,704),Image.Resampling.NEAREST).save(OUT/'comparison-2x.png')
sheet=Image.new('RGB',(1280,344),'#77736e');draw=ImageDraw.Draw(sheet)
for i,(angle,im) in enumerate(zip((0,45,180,315),frames)):
 sheet.paste(im,(i*320,20),im);draw.text((i*320+5,3),str(angle),fill='white')
sheet.save(OUT/'four-views.png')
(OUT/'study-validation.json').write_text(json.dumps({'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'frames':report,'not_implemented':['final detail/proportion match','native game resolution','32 facings','game actor/weapon']},indent=2)+'\n')
print('HEAVY_BLASTER_REVIEW_COMPLETE')
