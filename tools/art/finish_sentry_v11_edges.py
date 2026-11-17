"""Single native-pixel black finishes on major plate edges, using geometry ID masks.
No texture generation/painting: existing sprite pixels plus one sampled source ink.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,argparse
parser=argparse.ArgumentParser()
parser.add_argument('--input-attempt',default='attempt-10')
parser.add_argument('--output-attempt',default='attempt-11')
parser.add_argument('--before-label',default='V10')
parser.add_argument('--after-label',default='V11 / 1px ink edges')
parser.add_argument('--paired-highlights',action='store_true',help='Add one source-gray pixel inside upper/left black plate edges.')
parser.add_argument('--weapon-tone-reference-attempt',default=None,help='Same-geometry predecessor for thinning existing weapon contrast bands.')
parser.add_argument('--weapon-tone-reference-prefix',default='final-turret-')
parser.add_argument('--angles',default='0,45,180,315')
options=parser.parse_args();angles=tuple(map(int,options.angles.split(',')))
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry';OUT=BASE/options.output_attempt
INPUT=BASE/options.input_attempt
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
source_colors=set(source.getdata());ink=source.getpixel((75,115));assert ink==(3,2,1,255)
frames=[];reports=[]
directions=[(-1,0),(0,1),(1,0),(0,-1)]
for angle in angles:
 original=Image.open(INPUT/f'turret-{angle:03d}.png').convert('RGBA');a=np.array(original);result=a.copy()
 ids=np.array(Image.open(OUT/f'part-ids-{angle:03d}.png').convert('RGBA'))[:,:,0].copy()
 solid=a[:,:,3]==255;ids[~solid]=0
 assert set(np.unique(ids))<={0,1,30,40,50,55,60,65,70,75,80,90},set(np.unique(ids))
 h,w=ids.shape;edges=np.zeros((h,w),dtype=bool);inward={}
 for y,x in zip(*np.where(solid&(ids>=30))):
  # Original dark strokes already finish the part; do not thicken them.
  if max(a[y,x,:3])<=28:continue
  owner=int(ids[y,x]);boundaries=[];already_inked=False
  for dy,dx in directions:
   yy,xx=y+dy,x+dx
   if not(0<=yy<h and 0<=xx<w):continue
   if ids[yy,xx]<owner:
    boundaries.append((dy,dx))
    if solid[yy,xx] and max(a[yy,xx,:3])<=28:already_inked=True
  if boundaries and not already_inked:
   edges[y,x]=True;inward[(y,x)]=boundaries
 # Keep a bright metal edge immediately INSIDE its new black finish where there
 # is room, by moving the existing source-colored highlight one pixel inward.
 # Never overwrite a rivet, emblem, existing black line or another contour.
 moved=0;destinations=set()
 for (y,x),normals in inward.items():
  rgb=a[y,x,:3].astype(int)
  if min(rgb)<94 or max(rgb)-min(rgb)>25:continue
  for dy,dx in normals:
   yy,xx=y-dy,x-dx
   if not(0<=yy<h and 0<=xx<w) or edges[yy,xx] or (yy,xx) in destinations:continue
   neighbor=a[yy,xx,:3].astype(int)
   if ids[yy,xx]!=ids[y,x] or max(neighbor)-min(neighbor)>25 or min(neighbor)<40:continue
   if sum(neighbor)>=sum(rgb):continue
   result[yy,xx]=a[y,x];destinations.add((yy,xx));moved+=1;break
 result[edges]=ink
 paired=np.zeros((h,w),dtype=bool)
 if options.paired_highlights:
  # Original black + light edge pair, not a broad white stroke. Only upper/left
  # plate boundaries catch this line; painted surfaces, emblems and gun parts do not.
  metal_groups={50,55,60,65,70,75,80,90}
  light_steps=[source.getpixel(p) for p in ((133,124),(124,73),(130,117))]
  for y,x in zip(*np.where(solid & np.isin(ids,list(metal_groups)))):
   owner=int(ids[y,x])
   for dy,dx in ((-1,0),(0,-1)):
    yy,xx=y+dy,x+dx
    if not(0<=yy<h and 0<=xx<w) or ids[yy,xx]>=owner:continue
    if max(result[y,x,:3])<=28:
     ty,tx=y-dy,x-dx
    elif solid[yy,xx] and max(result[yy,xx,:3])<=28:
     ty,tx=y,x
    else:continue
    if not(0<=ty<h and 0<=tx<w) or ids[ty,tx]!=owner or paired[ty,tx]:continue
    rgb=result[ty,tx,:3].astype(int)
    if min(rgb)<35 or max(rgb)-min(rgb)>25 or min(rgb)>=94:continue
    iy,ix=ty-dy,tx-dx
    if 0<=iy<h and 0<=ix<w and ids[iy,ix]==owner:
     inside=result[iy,ix,:3].astype(int)
     # Do not broaden a pre-existing bright bevel into a second bright row.
     if min(inside)>=94 and max(inside)-min(inside)<=25:continue
    replacement=light_steps[0 if max(rgb)<=55 else 1 if max(rgb)<=80 else 2]
    if sum(replacement[:3])<=sum(rgb):continue
    result[ty,tx]=replacement;paired[ty,tx]=True
 weapon_finish={}
 if options.weapon_tone_reference_attempt:
  reference=np.array(Image.open(BASE/options.weapon_tone_reference_attempt/f'{options.weapon_tone_reference_prefix}{angle:03d}.png').convert('RGBA'))
  assert np.array_equal(reference[:,:,3],a[:,:,3]),'Weapon tone reference requires identical geometry/silhouette'
  weapon_ids=np.array(Image.open(INPUT/f'weapon-mask-{angle:03d}.png').convert('RGBA'))[:,:,0]
  weapon=(weapon_ids>0)&solid
  peak=reference[:,:,:3].max(axis=2);dark=weapon&(peak<=12);bright=weapon&(peak>=110)
  up=lambda m:np.pad(m[:-1],((1,0),(0,0)))
  left=lambda m:np.pad(m[:,:-1],((0,0),(1,0)))
  # Keep a single upper/left boundary of old dark bands plus the old exterior
  # contour, but leave broad band interiors as the new middle-gray metal.
  thin_dark=dark&((~up(dark)&up(weapon))|(~left(dark)&left(weapon)))
  exterior=np.zeros_like(solid)
  for dy,dx in directions:exterior|=~np.roll(solid,(dy,dx),(0,1))
  thin_dark|=dark&exterior
  thin_dark|=dark&(weapon_ids==2)  # genuine bores are cavities, not metal bands
  result[thin_dark]=reference[thin_dark]
  # The upper boundary of each OLD bright strip becomes one modest source-gray
  # pixel; only a few directly ink-adjacent spots retain the brighter source gray.
  thin_bright=bright&~up(bright)
  result[thin_bright]=source.getpixel((130,117))  # 101,96,92
  accent=thin_bright&up(thin_dark)
  result[accent]=source.getpixel((78,115))       # 121,113,105
  weapon_finish={'old_dark_pixels':int(dark.sum()),'retained_dark_contour_pixels':int(thin_dark.sum()),
   'old_bright_pixels':int(bright.sum()),'thin_glint_pixels':int(thin_bright.sum()),'strong_glint_pixels':int(accent.sum())}
 image=Image.fromarray(result)
 assert image.getchannel('A').tobytes()==original.getchannel('A').tobytes()
 colors={c for c in image.getdata() if c[3]};assert colors<=source_colors
 bbox=image.getbbox();assert min(bbox[:2])>0 and bbox[2]<w and bbox[3]<h
 image.save(OUT/f'turret-{angle:03d}.png');frames.append(image)
 # Diagnostic only: white marks the new single-pixel ink positions.
 diagnostic=np.zeros_like(result);diagnostic[edges]=(255,255,255,255)
 Image.fromarray(diagnostic).save(OUT/f'new-edge-mask-{angle:03d}.png')
 reports.append({'angle':angle,'new_ink_pixels':int(edges.sum()),'highlights_moved_inward':moved,
 'paired_highlight_pixels':int(paired.sum()),'weapon_finish':weapon_finish,
 'changed_sprite_pixels':int(np.any(result!=a,axis=2).sum()),'source_palette_errors':len(colors-source_colors),
 'alpha_unchanged':True,'bounds':bbox})
(OUT/'edge-finish-report.json').write_text(json.dumps({'ink_source_pixel':[75,115],'ink_rgba':ink,
 'line_width':'one native output pixel, one owner per shared boundary',
 'small_gun_parts_excluded':True,'paired_highlights':options.paired_highlights,'weapon_tone_reference':options.weapon_tone_reference_attempt,'views':reports},indent=2)+'\n')
old=Image.open(INPUT/'turret-000.png').convert('RGBA')
comparison=Image.new('RGB',(576,152),'#77736e');d=ImageDraw.Draw(comparison)
for i,(im,label) in enumerate([(source.crop((43,58,187,161)),'Reference'),(old,options.before_label),(frames[0],options.after_label)]):
 if i:im=im.crop(im.getbbox())
 comparison.paste(im,(i*192+(192-im.width)//2,10),im);d.text((i*192+5,132),label,fill='white')
comparison.save(OUT/'comparison-native.png');comparison.resize((1728,456),Image.Resampling.NEAREST).save(OUT/'comparison-3x.png')
fw,fh=frames[0].size
check=Image.new('RGB',(fw*len(angles),fh+24),'#77736e');d=ImageDraw.Draw(check)
for i,(angle,im) in enumerate(zip(angles,frames)):
 check.paste(im,(i*fw,0),im);d.text((i*fw+5,fh+5),f'{options.after_label} {angle} degrees',fill='white')
check.resize((fw*len(angles)*2,(fh+24)*2),Image.Resampling.NEAREST).save(OUT/'rotation-check-2x.png')
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/sentry base.png').convert('RGBA')
attachment=INPUT/'attachment.json'
if not attachment.exists():attachment=BASE/'attempt-05/attachment.json'
attachment=json.loads(attachment.read_text());px,py=attachment['bearing_pivot_screen']
preview_scale=attachment.get('native_render_scale',1)
if preview_scale!=1:base=base.resize((round(base.width*preview_scale),round(base.height*preview_scale)),Image.Resampling.NEAREST)
assembly=Image.new('RGBA',(256,256));bx,by=(256-base.width)//2,155
assembly.alpha_composite(base,(bx,by));assembly.alpha_composite(frames[0],(round(bx+73*preview_scale-px),round(by+16*preview_scale-py)))
assembly.save(OUT/'base-assembly-preview.png');assembly.resize((768,768),Image.Resampling.NEAREST).save(OUT/'base-assembly-preview-3x.png')
print(json.dumps(reports,indent=2))
