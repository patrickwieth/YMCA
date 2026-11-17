"""Explicit requested screen translations; never move bibs, occupied cells or gameplay origins."""
from pathlib import Path
import json,sys,re,math
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools'))
from test_soviet_building_art import resolved,traits
OUT=ROOT/'art-source/nod-bib-followup';OUT.mkdir(exist_ok=True)
DELTAS={'AFAC':(0,4),'PYLE':(0,4),'WEAP.TD':(0,4),'NUK2':(0,-4),'HQ':(20,20),'REP':(0,4),'PROC.TD':(40,40),'EYE':(40,40),'GTEK':(0,10),'AFLD.GDI':(0,10),'UPGC':(0,10),'INDP':(0,30),'FIX.Soviet':(24,16),'STEK':(0,10),'FACT.Soviet':(0,-10),'POWR.Soviet':(0,-10),'APWR.Soviet':(0,4),'Soviet_Refinery':(0,20),'AFAC.Nod':(0,-8)}
scrin_plan_path=ROOT/'art-source/scrin-rear-alignment/plan.json'
scrin_plan=json.loads(scrin_plan_path.read_text()) if scrin_plan_path.exists() else []
DELTAS.update({e['actor']:tuple(e['screen_delta']) for e in scrin_plan})
DELTAS['SILO.SCRIN']=(0,-34)
# Explicit upper2x2 collision request: clear the half-cell visual offset on3x3 slabs.
DELTAS.update({'chpower':(0,-32),'chpower2':(0,-32),'WEAP.NOD':(30,30)})
# Pin effects on unrelated derived Nod actors to their exact pre-feedback positions.
PINS=['NUK2.Nod','HQ.Nod','REP.Nod','PROC.TD.Nod','WEAP.NOD','fact.colorpicker']
def snapshot(actor):
 path=OUT/(actor.lower()+'-before.yaml')
 if not path.exists():path.write_text(resolved('--resolved-rules',actor))
 return traits(path.read_text())
def field(text,name,default):
 m=re.search(r'^\t'+re.escape(name)+r': (.*)$',text,re.M);return m.group(1) if m else default
def rnd(x):return math.floor(x+.5) if x>=0 else math.ceil(x-.5)
seqs=['# Explicit user-requested body translations; construction and bibs are unchanged.'];rules=['# Relative visual feedback only. Do not change Building/Exit/Docking/weapon traits.'];records=[]
for actor,(dx,dy) in DELTAS.items():
 old=snapshot(actor);image=field(old['RenderSprites'],'Image',actor.lower());path=OUT/(image+'-sequences-before.yaml')
 if not path.exists():path.write_text(resolved('--resolved-sequences',image))
 states=traits(path.read_text());seqs+=['',image+':'];updates={}
 for name,text in states.items():
  filename=field(text,'Filename','')
  # Only the supplied native building body, never inherited cosmetic/status/icon sequences.
  if not (actor=='SILO.SCRIN' and name in ('idle','damaged-idle')) and (not filename.startswith('bits/') or '/buildings/' not in filename or not filename.endswith('.png') or 'bib' in filename):continue
  scale=float(field(text,'Scale','1'));offset=[float(v) for v in field(text,'Offset','0,0').split(',')];offset[0]+=dx/scale;offset[1]+=dy/scale
  value=','.join(f'{v:g}' for v in offset);seqs+=['\t'+name+':','\t\tOffset: '+value];updates[name]=offset
 raw_bounds=field(old['IsometricSelectable'],'DecorationBounds',field(old['IsometricSelectable'],'Bounds',''))
 bounds=[int(v.strip()) for v in raw_bounds.split(',')] if raw_bounds else None
 decoration_offset=None
 if bounds:
  while len(bounds)<4:bounds.append(0)
  bounds[2]+=rnd(dx*1448/128);bounds[3]+=rnd(dy*1448/64)
  rules+=['',actor+':','\tIsometricSelectable:','\t\tDecorationBounds: '+', '.join(map(str,bounds))]
 else:
  decoration_offset=[rnd(dx*1448/128),rnd(dy*1448/64),0]
  rules+=['',actor+':','\tIsometricSelectable:','\t\tDecorationOffset: '+','.join(map(str,decoration_offset))]
 effects={}
 for key,text in old.items():
  if not key.startswith('WithIdleOverlay') or 'BIB' in key.upper():continue
  off=[int(v) for v in field(text,'Offset','0,0,0').split(',')];off[1]+=rnd(dx*1448/128);off[2]-=rnd(dy*1448/64);effects[key]=off
  rules+=['\t'+key+':','\t\tOffset: '+','.join(map(str,off))]
 for e in scrin_plan:
  if e['actor']==actor and e['new_bib_cells']!=e['old_bib_cells']:
   rules+=['\tWithIdleOverlay@SCRINBIB:','\t\tImage: scrinbib'+str(e['new_bib_cells'])+'x'+str(e['new_bib_cells']),'\t\tOffset: 0,0,'+str(-rnd(e['bib_down_pixels']*1448/64))]
 if actor in {e['actor'] for e in scrin_plan} or actor=='SILO.SCRIN':
  rules+=['\tRenderSprites:','\t\tHideBodyWhileBuilding: true']
  # Existing larger-bib block already exists for three actors; add the condition there.
  if any(e['actor']==actor and e['new_bib_cells']!=e['old_bib_cells'] for e in scrin_plan):
   index=max(i for i,line in enumerate(rules) if line=='\tWithIdleOverlay@SCRINBIB:');rules.insert(index+1,'\t\t-RequiresCondition:')
  else:rules+=['\tWithIdleOverlay@SCRINBIB:','\t\t-RequiresCondition:']
 records.append({'actor':actor,'image':image,'screen_delta':[dx,dy],'sequences':updates,'decoration_bounds':bounds,'decoration_offset':decoration_offset,'effects':effects})
for actor in PINS:
 if actor in DELTAS:continue
 old=snapshot(actor);rules+=['',actor+':']
 for key,text in old.items():
  if key.startswith('WithIdleOverlay') and 'BIB' not in key.upper():rules+=['\t'+key+':','\t\tOffset: '+field(text,'Offset','0,0,0')]
for actor in ['PTUR','SHAR','SCOL']:
 rules+=['',actor+':','\tRenderSprites:','\t\tHideBodyWhileBuilding: true','\tWithIdleOverlay@SCRINBIB:','\t\t-RequiresCondition:']
rules+=['','BARR:','\t-WithIdleOverlay@SOVIETBIB:','\tWithShadow@BUILDINGFEEDBACK:','\t\tRequiresCondition: !build-incomplete','\t\tShadowColor: 00000070','\t\tOffset: 0,90,0','\t\tZOffset: -2048']
(ROOT/'mods/ca/sequences/building-position-feedback.yaml').write_text('\n'.join(seqs)+'\n')
(ROOT/'mods/ca/rules/building-position-feedback.yaml').write_text('\n'.join(rules)+'\n')
(OUT/'position-feedback.json').write_text(json.dumps(records,indent=2)+'\n')
print({r['actor']:r['screen_delta'] for r in records})
