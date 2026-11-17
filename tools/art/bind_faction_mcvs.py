"""Generate explicit Nod starting-unit/AI policies, preserving all existing AI lists."""
import os, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art-source/faction-mcvs';OUT.mkdir(exist_ok=True)
snapshot=OUT/'player-before.yaml'
if not snapshot.exists():
 utility=os.environ['YMCA_UTILITY'];env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
 snapshot.write_text(subprocess.check_output(['dotnet',utility,str(ROOT/'mods/ca'),'--resolved-rules','Player'],cwd=ROOT,env=env,text=True))
rules=['# GDI keeps legacy AMCV; Nod has independent deploy/redeploy/recovery identity.','^BaseWorld:']
for trait in ['mcvonlynod','mcvdoublednod','defaultnoda','heavynoda']:
 rules+=['\tStartingUnits@'+trait+':','\t\tBaseActor: amcv.nod']
 if trait=='mcvdoublednod':rules+=['\t\tSupportActors: amcv.nod']
rules+=['','Player:'];traits={};parent=None;sub=None
for line in snapshot.read_text().splitlines():
 depth=len(line)-len(line.lstrip('\t'));key,_,value=line.strip().partition(':')
 if depth==0:parent=key;sub=None
 if depth==1:sub=key
 if depth==1 and key in {'McvTypes','ConstructionYardTypes','ExcludeFromSquadsTypes'}:
  additions=['afac.nod.nukular'] if key=='ConstructionYardTypes' else ['amcv.nod','amcv.nod.nukular']
  traits.setdefault(parent,[]).append('\t\t'+key+': '+value.strip()+', '+', '.join(additions))
 if depth==2 and sub in {'UnitIntervals','UnitDelays','UnitsToBuild','UnitLimits'} and key=='amcv':
  traits.setdefault(parent,[]).extend(['\t\t'+sub+':','\t\t\tamcv.nod:'+value])
for trait,fields in traits.items():rules+=['\t'+trait+':']+fields
(ROOT/'mods/ca/rules/faction-mcvs.yaml').write_text('\n'.join(rules)+'\n')
