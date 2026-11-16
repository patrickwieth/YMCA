"""Reserve every decorative-bib tile against construction; retain existing movement masks
except explicitly requested China2x2 bodies. Art/actor centers and exits remain fixed.
Other factions' detailed body collision masks still require individual ground review.
"""
from pathlib import Path
import json,re
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/bib-occupancy';OUT.mkdir(exist_ok=True);validation=ROOT/'.art-validation/footprint-feedback'
entries=json.loads((validation/'before.json').read_text());audit={e['actor']:e for e in json.loads((validation/'before-grid.json').read_text())};rules=['# Bib construction reservation. Movement-only body mask refinement is separate.'];records=[]
for e in entries:
 name=e['Actor'];a=audit[name];assert a['already_aligned'];bx,by=a['proposed_cell_anchor'];nx,ny=map(int,re.search(r'(\d+)x(\d+)',a['bib']).groups());special=name in {'chpower','chpower2','chbarracks'}
 if name=='chbarracks':nx=ny=3
 bib={(bx+x,by+y) for y in range(ny) for x in range(nx)};old={(p['X'],p['Y']):p['Type'] for p in e['FootprintCells']};cells={p:'=' for p in bib}
 if special:
  for y in range(2):
   for x in range(2):cells[(bx+x,by+y)]='x'
 else:
  # No blind conversion of roofs/selection boxes into unit collision cells.
  cells.update({p:t for p,t in old.items() if t!='_'})
 left=min(p[0] for p in cells);right=max(p[0] for p in cells);top=min(p[1] for p in cells);bottom=max(p[1] for p in cells);w=right-left+1;h=bottom-top+1;dx,dy=e['BuildingDimensions'];ox,oy,oz=e['BuildingCenterOffset'];center=[ox-362*(w-h-dx+dy),oy-362*(w+h-dx-dy),oz];foot=' '.join(''.join(cells.get((x,y),'_') for x in range(left,right+1)) for y in range(top,bottom+1))
 rules+=['',e['RuleKey']+':','\tBuilding:',f'\t\tDimensions: {w},{h}',f'\t\tFootprintOffset: {left},{top}','\t\tFootprint: '+foot,'\t\tLocalCenterOffset: '+','.join(map(str,center))]
 if name=='chbarracks':rules+=['\tWithIdleOverlay@CHINABIB:','\t\tImage: sovietbib3x3','\t\tOffset: 0,0,-724']
 records.append({'actor':name,'rule_key':e['RuleKey'],'bib_cells':sorted([list(p) for p in bib]),'dimensions':[w,h],'footprint_offset':[left,top],'footprint':foot,'center_offset':center,'old_dimensions':[dx,dy],'old_center_offset':[ox,oy,oz],'old_cells':e['FootprintCells'],'movement_mask_reviewed':special})
(ROOT/'mods/ca/rules/bib-occupancy.yaml').write_text('\n'.join(rules)+'\n');(OUT/'binding.json').write_text(json.dumps(records,indent=2)+'\n')
print('Bib build-reservations:',len(records),'; explicit2x2-on3x3 movement masks: China Power/Advanced Power/Barracks. Other movement masks retained, not claimed reviewed.')
