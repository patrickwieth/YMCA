"""Non-destructive reference intake, not a 3D reconstruction or game export."""
from pathlib import Path
from PIL import Image
import hashlib,json,numpy as np
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art-source/gdi ion cannon/heavy blaster 3.png'
OUT=SOURCE.parent/'attempt-01';OUT.mkdir(parents=True,exist_ok=True)
original=SOURCE.read_bytes();image=Image.open(SOURCE).convert('RGBA');a=np.array(image)
colors=sorted(set(image.getdata()))
# Reference appears to be approximately 4x nearest-enlarged pixel art. Keep the
# original authoritative: a few edge pixels do NOT match a perfect 4x grid.
working=image.resize((image.width//4,image.height//4),Image.Resampling.NEAREST)
working.save(OUT/'reference-native-study.png')
working.resize((working.width*3,working.height*3),Image.Resampling.NEAREST).save(OUT/'reference-review-3x.png')
back=working.resize(image.size,Image.Resampling.NEAREST)
report={'status':'reference intake only; no model/render/voxel/game actor yet',
 'source':SOURCE.relative_to(ROOT).as_posix(),'source_sha256':hashlib.sha256(original).hexdigest(),
 'source_size':list(image.size),'alpha_bounds':image.getbbox(),'rgba_colors':len(colors),
 'alpha_values':sorted(set(image.getchannel('A').getdata())),
 'study_size':list(working.size),'working_reference_filter':'nearest',
 '4x_roundtrip_changed_pixels':int(np.any(a!=np.array(back),axis=2).sum()),
 'native_game_scale_goal':1,'rotation_goal':32,
 'components':['stationary armored base','yaw bearing','paired side supports/trunnions',
 'long split barrel shroud','recessed cyan muzzle','rear receiver and top vents',
 'source-cut cyan conduits, side display and capacitor bank'],
 'source_colors':[list(c) for c in colors]}
(OUT/'reference-analysis.json').write_text(json.dumps(report,indent=2)+'\n')
assert SOURCE.read_bytes()==original
print(json.dumps({k:v for k,v in report.items() if k!='source_colors'},indent=2))
