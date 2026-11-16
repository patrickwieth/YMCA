"""Cell-level ground-contact review. Explicit masks, never alpha/roof/selection-box inference.
Coordinates are relative to Actor.Location; review PNGs label those exact coordinates.
"""
import json, math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/bib-occupancy/ground-review'
entries=json.loads((OUT/'baseline-geometry.json').read_text())
prior={e['actor']:e for e in json.loads((ROOT/'art-source/bib-occupancy/binding.json').read_text())}
plans={}
def mask(names, origin, rows, note):
 for name in names.split():plans[name]={'origin':origin,'rows':rows.split(),'note':note}
# Each pattern was compared with native, labelled ground-review panels.
mask('apwr atek dome hpad orep pdox powr weat', (0,0),'xx xx','Raised enclosed two-by-two body; front bib border stays clear.')
mask('afld.allies',(0,0),'xxx ===','User correction: control tower and both upper parked-aircraft cells1,0 and2,0 are impassable; runway and remaining apron stay passable.')
mask('fact fact.nukular',(0,0),'xxX xxx XxX','Hall, crane footing and raised construction machinery occupy the three-by-three yard.')
mask('gap',(0,0),'xx xx','User correction: all four cells of the two-by-two Gap Generator body are impassable; surrounding bib remains passable.')
mask('fix',(0,0),'xx x+','Rear workshop/arms solid; central service tile is the sole repair destination.')
mask('tent',(0,0),'xxx ===','The two barracks halls stand on the rear row; front doors/apron remain open.')
mask('weap',(0,0),'xxxx xxxx ====','Explicit user correction: all four lower-left/front bib cells are normally passable, not transit-only; unchanged production exit remains open.')
mask('proc',(0,0),'xx xx','Explicit user correction: two-by-two refinery body; complete lower-left/right bib borders passable, no external docking reservation.')
# GDI positions include the previously approved body-only translations.
mask('afac afac.nukular',(-1,0),'xxx xxx xxx','Three-wide construction hall/crane field, shifted to its actual registered rear corner.')
mask('afld.gdi',(0,-1),'=x== xx== xx=x =x== ====','User correction: remove both extra top cells; hangars/control building, radar footing3,1 and parked aircraft cells1,0 /1,1 /1,2 solid; remaining borders walkable; landing at runway cell2,1.')
mask('eye hq nuke nuk2 hpad.td',(-1,0),'xx xx','Two-by-two ground body at the actual left/rear slab anchor.')
mask('gtek',(0,0),'xx xx','Tower and front cooling housings form the two-by-two ground body.')
mask('patr',(0,0),'xx xx','Launcher building and attached control tower; not the elevated missile.')
mask('proc.td',(0,0),'xx xx','Explicit user correction: two-by-two GDI refinery body solid; remaining bib border passable, docking unchanged.')
mask('pyle',(-1,0),'xx xx','Two halls; existing infantry exits remain in the front bib row.')
mask('rep',(0,0),'xx x+','Workshop sides solid, open central repair court is the service destination.')
mask('upgc',(-1,-1),'xx xx','Explicit user request: two-by-two body on the rear four cells of the three-by-three bib.')
mask('weap.td',(-1,-1),'xxx xxx xxx','User correction: three-by-three solid body; entire surrounding bib reserved against construction but passable, including the existing production exit.')
# Nod: keep supplied ground contacts; do not project tall towers onto extra cells.
mask('afac.nod',(0,0),'xxx xxx xxx','Existing three-by-three construction machinery footprint agrees with supplied art.')
mask('bio.nod hpad.td.nod hq.nod nuk2.nod nuke.nod sgen',(0,0),'xx xx','Native two-by-two raised ground base, with walkable slab border.')
mask('hand',(0,0),'xx xx','Explicit user correction: two-by-two impassable Hand of Nod; existing bib and artwork unchanged.')
mask('mslo.nod',(0,0),'xxx xxx xxx','Raised fenced silo platform occupies the slab, not just the central missile housing.')
mask('proc.td.nod',(0,0),'xxx x== ===','Rear machinery and left processing wing; middle/front delivery court remains open.')
mask('rep.nod',(0,0),'x= x+','Workshop on the left, designated service platform at1,1 on the right.')
mask('tmpl',(0,0),'xxx xxx','Temple and side towers occupy the rear two rows; stairs/front apron remain passable.')
mask('weap.nod',(-1,-1),'xxxx x+xx =+==','User correction: rightmost two bib cells2,-1 and2,0 solid; bottom cell2,1 passable again. Central production lane and existing exit unchanged.')
# China.
mask('chpower chpower2',(-1,0),'xx xx','Approved upper two-by-two body; the authoritative art was raised32px in the preceding pass.')
mask('chbarracks',(0,0),'xx xx','Explicit upper two-by-two body on three-by-three bib; no new body translation.')
mask('chairport',(0,-1),'xxx= ==== ==== ====','Control tower and terminal are behind the runway; prior one-cell mask missed the terminal.')
mask('chcommand chcommand.nukular',(0,0),'xxx xxx +xx','Raised three-by-three command complex; front-left gate0,2 retains the existing unit exit, slab border free.')
mask('chinternetcenter',(-1,-1),'xx== xxx= =xx= ====','Explicit user correction: lower-left/right bib strips and one additional adjacent side cell above each strip remain passable.')
mask('chpropaganda chsilo',(0,-1),'xxx xxx xxx','Raised walled complex fills the three-by-three slab including rear buildings.')
mask('chsupply',(0,0),'xxx xx= x==','Storehouse/silos and left frontage solid; loading stairs/front delivery access remain open.')
mask('chweap',(0,0),'xxxx x=xx','Latest user correction: four-by-two body except the existing free courtyard cell1,1; remaining bib and all production exits stay passable.')
mask('chbunker chbunker.advinf chbunker.ai chbunker2 chbunker2.advinf',(0,0),'xx xx','Existing two-by-two physical enclosure retained; mines and actor origin unchanged.')
# Scrin: source roots define the ground body, not crowns, antennae or floating coils.
mask('grav rift',(-1,-1),'xx xx','Root cluster on rear two-by-two slab cells; tall upper housing does not occupy front bib.')
mask('mani',(0,0),'xx xx','Rooted field-manipulator fills its two-by-two slab.')
mask('nerv',(-1,0),'xx xx','Explicit user request: two-by-two nerve-center body and clear front/side three-by-three bib border.')
mask('port',(0,0),'xx ==','Explicit user request: two-by-one portal, two-by-two bib, lower-left strip passable.')
mask('proc.scrin',(0,0),'xx= xx= ===','User correction: only the upper two-by-two body is impassable; surrounding bib and delivery approach remain passable.')
mask('rea2',(0,0),'xx= xx= ===','User correction: upper two-by-two Advanced Reactor body impassable; remaining bib stays passable.')
mask('reac scrt',(0,0),'xx xx','Two-by-two root mass on enlarged three-by-three slab; border clear.')
mask('sfac sfac.nukular',(0,0),'xxx xxx ===','Wide rear headquarter root mass; front slab row is not a building.')
mask('sign',(0,0),'xx xx','Explicit user correction: two-by-two transmitter body; existing bib unchanged and its remaining cells passable.')
mask('srep',(0,0),'xx +x','Explicit user request: three solid cells and left-middle repair tile0,1 in upper two-by-two area.')
mask('wsph',(0,0),'xx= xx= ===','User correction: upper two-by-two body impassable; remaining bib and existing vehicle exit0,2 stay passable.')
# Soviet.
mask('afld',(0,0),'xx= ===','Only rear hangars are structural; runway/front apron is passable.')
mask('apwr.soviet',(0,0),'xxxx xxxx xxxx ====','Fenced reactor/pool/transformer platform occupies rear three rows; empty front bib strip stays passable.')
mask('barr',(0,-1),'xxx x=x _+_','U-shaped barracks: rear/side wings solid, central court and existing infantry exit open; no invisible front-corner reservation and still bibless.')
mask('cloning_vats',(-1,0),'xx xx','Vat body and pipes at actual rear-left two-by-two ground anchor.')
mask('dome.soviet',(-1,0),'xx= xxx xx=','Latest user correction: center0,1 solid; far-right cell1,0 free. Previously requested lower-left cells-1,2 and0,2 stay solid; front corner1,2 remains free.')
mask('fact.soviet fact.nukular.soviet',(-1,0),'xxx xxx xxx','Registered three-by-three hall/crane field, not old right-shifted collision square.')
mask('fix.soviet',(0,0),'xx= xx= +==','User correction:3x3 bib, upper2x2 solid; repair tile0,2 unchanged, original actor/body anchor preserved.')
mask('ftur',(0,0),'x','User-requested Sentry replacement: single solid pedestal cell.')
mask('ttur',(0,0),'x','User correction: chemical tower occupies exactly one solid cell; preserve visual registration.')
mask('hpad.soviet',(-1,0),'== ++','Low landing platform is transit-only rather than an impassable wall; front border stays walkable.')
mask('indp',(0,0),'xxx xxx','Industrial building and attached processing section occupy rear three-by-two.')
mask('iron nkorea_bunker powr.soviet',(-1,-1),'xx xx','Body ground contacts on rear four slab cells, replacing displaced old collision.')
mask('mslo',(0,-1),'xx xx','User correction: upper two-by-two bib cells (x0..1,y-1..0) impassable, not the left block; approved art and actor center unchanged.')
mask('soviet_refinery',(0,0),'xxx xxx ===','User correction: central cell1,1 is impassable; front loading approach remains open.')
mask('stek',(0,0),'xx xx','Compact two-by-two tech building; old third column was empty bib.')
mask('tpwr',(0,0),'xx xx','User correction: three-by-three bib; existing two-by-two solid machinery footprint retained.')
mask('weap.soviet',(-1,-1),'xxxx xxxx ==== ====','User correction: full upper-right half is a solid2x4 block (x-1..2,y-1..0); lower-left half and existing exit1,2 remain passable.')
# Small defense bases, reviewed at native size; airborne weapons are excluded from ground occupancy.
mask('agun chgatling chpropagandatower chpropagandatower.pdl cram gtwr gun.nod kenn ltur nsam obli pbox pris ptur sam scol scol.temp sentry.soviet sentry.soviet.voxel shar silo.scrin stwr tsla flak.soviet.sprite flak.soviet.voxel',(0,0),'x','Single registered ground pedestal; no change to approved pixels, pivots, muzzles or targets.')
mask('ion_defense',(0,0),'xx xx','Existing two-by-two Ion Defense base retained; rotating atlas is not a ground footprint.')
# Shipyards keep corner-water exits; reserve the platform envelope against construction.
mask('syrd syrd.gdi spen chsyrd',(0,0),'=x= xxx =x=','Support-pier cross solid; four corner water exits stay open, entire platform reserved.')
mask('spen.nod',(0,0),'xxx xxx x=x','Explicit user correction: entire submarine pen solid except the middle lower-left edge cell1,2; existing exits unchanged.')

