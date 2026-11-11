"""Cut/paste-only per-plate textures. No painting, procedural noise or image generation."""
from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/soviet-sentry/attempt-04'
TEX=OUT/'textures'
TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
im=Image.open(source).convert('RGBA')
# UL, LL, LR, UR source corners. Nearest-neighbour perspective rectification.
patches={
 'side_plate':((32,40),(140,102,139,134,165,122,166,93)),
 'roof_plate':((40,28),(115,73,139,87,162,78,137,63)),
 'roof_rear':((32,12),(141,64,164,78,178,82,156,67)),
 'front_red':((24,16),(114,84,107,96,126,103,132,90)),
 'drum_red':((24,16),(108,137,108,148,139,155,139,145)),
 'rear_equipment':((12,24),(172,93,172,112,179,109,179,89)),
 'shoulder_metal':((10,28),(108,82,99,99,103,102,113,85)),
 'steel_strip':((24,4),(66,113,67,117,90,105,89,102)),
}
for name,(size,quad) in patches.items():
    patch=im.transform(size,Image.Transform.QUAD,quad,Image.Resampling.NEAREST)
    patch.save(TEX/(name+'.png'))
    assert set(patch.getdata())<=set(im.getdata()),name
# Separate cut-out emblem: preserve source color pixels, mask surrounding metal.
panel=Image.open(TEX/'side_plate.png').convert('RGBA')
emblem=panel.copy()
for y in range(panel.height):
 for x in range(panel.width):
  r,g,b,a=panel.getpixel((x,y))
  if not (r>70 and r>g*1.35 and r>b*1.6):emblem.putpixel((x,y),(0,0,0,0))
emblem.save(TEX/'emblem-cutout.png')
# Keep emblem separate from the plate; paste a source metal pixel where it was.
metal=im.getpixel((146,118))
for y in range(panel.height):
 for x in range(panel.width):
  if emblem.getpixel((x,y))[3]:panel.putpixel((x,y),metal)
panel.save(TEX/'side-metal.png')
assert set(panel.getdata())<=set(im.getdata())
(OUT/'texture-provenance.json').write_text(json.dumps({'source':str(source.relative_to(ROOT)),
 'patches':patches,'emblem':'source red/orange pixel cutout; alpha mask only',
 'side_metal_fill_source_pixel':[146,118],'sampling':'NEAREST only'},indent=2)+'\n')
canvas=Image.new('RGBA',(320,160))
for i,p in enumerate(sorted(TEX.glob('*.png'))):
 patch=Image.open(p);canvas.paste(patch,((i%5)*64,(i//5)*64))
canvas.resize((640,320),Image.Resampling.NEAREST).save(OUT/'texture-cuts-2x.png')
print('V4 textures prepared; source pixel provenance verified.')
