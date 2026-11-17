"""Use full-scene native colors with independently rendered holdout alpha.
Avoid tiny EEVEE texture/depth differences between separate draw passes. Preserve
independent renders for auditing; no reference image projection or alpha edits.
"""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,shutil

def assemble(directory):
 p=Path(directory);meta=json.loads((p/'material-report.json').read_text());fixed=meta['fixed_part_ids'];base=np.array(Image.open(p/'base-raw.png').convert('RGBA'));backup=p/'independent-parts';backup.mkdir(exist_ok=True);report=[]
 for i in range(32):
  target=p/f'turret-{i:02d}-raw.png';original=backup/target.name
  if not original.exists():shutil.copy2(target,original)
  part=np.array(Image.open(original).convert('RGBA'));full=np.array(Image.open(p/f'native-full-{i:02d}-raw.png').convert('RGBA'));ids=np.array(Image.open(p/f'native-full-{i:02d}-ids.png')).astype(int);code=ids[:,:,0]+256*ids[:,:,1]
  visible=part[:,:,3]>0;full_moving=(full[:,:,3]>0)&~np.isin(code,fixed)
  assert np.array_equal(visible,full_moving),(i,'Independent holdout / full-scene part mask disagreement')
  fixed_visible=(full[:,:,3]>0)&~visible
  assert np.array_equal(base[fixed_visible],full[fixed_visible]),(i,'Fixed colors are not stable across full poses')
  differences=int(np.any(part[visible,:3]!=full[visible,:3],axis=1).sum());part[visible,:3]=full[visible,:3]
  Image.fromarray(part).save(target)
  assert Image.alpha_composite(Image.fromarray(base),Image.fromarray(part)).tobytes()==full.tobytes(),i
  report.append({'facing':i,'independent_color_differences':differences,'independent_alpha_preserved':True,'full_composite_exact':True,'full_render_sha256':hashlib.sha256((p/f'native-full-{i:02d}-raw.png').read_bytes()).hexdigest()})
 (p/'native-part-color-audit.json').write_text(json.dumps({'method':'Full native scene RGB with independently verified holdout alpha; independent colors archived, no alpha edits','frames':report},indent=2)+'\n')