records=[];rules=['# Individually reviewed ground masks; no art, actor origins, exit definitions or docking offsets moved.']
for e in entries:
 name=e['Actor']
 if name not in plans:
  if name in prior:raise ValueError('Missing reviewed bib actor: '+name)
  continue
 p=plans[name];ox,oy=p['origin'];body={(ox+x,oy+y):c for y,row in enumerate(p['rows']) for x,c in enumerate(row)}
 bib={tuple(c) for c in prior.get(name,{}).get('bib_cells',[])}
 if name in ('fix.soviet', 'tpwr'):bib={(x,y) for x in range(3) for y in range(3)}
 if name=='chpropaganda':bib={(x,y) for x in range(4) for y in range(-1,3)}
 reservation = set(body) if name in ('mslo', 'ttur', 'ftur') else bib
 cells={c:'=' for c in (reservation or {c for c,t in body.items() if t!='_'})};cells.update({c:t for c,t in body.items() if t not in ('=','_')})
 # Keep docking locations unchanged, reserve their approach tiles, and fail rather than silently cut a wall.
 dx,dy=e['BuildingDimensions'];lx,ly,lz=e['BuildingCenterOffset'];ax=724+362*(dx-dy)+lx;ay=362*(dx+dy)+ly
 docks=[]
 for wx,wy,wz in e['Docks']:
  x=math.floor((ax+wx+ay+wy-724)/1448);y=math.floor((ay+wy-ax-wx+724)/1448);docks.append([x,y])
  if cells.get((x,y)) in ('x','X'):raise ValueError(f'{name}: dock {(x,y)} intersects reviewed solid mask')
  # User correction for Nod/Scrin refinery spikes: external docking locations
  # remain ordinary ground, never an invisible extra construction reservation.
  if (x,y) in cells:cells[(x,y)]='+'
 left=min(x for x,y in cells);top=min(y for x,y in cells);w=max(x for x,y in cells)-left+1;h=max(y for x,y in cells)-top+1
 center=[lx-362*(w-h-dx+dy),ly-362*(w+h-dx-dy),lz]
 if name in ('ftur', 'ttur'):center=[0,0,0]  # Center single-cell defenses and their1x1 bib on the occupied cell, not the old2x2 pedestal.
 footprint=' '.join(''.join(cells.get((x,y),'_') for x in range(left,left+w)) for y in range(top,top+h))
 rules+=['',e['RuleKey']+':','\tBuilding:',f'\t\tDimensions: {w},{h}',f'\t\tFootprintOffset: {left},{top}',f'\t\tFootprint: {footprint}','\t\tLocalCenterOffset: '+','.join(map(str,center))]
 expected_exits=e['Exits']
 if name=='afld.gdi':
  # Aircraft ReturnToBase uses Exit.SpawnOffset for landing as well as production.
  spawn=[724+724*(2-1)-ax,724+724*(2+1)-ay,0]
  expected_exits=[{'Cell':[2,1],'Spawn':spawn}]
  rules+=['\tExit@1:','\t\tExitCell: 2,1','\t\tSpawnOffset: '+','.join(map(str,spawn))]
 if name=='chpropaganda':
  rules+=['\tWithIdleOverlay@CHINABIB:','\t\tImage: sovietbib4x4','\t\tOffset: 0,0,-724']
 if name=='ftur':
  rules+=['\tIsometricSelectable:','\t\tDecorationBounds: 1267,1267,0,0','\t\tDecorationHeight: 49']
 if name=='fix.soviet':
  # Body center is intentionally retained at Y+724. The 3x3 bib must instead
  # be centered on its occupied grid cells: move only the decoration up32px.
  rules+=['\tWithIdleOverlay@SOVIETBIB:','\t\tImage: sovietbib3x3','\t\tOffset: 0,0,724']
 if name=='grav':
  rules+=['\tHoversAboveFoundation:','\t\tRequiresCondition: !build-incomplete','\t\tInitialHeight: 136','\t\tBobDistance: -68','\t\tTicks: 24','\t\tRiseTicks: 24']
 records.append({'actor':name,'note':p['note'],'cells':[{'X':x,'Y':y,'Type':t} for (x,y),t in sorted(cells.items())],'bib_cells':sorted(bib),'dimensions':[w,h],'offset':[left,top],'center_offset':center,'old_dimensions':[dx,dy],'old_center_offset':[lx,ly,lz],'exits':expected_exits,'old_exits':e['Exits'],'docks':e['Docks'],'dock_cells':docks,'file':e['File']})
(ROOT/'mods/ca/rules/building-ground-masks.yaml').write_text('\n'.join(rules)+'\n');(OUT/'masks.json').write_text(json.dumps(records,indent=2)+'\n');print('Explicit ground masks:',len(records))
