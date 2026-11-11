"""V6-based source-only colors and copied fasteners. Never modifies supplied art."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-08';TEX=OUT/'textures';TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png'
im=Image.open(source).convert('RGBA')
# Keep V6's wear, not V7's quiet replacement panels. Include its older packed cuts.
for attempt in ('attempt-04','attempt-05','attempt-06'):
 for path in (BASE/attempt/'textures').glob('*.png'):shutil.copyfile(path,TEX/path.name)
cuts={'recessed-rivet':(128,116,133,122),'side-rivet':(138,125,143,132),
      'rear-side-rivet':(160,116,165,123)}
for name,rect in cuts.items():im.crop(rect).save(TEX/(name+'.png'))
# Put the original recessed fastener into the already weathered front edge.
# It is a copied 5x6 source cluster, not an invented circle or painted highlight.
edge=Image.open(TEX/'front-edge.png').convert('RGBA')
edge.paste(im.crop(cuts['recessed-rivet']),(1,22));edge.save(TEX/'front-edge.png')
# Named color samples come from the corresponding ORIGINAL turret parts.
samples={'armor':(124,73),'side_metal':(133,124),'side_shadow':(146,118),
 'rivet_center':(130,118),'rivet_edge':(130,117),'upper_bearing':(119,135),
 'upper_bearing_shadow':(116,134),'lower_bearing_edge':(120,156),
 'gun_highlight':(78,115),'gun_shade':(72,116),'outline':(75,115),
 'red_paint':(118,144),'red_shadow':(160,140)}
swatches={name:{'pixel':xy,'rgba':im.getpixel(xy)} for name,xy in samples.items()}
# Explicit allowed palette is verbatim SOURCE, not a visually similar replacement palette.
source_colors=set(im.getdata());used=set()
for path in TEX.glob('*.png'):
 colors={c for c in Image.open(path).convert('RGBA').getdata() if c[3]}
 assert colors<=source_colors,path
 used|=colors
provenance={'source':source.relative_to(ROOT).as_posix(),
 'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'base':'attempt-06',
 'inherits':['../attempt-04/texture-provenance.json','../attempt-05/texture-provenance.json','../attempt-06/texture-provenance.json'],
 'rivet_crops':cuts,'front_edge_paste':{'cut':'recessed-rivet','position':[1,22]},
 'swatches':swatches,'source_rgba_palette':sorted(c for c in source_colors if c[3]==255),
 'new_texture_colors':len(used-source_colors),'sampling':'NEAREST; no lighting tint, painting, noise or generated image content'}
(OUT/'texture-provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
sheet=Image.new('RGB',(480,160),'#77736e');d=ImageDraw.Draw(sheet)
for i,(name,rect) in enumerate(cuts.items()):
 patch=im.crop(rect).resize((80,112),Image.Resampling.NEAREST)
 sheet.paste(patch,(i*160+12,26),patch);d.text((i*160+4,4),name,fill='white')
sheet.save(OUT/'original-rivet-cuts.png')
print('V8: original colors verified; V6 wear preserved; source rivets extracted.')
