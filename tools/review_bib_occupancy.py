"""Native concrete China footprint review: red blocked cells, cyan passable bib."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/bib-occupancy';entries={e['Actor']:e for e in json.loads((ROOT/'.art-validation/footprint-feedback/after.json').read_text())};board=Image.new('RGBA',(1536,480),(65,70,63,255))
for k,name in enumerate(['chpower','chpower2','chbarracks']):
 e=entries[name];tile=Image.new('RGBA',(512,480),(65,70,63,255));cx,cy=256,290
 for layer in e['Bibs']:
  im=Image.open(ROOT/'mods/ca'/layer['File']).convert('RGBA');x,y=map(float,layer['Offset'].split(',')[:2]);tile.alpha_composite(im,(round(cx+x-im.width/2+layer['ScreenOffset'][0]),round(cy+y-im.height+im.width/4+layer['ScreenOffset'][1])))
 dx,dy=e['BuildingDimensions'];lx,ly,lz=e['BuildingCenterOffset'];ax=724+362*(dx-dy)+lx;ay=724+362*(dx+dy-2)+ly-lz;draw=ImageDraw.Draw(tile)
 for c in e['FootprintCells']:
  x=cx+(724+724*(c['X']-c['Y'])-ax)*128/1448;y=cy+(724+724*(c['X']+c['Y'])-ay)*64/1448;color='red' if c['Type']=='x' else 'cyan';draw.line([(x,y-32),(x+64,y),(x,y+32),(x-64,y),(x,y-32)],fill=color,width=2)
 im=Image.open(ROOT/'mods/ca'/e['File']).convert('RGBA');s=float(e['Scale']);x,y=map(float,e['Offset'].split(',')[:2]);px=cx+(x-im.width/2)*s;py=cy+(y-im.height+im.width/4)*s;im=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.NEAREST);tile.alpha_composite(im,(round(px),round(py)));ImageDraw.Draw(tile).text((10,10),name+' / red=blocked, cyan=passable',fill='white');board.alpha_composite(tile,(k*512,0))
board.save(OUT/'china-footprint-review.png')
