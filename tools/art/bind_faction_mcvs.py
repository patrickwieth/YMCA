"""Generate faction MCV AI policies, preserving existing lists and faction variants."""
import os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/faction-mcvs';OUT.mkdir(exist_ok=True)
snapshot=OUT/'player-before.yaml'
if not snapshot.exists():
 utility=os.environ['YMCA_UTILITY'];env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
 snapshot.write_text(subprocess.check_output(['dotnet',utility,str(ROOT/'mods/ca'),'--resolved-rules','Player'],cwd=ROOT,env=env,text=True))
rules=['# Faction-specific MCV deploy/redeploy/recovery identities; retain legacy GDI AMCV.','^BaseWorld:']
for trait in ['mcvonlynod','mcvdoublednod','defaultnoda','heavynoda']:
 rules+=['\tStartingUnits@'+trait+':','\t\tBaseActor: amcv.nod']
 if trait=='mcvdoublednod':rules+=['\t\tSupportActors: amcv.nod']
rules+=['','Player:'];traits={};parent=None;sub=None
for line in snapshot.read_text().splitlines():
 depth=len(line)-len(line.lstrip('\t'));key,_,value=line.strip().partition(':')
 if depth==0:parent=key;sub=None
 if depth==1:sub=key
 if depth==1 and key in {'McvTypes','ConstructionYardTypes','ExcludeFromSquadsTypes','McvFactoryTypes'}:
  additions={'ConstructionYardTypes':['afac.nod.nukular','fact.nukular.soviet'],
   'McvFactoryTypes':['weap.soviet'],
   'McvTypes':['amcv.nod','amcv.nod.nukular','mcv.soviet','mcv.nukular.soviet'],
   'ExcludeFromSquadsTypes':['amcv.nod','amcv.nod.nukular','mcv.soviet','mcv.nukular.soviet']}[key]
  existing=[v.strip() for v in value.split(',')]
  traits.setdefault(parent,[]).append('\t\t'+key+': '+', '.join(existing+[v for v in additions if v not in existing]))
 if depth==2 and sub in {'UnitIntervals','UnitDelays','UnitsToBuild','UnitLimits'} and key in {'amcv','mcv'}:
  variant='amcv.nod' if key=='amcv' else 'mcv.soviet'
  traits.setdefault(parent,[]).extend(['\t\t'+sub+':','\t\t\t'+variant+':'+value])
for trait,fields in traits.items():
 grouped={};key=None
 for line in fields:
  if line.startswith('\t\t\t'):
   grouped[key].append(line)
  else:
   key=line.strip().partition(':')[0]
   grouped.setdefault(key,[line])
 rules+=['\t'+trait+':']+[line for group in grouped.values() for line in group]
(ROOT/'mods/ca/rules/faction-mcvs.yaml').write_text('\n'.join(rules)+'\n')
