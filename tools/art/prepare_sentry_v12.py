"""Source-only texture corrections for the seven-point V11/reference review."""
from pathlib import Path
from PIL import Image
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/soviet-sentry'
OUT=BASE/'attempt-12';TEX=OUT/'textures';TEX.mkdir(parents=True,exist_ok=True)
source=ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png';im=Image.open(source).convert('RGBA')
for path in (BASE/'attempt-10/textures').glob('*.png'):shutil.copyfile(path,TEX/path.name)
# A small directed source scratch, not V6's enlarged orange cluster.
red=Image.new('RGBA',(32,22),im.getpixel((123,107)))
quad=(112,85,112,87,126,91,126,88)
scratch=im.transform((16,3),Image.Transform.QUAD,quad,Image.Resampling.NEAREST)
mask=Image.new('L',scratch.size)
for y in range(scratch.height):
 for x in range(scratch.width):
  r,g,b,a=scratch.getpixel((x,y))
  if r>g*1.25 and r>b*1.15:mask.putpixel((x,y),255)
red.paste(scratch,(7,6),mask)
red.paste(im.crop((124,105,127,108)),(27,17));red.save(TEX/'red-slope.png')
# The actual raised metal cover uses a neutral metal sample behind retained wear.
cover=im.transform((20,8),Image.Transform.QUAD,(108,90,107,93,118,98,120,95),Image.Resampling.NEAREST)
for y in range(cover.height):
 for x in range(cover.width):
  r,g,b,a=cover.getpixel((x,y))
  if min(r,g,b)<49 or max(r,g,b)-min(r,g,b)>25:cover.putpixel((x,y),im.getpixel((124,73)))
cover.save(TEX/'sensor-metal.png')
# Do not bake the source's black neighboring tubes/clamps into the bright tube face.
# Geometry and real shadows already provide those separations.
steel=im.transform((48,3),Image.Transform.QUAD,(73,111,74,112,92,102,91,101),Image.Resampling.NEAREST)
for y in range(steel.height):
 for x in range(steel.width):
  r,g,b,a=steel.getpixel((x,y))
  if min(r,g,b)<65 or max(r,g,b)-min(r,g,b)>25:steel.putpixel((x,y),im.getpixel((124,73)))
steel.save(TEX/'steel-tube.png')
colors=set(im.getdata())
for path in TEX.glob('*.png'):
 assert {c for c in Image.open(path).convert('RGBA').getdata() if c[3]}<=colors,path
(OUT/'texture-provenance.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),
 'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'inherits':'../attempt-10/texture-provenance.json',
 'front_paint':{'fill_pixel':[123,107],'scratch_quad':quad,'scratch_size':[16,3],'paste':[7,6],
 'scratch_mask':'red/warm pixels only','corner_cut':[124,105,127,108],'corner_paste':[27,17]},
 'cover_quad':[108,90,107,93,118,98,120,95],
 'tube_quad':[73,111,74,112,92,102,91,101], 'metal_backing_pixel':[124,73],
 'sampling':'nearest, source cuts and source-pixel backing; no generated texture artwork'},indent=2)+'\n')
print('V12 source texture corrections verified.')
