"""Source-only texture cuts for the Heavy Blaster geometry study. No painting/noise."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-01/textures';OUT.mkdir(parents=True,exist_ok=True)
p=BASE/'heavy blaster 3.png';original=p.read_bytes();src=Image.open(p).convert('RGBA')
# Corner coordinates in the 256px study, sampled from the authoritative 1024px source.
cuts={
 'barrel-side':([(42,126),(99,98),(101,131),(42,160)],(48,24)),
 'barrel-top':([(20,116),(76,86),(94,98),(37,130)],(48,12)),
 'muzzle':([(19,124),(37,132),(37,159),(19,151)],(16,28)),
 'receiver-side':([(137,66),(171,48),(181,76),(147,95)],(36,28)),
 'display':([(187,76),(202,68),(202,86),(187,94)],(16,20)),
 'side-window':([(123,108),(135,101),(135,110),(123,118)],(16,12)),
 'roof-panel':([(124,49),(143,38),(157,45),(137,58)],(24,16)),
 'base-front':([(102,190),(145,211),(145,224),(102,203)],(44,16)),
 'base-side':([(153,210),(218,176),(218,192),(153,225)],(48,16)),
 'gear':([(52,179),(78,192),(78,216),(52,201)],(28,28)),
 'steel':([(145,174),(159,168),(166,172),(152,179)],(16,8)),
 'support':([(124,143),(135,148),(135,167),(124,163)],(12,24)),
}
report={}
for name,(quad,size) in cuts.items():
 w,h=size;dest=[(0,0),(w,0),(w,h),(0,h)];A=[];B=[]
 for (x,y),(u,v) in zip(dest,quad):
  u*=4;v*=4;A.extend([[x,y,1,0,0,0,-u*x,-u*y],[0,0,0,x,y,1,-v*x,-v*y]]);B.extend([u,v])
 coeff=np.linalg.solve(np.array(A,float),np.array(B,float))
 image=src.transform(size,Image.Transform.PERSPECTIVE,coeff,Image.Resampling.NEAREST)
 image.save(OUT/(name+'.png'));report[name]={'source_quad_256':quad,'size':size}
(BASE/'attempt-01/texture-provenance.json').write_text(json.dumps({'source_sha256':hashlib.sha256(original).hexdigest(),'method':'inverse projective source cuts; nearest; no new painted pixels','cuts':report},indent=2)+'\n')
# Select real source colors nearest the intended material families; retain exact
# source RGBA values instead of relying on ambiguous detail-pixel coordinates.
palette=np.array(sorted({c for c in src.getdata() if c[3]==255}),dtype=np.int32)
roles={(153,91):(180,160,130),(111,137):(130,80,65),(112,150):(28,28,36),
       (155,184):(75,78,90),(156,88):(175,187,192),(164,140):(70,205,245)}
samples={f'{x},{y}':palette[((palette[:,:3]-rgb)**2).sum(axis=1).argmin()].tolist() for (x,y),rgb in roles.items()}
(BASE/'attempt-01/source-samples.json').write_text(json.dumps(samples,indent=2)+'\n')
assert p.read_bytes()==original
