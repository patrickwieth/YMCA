"""Plan-only audit: resolved decorative bib planes against the actual128x64 grid.
No runtime changes. Moving only bibs would destroy accepted building registration;
changing Building.LocalCenterOffset would also move gameplay centers/docking.
"""
from pathlib import Path
from PIL import Image
import json,re,math,sys
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/bib-grid-alignment'
input_path=Path(sys.argv[1]) if len(sys.argv)>1 else OUT/'resolved-before.json'
output_path=Path(sys.argv[2]) if len(sys.argv)>2 else OUT/'grid-audit.json'
entries=json.loads(input_path.read_text());scrin=json.loads((ROOT/'art-source/scrin bib/cut-paste-manifest.json').read_text());surfaces={e['name']:e['top_surface_center'] for e in scrin['outputs']};result=[]
for e in entries:
 dx,dy=e['BuildingDimensions'];lx,ly,lz=e['BuildingCenterOffset'];actor=(724+362*(dx-dy)+lx,724+362*(dx+dy-2)+ly-lz)
 for bib in e['Bibs']:
  match=re.search(r'(\d+)x(\d+)',bib['Image'])
  if not match:result.append({'actor':e['Actor'],'bib':bib['Image'],'needs_manual_plane_definition':True});continue
  nx,ny=map(int,match.groups());file=ROOT/'mods/ca'/bib['File']
  with Image.open(file) as im:w,h=im.size
  cx,cy=surfaces[file.name] if bib['Image'].startswith('scrinbib') else (w/2,w/4) if bib['Image'].startswith('nodbib') else (w/2,h/2)
  ox,oy=map(float,bib['Offset'].split(',')[:2]);scale=float(bib['Scale']);wx,wy,wz=bib['WorldOffset']
  px=(cx-w/2+ox)*scale;py=(cy-h+(w/4)+oy)*scale if bib['Anchor']=='Bottom' else (cy-h/2+oy)*scale
  x=actor[0]+wx+px*1448/128;y=actor[1]+wy-wz+py*1448/64
  # Center of a rectangle of nx*ny cells rooted at integer cell(i,j).
  ax=724+362*(nx-ny);ay=362*(nx+ny)
  u=((x-ax)+(y-ay))/1448;v=((y-ay)-(x-ax))/1448;choices=[]
  for i in range(math.floor(u)-1,math.ceil(u)+2):
   for j in range(math.floor(v)-1,math.ceil(v)+2):
    sx=ax+724*(i-j);sy=ay+724*(i+j);px=(sx-x)*128/1448;py=(sy-y)*64/1448;choices.append((px*px+py*py,abs(px),-py,i,j,px,py,sx-x,sy-y))
  _,_,_,i,j,px,py,wx,wy=min(choices)
  result.append({'actor':e['Actor'],'bib':bib['Image'],'grid_anchor_fraction':[u,v],'already_aligned':abs(px)<.1 and abs(py)<.1,'proposed_cell_anchor':[i,j],'proposed_screen_translation':[round(px,6),round(py,6)],'proposed_world_translation':[round(wx,6),round(wy,6),0],'scale_matches_declared_cells':abs(w*scale-(nx+ny)*64)<.01,'applied':False})
output_path.write_text(json.dumps(result,indent=2)+'\n')
print('Audit only. Already aligned:',sum(e.get('already_aligned',False) for e in result),'of',len(result))
print('No actor, bib, footprint, center, exit, docking or weapon changed.')
