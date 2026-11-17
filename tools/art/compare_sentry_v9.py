"""Constrain lit V9 renders to exact original colors; compare spatial shading."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-09'
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=set(source.getdata());palette=np.array(sorted({c[:3] for c in colors if c[3]==255}),dtype=np.int32)
frames=[];audit=[]
for angle in (0,45,180,315):
 raw=np.array(Image.open(OUT/f'render-{angle:03d}-raw.png').convert('RGBA'));a=raw.copy();visible=a[:,:,3]>=128
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
 # No dither: real shadow gradients become discrete ORIGINAL color steps.
 nearest=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 a[:,:,:3]=palette[nearest][inverse].reshape(a.shape[0],a.shape[1],3)
 # Preserve material families: gray source metal must not turn olive/brown merely
 # because those scenery colors happen to be nearer to a newly shaded RGB value.
 old=Image.open(BASE/f'attempt-08/turret-{angle:03d}.png').convert('RGBA')
 albedo=np.array(old).astype(np.int32)
 neutral=(np.abs(albedo[:,:,0]-albedo[:,:,1])<=12)&(np.abs(albedo[:,:,1]-albedo[:,:,2])<=12)
 gray=palette[(np.abs(palette[:,0]-palette[:,1])<=12)&(np.abs(palette[:,1]-palette[:,2])<=12)]
 gray_nearest=((unique.astype(np.int32)[:,None,:]-gray[None,:,:])**2).sum(axis=2).argmin(axis=1)
 gray_mapped=gray[gray_nearest][inverse].reshape(a.shape[0],a.shape[1],3)
 a[neutral,:3]=gray_mapped[neutral]
 a[:,:,3]=np.where(visible,255,0);a[~visible]=0
 im=Image.fromarray(a);box=im.getbbox()
 assert box and min(box[:2])>0 and box[2]<192 and box[3]<192
 actual={c for c in im.getdata() if c[3]};assert actual<=colors
 assert old.getchannel('A').tobytes()==im.getchannel('A').tobytes(),f'Silhouette changed at {angle}'
 im.save(OUT/f'turret-{angle:03d}.png');frames.append(im)
 audit.append({'angle':angle,'final_colors':len(actual),'foreign_final_colors':len(actual-colors),
 'raw_lighting_colors':len({tuple(c) for c in raw[visible,:3]}),'exact_v8_silhouette':True})
(OUT/'palette-audit.json').write_text(json.dumps(audit,indent=2)+'\n')
old=Image.open(BASE/'attempt-08/turret-000.png').convert('RGBA')
comp=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comp)
for i,(im,label) in enumerate([(source.crop((43,58,187,161)),'Reference'),(old,'V8 / unlit'),(frames[0],'V9 / light + shadows')]):
 if i:im=im.crop(im.getbbox())
 comp.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comp.save(OUT/'comparison-native.png');comp.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
check=Image.new('RGB',(768,216),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip((0,45,180,315),frames)):
 check.paste(im,(i*192,0),im);d.text((i*192+5,197),f'V9 {angle} degrees',fill='white')
check.resize((1536,432),Image.Resampling.NEAREST).save(OUT/'rotation-check-2x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
px,py=json.loads((BASE/'attempt-05/attachment.json').read_text())['bearing_pivot_screen']
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(frames[0],(round(bx+73-px),round(by+16-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print(json.dumps(audit,indent=2))
