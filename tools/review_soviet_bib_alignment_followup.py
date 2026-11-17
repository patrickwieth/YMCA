"""Composed native before/after, using engine-resolved body/bib offsets (not live capture)."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
from calibrate_gdi_building_alignment import ROOT,image_origin
OUT=ROOT/'art-source/soviet-bib-alignment';old=json.loads((OUT/'geometry-before.json').read_text());new=json.loads((OUT/'geometry-after.json').read_text())
for outlines in (False,True):
 sheet=Image.new('RGBA',(1120,880),(64,68,62,255));d=ImageDraw.Draw(sheet)
 for row in range(2):
  for col,entry in enumerate((old[row],new[row])):
   x=280+560*col;y=280+440*row;im=Image.open(ROOT/'mods/ca'/entry['File']).convert('RGBA');b=entry['Bibs'][0];bib=Image.open(ROOT/'mods/ca'/b['File']).convert('RGBA');bx,by=image_origin(bib,b['Offset'],b['Anchor']);ix,iy=image_origin(im,entry['Offset'],entry['Anchor'])
   sheet.alpha_composite(bib,(round(x+bx+b['ScreenOffset'][0]),round(y+by+b['ScreenOffset'][1])));sheet.alpha_composite(im,(round(x+ix),round(y+iy)))
   if outlines:
    for points,color in [(entry['GroundCorners'],'#e3b249'),(entry['SelectionPolygon'],'#63ef91')]:d.line([(round(x+u),round(y+v)) for u,v in points+[points[0]]],fill=color)
   d.line((x-4,y,x+4,y),fill='#ffdd66');d.line((x,y-4,x,y+4),fill='#ffdd66')
   d.text((560*col+12,440*row+8),f'{entry["Actor"]} / {"BEFORE" if col==0 else "AFTER"} / {entry["Offset"]}',fill='white')
 sheet.save(OUT/('before-after-selection.png' if outlines else 'before-after.png'))
print(OUT/'before-after.png')
