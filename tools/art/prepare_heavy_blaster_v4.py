"""Source-only colors and nearest texture LOD for the V4 comparison."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-04';OUT.mkdir(exist_ok=True)
path=BASE/'heavy blaster 3.png';before=path.read_bytes();src=Image.open(path).convert('RGBA')
src.resize((128,128),Image.Resampling.NEAREST).save(OUT/'source-native-lod.png')
palette=np.array(sorted({c[:3] for c in src.getdata() if c[3]==255}),dtype=np.int32)
roles={'steel-light':(180,191,196),'steel-mid':(123,136,145),'steel-dark':(74,81,91),'tan':(176,155,125),'copper':(130,85,65)}
colors={name:palette[((palette-rgb)**2).sum(axis=1).argmin()].tolist() for name,rgb in roles.items()}
(OUT/'material-source.json').write_text(json.dumps({'source_sha256':hashlib.sha256(before).hexdigest(),'colors':colors,'lod':'128px nearest source sample; material LOD only, never sprite resampling'},indent=2)+'\n')
assert path.read_bytes()==before
