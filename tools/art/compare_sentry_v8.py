"""V6/V7/V8 review and exact-source palette audit, with no geometry changes."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
import json
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/'attempt-08'
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=set(source.getdata());palette=np.array(sorted({c[:3] for c in colors if c[3]==255}),dtype=np.int32)
frames=[];audits=[]
for angle in (0,45,180,315):
 a=np.array(Image.open(OUT/f'render-{angle:03d}-raw.png').convert('RGBA'))
 visible=a[:,:,3]>=128
 raw_colors={tuple(c) for c in a[visible,:3]}
 unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
 nearest=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
 mapped=palette[nearest][inverse].reshape(a.shape[0],a.shape[1],3)
 max_delta=int(np.abs(mapped[visible]-a[visible,:3].astype(np.int32)).max())
 a[:,:,:3]=mapped;a[:,:,3]=np.where(visible,255,0);a[~visible]=0
 im=Image.fromarray(a);bbox=im.getbbox()
 assert bbox and min(bbox[:2])>0 and bbox[2]<192 and bbox[3]<192
 output_colors={c for c in im.getdata() if c[3]}
 assert output_colors<=colors
 previous=Image.open(BASE/f'attempt-05/turret-{angle:03d}.png').convert('RGBA')
 assert im.getchannel('A').tobytes()==previous.getchannel('A').tobytes(),f'Silhouette changed: {angle}'
 im.save(OUT/f'turret-{angle:03d}.png');frames.append(im)
 audits.append({'angle':angle,'visible_colors':len(output_colors),'colors_not_in_original':len(output_colors-colors),
 'raw_colors_not_in_original':len(raw_colors-{c[:3] for c in colors if c[3]==255}),
 'max_color_management_roundtrip_correction':max_delta,'silhouette_matches_v5':True})
(OUT/'palette-audit.json').write_text(json.dumps({'source_opaque_colors':len(palette),'views':audits},indent=2)+'\n')
items=[(source.crop((43,58,187,161)),'Reference'),
 (Image.open(BASE/'attempt-06/textured-v6.png').convert('RGBA'),'V6'),
 (Image.open(BASE/'attempt-07/turret-000.png').convert('RGBA'),'V7'),(frames[0],'V8: V6 + source rivets')]
comparison=Image.new('RGB',(768,152),'#77736e');d=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate(items):
 if i:im=im.crop(im.getbbox())
 comparison.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png');comparison.resize((2304,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
check=Image.new('RGB',(768,216),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip((0,45,180,315),frames)):
 check.paste(im,(i*192,0),im);d.text((i*192+5,197),f'V8 {angle} degrees',fill='white')
check.resize((1536,432),Image.Resampling.NEAREST).save(OUT/'rotation-check-2x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
px,py=json.loads((BASE/'attempt-05/attachment.json').read_text())['bearing_pivot_screen']
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(frames[0],(round(bx+73-px),round(by+16-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
# A same-scale close-up makes the tiny copied fastener details inspectable.
close=Image.new('RGB',(384,192),'#77736e')
for i,im in enumerate((items[1][0],frames[0])):close.paste(im,(i*192,0),im)
close.resize((1536,768),Image.Resampling.NEAREST).save(OUT/'v6-v8-detail-4x.png')
print(json.dumps(audits,indent=2))
