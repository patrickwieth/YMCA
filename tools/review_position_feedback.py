"""Native resolved composites; never paint runtime/source artwork."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,math
ROOT=Path(__file__).resolve().parents[1];entries={e['Actor']:e for e in json.loads((ROOT/'.art-validation/position-feedback/geometry-after.json').read_text())};OUT=ROOT/'art-source/nod-bib-followup'
def panel(e,title):
 tile=Image.new('RGBA',(512,480),(68,71,68,255));cx,cy=256,340
 for layer in e['Bibs']+[e]:
  p=ROOT/'mods/ca'/layer['File'];anchor=layer['Anchor'];offset=layer['Offset']
  if e['Actor']=='silo.scrin' and layer is e:p=ROOT/'art-source/scrin-rear-alignment/silo/scrinsilo-0012.png';anchor='Center'
  if not p.exists():continue
  im=Image.open(p).convert('RGBA');s=float(layer['Scale']);ox,oy=map(float,offset.split(',')[:2]);x=(ox-im.width/2)*s;y=(oy-im.height+im.width/4)*s if anchor=='Bottom' else (oy-im.height/2)*s;x+=layer.get('ScreenOffset',[0,0])[0];y+=layer.get('ScreenOffset',[0,0])[1];im=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.NEAREST);tile.alpha_composite(im,(round(cx+x),round(cy+y)))
 ImageDraw.Draw(tile).text((8,8),title,fill='white');return tile
old={e['Actor']:e for e in json.loads((OUT/'geometry-before.json').read_text())};board=Image.new('RGBA',(1024,960),(68,71,68,255))
for i,n in enumerate(['weap.nod','afac.nod']):board.alpha_composite(panel(old[n],n+' BEFORE'),(0,i*480));board.alpha_composite(panel(entries[n],n+' AFTER'),(512,i*480))
board.save(OUT/'nod-before-after.png')
scrin=json.loads((ROOT/'art-source/scrin-rear-alignment/before.json').read_text());board=Image.new('RGBA',(2048,math.ceil(len(scrin)/4)*480),(68,71,68,255))
for i,e in enumerate(scrin):board.alpha_composite(panel(entries[e['Actor']],e['Actor']+' AFTER'),(i%4*512,i//4*480))
board.save(ROOT/'art-source/scrin-rear-alignment/after.png')
