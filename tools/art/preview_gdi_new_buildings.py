"""Native-scale placement preview; does not change or resample supplied buildings."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'mods/ca/bits/gdi/buildings'
calibration=json.loads((ROOT/'tools/gdi-building-alignment.json').read_text())['entries']
offsets={Path(e['File']).name:e['Offset'] for e in calibration}
canvas=Image.new('RGBA',(1152,540),(76,76,72,255));draw=ImageDraw.Draw(canvas)
for i,(filename,bib,label) in enumerate([
 ('weapons factory.png','bib4x4.png','WEAP.TD - Weapons Factory / 4x4 decoration'),
 ('communication center.png','bib3x3.png','EYE - Advanced Communications Center / 3x3 decoration')]):
 cx,cy=i*576+288,350
 for path,(ox,oy) in [(ART/bib,(0,0)),(ART/filename,offsets[filename])]:

  image=Image.open(path).convert('RGBA')
  # Bottom anchor: -height + width/4 + offset, same source-pixel units at Scale 1.
  canvas.alpha_composite(image,(round(cx-image.width/2+ox),round(cy-image.height+image.width/4+oy)))
 draw.text((i*576+16,500),label,fill='white')
 draw.text((i*576+16,518),'Scale 1; source PNG unchanged; gameplay footprint unchanged',fill='white')
out=ROOT/'docs/modular/gdi-new-buildings-preview.png';canvas.convert('RGB').save(out)
print(out)
