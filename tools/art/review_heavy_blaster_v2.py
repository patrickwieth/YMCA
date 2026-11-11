"""Source-palette finishing and honest V1/V2 detail comparisons."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-02'
source=Image.open(BASE/'heavy blaster 3.png').convert('RGBA');colors={c for c in source.getdata() if c[3]==255};palette=np.array(sorted(c[:3] for c in colors),dtype=np.int32);ink=palette[palette.sum(axis=1).argmin()]
reports=[]
for size in (320,128):
 a=np.array(Image.open(OUT/f'front-{size}-raw.png').convert('RGBA'));solid=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True);indices=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[indices][inverse].reshape(size,size,3);a[:,:,3]=np.where(solid,255,0);a[~solid]=0
 edge=np.zeros_like(solid)
 for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
 a[edge,:3]=ink;im=Image.fromarray(a);im.save(OUT/f'front-{size}.png')
 assert {c for c in im.getdata() if c[3]}<=colors
 box=im.getbbox();assert min(box[:2])>0 and max(box[2:])<size
 reports.append({'resolution':size,'rendered_directly':True,'source_palette_errors':0,'alpha_values':sorted(set(im.getchannel('A').getdata())),'bounds':box})
reference=Image.new('RGBA',(320,320));reference.alpha_composite(source.resize((256,256),Image.Resampling.NEAREST),(32,32))
canvas=Image.new('RGB',(960,352),'#77736e');draw=ImageDraw.Draw(canvas)
for i,(im,label) in enumerate(((reference,'Reference / unchanged'),(Image.open(BASE/'attempt-01/study-000.png'),'V1 / rejected'),(Image.open(OUT/'front-320.png'),'V2 / source detail and UV study'))):
 canvas.paste(im,(i*320,24),im);draw.text((i*320+5,5),label,fill='white')
canvas.resize((1920,704),Image.Resampling.NEAREST).save(OUT/'comparison-2x.png')
native=Image.new('RGB',(512,548),'#77736e');im=Image.open(OUT/'front-128.png').resize((512,512),Image.Resampling.NEAREST);native.paste(im,(0,28),im);ImageDraw.Draw(native).text((5,7),'Direct 128px render / shown at 4x / NOT game integrated',fill='white');native.save(OUT/'native-review-4x.png')
(OUT/'validation.json').write_text(json.dumps({'source_sha256':hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'views':reports,'status':'front-view detail correction; geometry and unseen faces still need work; no game integration'},indent=2)+'\n')
print('V2 palette/alpha/clipping checks passed at 320 and directly rendered 128 pixels.')
