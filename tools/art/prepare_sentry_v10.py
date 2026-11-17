"""Source cuts for the longer stepped rear housing; no generated texture artwork."""
from pathlib import Path
from PIL import Image
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-10';TEX=OUT/'textures';TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png';im=Image.open(source).convert('RGBA')
for path in (BASE/'attempt-08/textures').glob('*.png'):shutil.copyfile(path,TEX/path.name)
# UL, LL, LR, UR. The fence/pole at x184 is background, NOT part of the turret.
cuts={
 'rear-roof-extension':((20,56),(141,63,167,78,180,80,146,61)),
 'rear-upper-grille':((20,10),(169,90,169,98,180,93,180,85)),
 'rear-lower-cover':((20,16),(171,99,171,109,178,106,178,95)),
}
for name,(size,quad) in cuts.items():
 patch=im.transform(size,Image.Transform.QUAD,quad,Image.Resampling.NEAREST)
 if name=='rear-roof-extension':
  # The original main-hatch silhouette crosses this source quad. It must not
  # become an unrelated black seam through the center of the NEW rear plate.
  # Cut those interior silhouette pixels out onto a verbatim roof-metal sample.
  for y in range(4,patch.height-4):
   for x in range(2,patch.width-2):
    if max(patch.getpixel((x,y))[:3])<40:patch.putpixel((x,y),im.getpixel((124,73)))
  # Preserve the original worn return edge as a narrow strip, rather than a
  # flat gray slab edge. This is copied from the rear roof itself, not painted.
  edge=im.transform((3,56),Image.Transform.QUAD,(146,61,180,80,181,83,147,64),Image.Resampling.NEAREST)
  patch.paste(edge,(17,0))
 patch.save(TEX/(name+'.png'))
colors=set(im.getdata())
for path in TEX.glob('*.png'):
 assert {c for c in Image.open(path).convert('RGBA').getdata() if c[3]}<=colors,path
(OUT/'texture-provenance.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),
 'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'inherits':'../attempt-08/texture-provenance.json',
 'rear_cuts':cuts,'rear_roof_edge':{'quad':[146,61,180,80,181,83,147,64],'size':[3,56],'paste':[17,0]},'rear_roof_interior_cutout':{'excluded':'all RGB channels <40, excluding boundary','source_backing_pixel':[124,73]},'sampling':'NEAREST, verbatim source pixels',
 'interpretation':'Visible rear grille and stepped roof reconstructed; background fence/pole explicitly excluded.'},indent=2)+'\n')
print('V10 rear source cuts prepared; exact source colors verified.')
