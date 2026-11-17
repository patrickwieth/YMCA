"""Preserve pass12; freestand its PNG and correct VXL grid-unit registration in pass13."""
from pathlib import Path
from PIL import Image,ImageDraw
import shutil,json,struct,hashlib,argparse
import numpy as np
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,default=14);a=p.parse_args()
R=Path(__file__).resolve().parents[2];F=R/'art-source/gdi missile tower';old=F/'pass-12';out=F/f'pass-{a.iteration:02d}';out.mkdir(exist_ok=True)
assert not (out/'registration.json').exists(),'Preserve completed passes.'
shutil.copytree(old/'source-cuts',out/'source-cuts',dirs_exist_ok=True)
for n in ['turret.blend','geometry.json','surface-uv.npz','voxel-sampling.json']:
 shutil.copy2(old/n,out/n)
im=Image.open(old/'base.png').convert('RGBA');mask=Image.new('1',im.size)
outline=[(24,47),(32,40),(34,36),(96,36),(104,47),(104,68),(105,77),(105,99),(104,108),(76,127),(48,127),(43,121),(42,114),(23,112),(22,105),(15,104),(15,85),(20,81),(24,78)]
ImageDraw.Draw(mask).polygon(outline,fill=1)
base=Image.new('RGBA',im.size);base.paste(im,(0,0),mask)
# The original machinery enclosed small white background islands; these are now exterior.
from collections import deque
q=deque([(x,y) for x in range(128) for y in (0,127)]+[(x,y) for y in range(128) for x in (0,127)]);seen=set()
while q:
 x,y=q.popleft()
 if (x,y) in seen or not (0<=x<128 and 0<=y<128):continue
 seen.add((x,y));c=base.getpixel((x,y))
 if c[3]!=0 and min(c[:3])<245:continue
 base.putpixel((x,y),(*c[:3],0));q.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
base.save(out/'base.png');base.save(out/'source-cuts/base.png')
base.resize((768,768),Image.Resampling.NEAREST).save(out/'base-6x.png')
manifest=json.loads((out/'source-cuts/manifest.json').read_text());manifest['cuts'].append({'name':'freestanding-building','source_pass':12,'keep_polygon':outline,'pixel_operation':'binary alpha cut; retained RGBA unchanged; external paving/flag removed'})
# Broader, calmer native tonal bands replace the small, conspicuously repeated noisy gold swatch.
source=Image.open(R/manifest['source']).convert('RGBA')
for name,rect in [('gold',(60,103,69,111)),('goldshade',(73,65,78,79))]:
 source.crop(rect).save(out/'source-cuts'/f'{name}.png')
 for c in manifest['cuts']:
  if c['name']==name:c['source_rect']=list(rect);c['size']=[rect[2]-rect[0],rect[3]-rect[1]]
(out/'source-cuts/manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
# Keep one source texel per model unit when donor rectangle dimensions change.
sample=dict(np.load(out/'surface-uv.npz'));roles=json.loads((out/'voxel-sampling.json').read_text())['textures']
for name,old_size,new_size in [('gold',(4,10),(9,8)),('goldshade',(4,6),(5,14))]:
 selected=sample['texture']==roles.index(name);sample['uv'][selected]*=np.array(old_size)/np.array(new_size)
np.savez_compressed(out/'surface-uv.npz',**sample)
(out/'registration.json').write_text(json.dumps({'source_pass':12,'render_scale':.85,'mount_local':[0,11,1527],'base_bearing_pixel':[65,44.5],'base_offset':[0,-16],'density':1.25,'export_fix':'OpenRA slice vertices use raw grid units: bounds must be grid_min/grid_max, limb Scale=1/density. Previously only bounds were divided, leaving geometry 25% oversized and off-center.','texture_scope':'Native source swatches mapped by UV; repeated for inferred surfaces. Not a one-to-one collage of complete source components. Larger coherent gold donor replaces tiny noisy patch; no invented RGB.'},indent=2)+'\n')
print(out)
