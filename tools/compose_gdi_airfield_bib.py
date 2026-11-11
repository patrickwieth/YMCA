"""Compose a native 4x5 decorative bib from two whole, unscaled supplied 4x4 bibs."""
from pathlib import Path
from PIL import Image
import argparse,hashlib,json
ROOT=Path(__file__).resolve().parents[1]
def compose():
 source=ROOT/'mods/ca/bits/gdi/buildings/bib4x4.png'
 with Image.open(source) as image:im=image.convert('RGBA')
 assert im.size==(512,256)
 canvas=Image.new('RGBA',(576,288));canvas.alpha_composite(im,(64,0));canvas.alpha_composite(im,(0,32))
 assert {c for c in canvas.getdata() if c[3]}<={c for c in im.getdata() if c[3]}
 return source,canvas
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--check',action='store_true');args=p.parse_args()
 source,canvas=compose();output=source.with_name('bib4x5.png');before=hashlib.sha256(source.read_bytes()).hexdigest()
 if args.check:
  with Image.open(output) as actual:assert actual.convert('RGBA').tobytes()==canvas.tobytes()
 else:
  canvas.save(output)
  (ROOT/'tools/gdi-bib4x5-provenance.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),
   'source_sha256':before,'output':output.relative_to(ROOT).as_posix(),'size':[576,288],'copies':[[64,0],[0,32]],
   'method':'Unscaled full-image copies, one cell apart; no recoloring or new painted pixels.'},indent=2)+'\n')
 assert hashlib.sha256(source.read_bytes()).hexdigest()==before
 print('GDI 4x5 source-copy composition verified; original bib unchanged.')
