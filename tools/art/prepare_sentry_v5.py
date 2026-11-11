"""Build V5 source-only cut/paste patches; preserve prior attempts."""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-05';TEX=OUT/'textures'
TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
im=Image.open(source).convert('RGBA')
for p in (BASE/'attempt-04/textures').glob('*.png'):
 shutil.copyfile(p,TEX/p.name)
# Three distinct source strips, laid side by side for a whole circumference.
# This replaces V4's repetition of the same narrow strip on every drum face.
quads=[(105,136,106,148,119,152,119,141),
       (119,141,119,152,137,155,137,145),
       (141,144,141,153,156,143,156,134)]
atlas=Image.new('RGBA',(96,16))
for i,q in enumerate(quads):
 atlas.paste(im.transform((32,16),Image.Transform.QUAD,q,Image.Resampling.NEAREST),(32*i,0))
atlas.save(TEX/'drum-circumference.png')
# Small metal-edge patches instead of painting scratches or using random noise.
patches={'front-steel-edge':((8,24),(130,92,126,104,131,108,137,96)),
         'rear-vent-face':((12,24),(170,91,170,111,180,109,180,87))}
for name,(size,quad) in patches.items():
 im.transform(size,Image.Transform.QUAD,quad,Image.Resampling.NEAREST).save(TEX/(name+'.png'))
source_colors=set(im.getdata())
for p in TEX.glob('*.png'):
 assert {c for c in Image.open(p).convert('RGBA').getdata() if c[3]} <= source_colors,p
(OUT/'texture-provenance.json').write_text(json.dumps({
 'source':source.relative_to(ROOT).as_posix(), 'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
 'inherits_source_cuts':'../attempt-04/texture-provenance.json',
 'drum_quads':quads,'edge_patches':patches,
 'sampling':'NEAREST; no painted/generated/noise pixels; alpha cutout allowed'},indent=2)+'\n')
print('V5 cut/paste textures: source-color verification passed.')
