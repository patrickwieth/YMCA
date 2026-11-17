"""Both selected studies on native 1x1 bibs, no sprite scaling in this comparison."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2];base=ROOT/'art-source';bib=Image.open(ROOT/'mods/ca/bits/gdi/buildings/bib1x1.png').convert('RGBA');canvas=Image.new('RGBA',(512,280),(119,115,110,255));d=ImageDraw.Draw(canvas)
for col,(folder,label) in enumerate((('gdi aa gun','AA GUN / native / 1x1'),('gdi sonic turret','SONIC TURRET / native / 1x1'))):
 p=base/folder/'pass-05';g=json.loads((p/'geometry.json').read_text());cx=128+col*256;cy=220;canvas.alpha_composite(bib,(cx-64,cy-32));im=Image.open(p/'review/assembly-000.png').convert('RGBA');canvas.alpha_composite(im,(round(cx-g['ground_origin_frame'][0]),round(cy-g['ground_origin_frame'][1])));d.text((col*256+5,8),label,fill='white')
canvas.convert('RGB').save(base/'gdi-small-turrets-native-preview.png')
