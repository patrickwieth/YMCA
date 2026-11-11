"""Install the reviewed broader V5 export; all earlier models/exports stay intact."""
from pathlib import Path
import shutil,json,hashlib
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'art-source/gdi ion cannon/attempt-05/export';OUT=ROOT/'mods/ca/bits/gdi/buildings/experimental';OUT.mkdir(parents=True,exist_ok=True)
r=json.loads((SRC/'binding.json').read_text());r['actor']='ion_defense';r['image']='ion_defense.native';r['gameplay_footprint']='unchanged 2x2';r['assets']={}
for name in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):
 shutil.copyfile(SRC/name,OUT/name);r['assets'][name]=hashlib.sha256((SRC/name).read_bytes()).hexdigest()
d=SRC.parent/'integration';d.mkdir(exist_ok=True);(d/'binding.json').write_text(json.dumps(r,indent=2)+'\n')
print('Ion Defense: broader V5, native 320px, original visible base perimeter installed.')
