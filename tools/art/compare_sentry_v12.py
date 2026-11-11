"""V12 palette/albedo checks, then optional final review after the V11 ink pass."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,argparse
p=argparse.ArgumentParser();p.add_argument('--final',action='store_true')
p.add_argument('--attempt',default='attempt-12')
p.add_argument('--previous-attempt',default='attempt-11')
p.add_argument('--previous-prefix',default='turret-')
p.add_argument('--previous-label',default='V11')
p.add_argument('--label',default='V12 / review corrections')
p.add_argument('--expect-same-silhouette',action='store_true')
p.add_argument('--angles',default='0,45,180,315')
args=p.parse_args();angles=tuple(map(int,args.angles.split(',')))
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/args.attempt
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors=set(source.getdata());palette=np.array(sorted({c[:3] for c in colors if c[3]==255}),dtype=np.int32)
gray=palette[(np.abs(palette[:,0]-palette[:,1])<=12)&(np.abs(palette[:,1]-palette[:,2])<=12)]
frames=[];audit=[]
for angle in angles:
 if args.final:im=Image.open(OUT/f'edges/turret-{angle:03d}.png').convert('RGBA')
 else:
  a=np.array(Image.open(OUT/f'render-{angle:03d}-raw.png').convert('RGBA'))
  albedo=np.array(Image.open(OUT/f'albedo-{angle:03d}.png').convert('RGBA')).astype(np.int32)
  visible=a[:,:,3]>=128;assert np.array_equal(visible,albedo[:,:,3]>=128)
  unique,inverse=np.unique(a[:,:,:3].reshape(-1,3),axis=0,return_inverse=True)
  idx=((unique.astype(np.int32)[:,None,:]-palette[None,:,:])**2).sum(axis=2).argmin(axis=1)
  a[:,:,:3]=palette[idx][inverse].reshape(a.shape[0],a.shape[1],3)
  neutral=(np.abs(albedo[:,:,0]-albedo[:,:,1])<=12)&(np.abs(albedo[:,:,1]-albedo[:,:,2])<=12)
  idx=((unique.astype(np.int32)[:,None,:]-gray[None,:,:])**2).sum(axis=2).argmin(axis=1)
  mapped=gray[idx][inverse].reshape(a.shape[0],a.shape[1],3);a[neutral,:3]=mapped[neutral]
  a[:,:,3]=np.where(visible,255,0);a[~visible]=0;im=Image.fromarray(a)
 box=im.getbbox();assert box and min(box[:2])>0 and box[2]<im.width and box[3]<im.height,(angle,box)
 actual={c for c in im.getdata() if c[3]};assert actual<=colors
 if args.expect_same_silhouette:
  before=Image.open(BASE/args.previous_attempt/f'{args.previous_prefix}{angle:03d}.png').convert('RGBA')
  assert before.getchannel('A').tobytes()==im.getchannel('A').tobytes(),angle
 im.save(OUT/(f'final-turret-{angle:03d}.png' if args.final else f'turret-{angle:03d}.png'));frames.append(im)
 audit.append({'angle':angle,'source_palette_errors':len(actual-colors),'visible_colors':len(actual),'bounds':box,'ink_finish':args.final})
(OUT/('final-validation.json' if args.final else 'palette-audit.json')).write_text(json.dumps(audit,indent=2)+'\n')
previous=Image.open(BASE/args.previous_attempt/(args.previous_prefix+'000.png')).convert('RGBA')
# Compare AREA occupied by extreme colors on the same weapon geometry, not just
# membership in the original palette. No numerical reference-match claim implied.
if (OUT/'weapon-mask-000.png').exists():
 weapon_report=[]
 for angle,im in zip(angles,frames):
  mask=np.array(Image.open(OUT/f'weapon-mask-{angle:03d}.png').convert('RGBA'))[:,:,0]>0
  old=np.array(Image.open(BASE/args.previous_attempt/f'{args.previous_prefix}{angle:03d}.png').convert('RGBA'))
  new=np.array(im);mask&=new[:,:,3]>0
  def stats(a):
   peak=a[:,:,:3].max(axis=2)
   return {'weapon_pixels':int(mask.sum()),'deep_black_pixels':int((mask&(peak<=12)).sum()),'bright_pixels':int((mask&(peak>=110)).sum())}
  weapon_report.append({'angle':angle,'before':stats(old),'after':stats(new)})
 (OUT/'weapon-tone-area-report.json').write_text(json.dumps(weapon_report,indent=2)+'\n')
 mask0=np.array(Image.open(OUT/'weapon-mask-000.png').convert('RGBA'))[:,:,0]>0
 box=Image.fromarray((mask0*255).astype('uint8')).getbbox()
 detail=Image.new('RGB',(336,80),'#77736e');detail_draw=ImageDraw.Draw(detail)
 for i,(patch,label) in enumerate([(source.crop((43,97,123,145)),'Reference'),
                                  (previous.crop(box),args.previous_label),(frames[0].crop(box),'New gun tones')]):
  detail.paste(patch,(i*112+(112-patch.width)//2,18),patch);detail_draw.text((i*112+4,3),label,fill='white')
 detail.resize((2016,480),Image.Resampling.NEAREST).save(OUT/'weapon-detail-6x.png')
comp=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comp)
for i,(im,label) in enumerate([(source.crop((43,58,187,161)),'Reference'),(previous,args.previous_label),(frames[0],args.label)]):
 if i:im=im.crop(im.getbbox())
 comp.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comp.save(OUT/'comparison-native.png');comp.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
fw,fh=frames[0].size
check=Image.new('RGB',(fw*len(angles),fh+24),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip(angles,frames)):
 check.paste(im,(i*fw,0),im);d.text((i*fw+5,fh+5),f'{args.label} {angle} degrees',fill='white')
check.resize((fw*len(angles)*2,(fh+24)*2),Image.Resampling.NEAREST).save(OUT/'rotation-check-2x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
attachment=json.loads((OUT/'attachment.json').read_text());px,py=attachment['bearing_pivot_screen']
preview_scale=attachment.get('native_render_scale',1)
if preview_scale!=1:base=base.resize((round(base.width*preview_scale),round(base.height*preview_scale)),Image.Resampling.NEAREST)
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(frames[0],(round(bx+73*preview_scale-px),round(by+16*preview_scale-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print(json.dumps(audit,indent=2))
