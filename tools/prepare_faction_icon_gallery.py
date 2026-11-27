"""Native menu fixture; prerequisites changed only in this disposable map."""
import argparse,json,re
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('destination',type=Path);p.add_argument('--family',required=True,choices=['gdi','nod','soviet','china','scrin']);a=p.parse_args()
art=ROOT/'art-source/faction-building-icons';catalog=json.loads((art/'catalog.json').read_text());baseline=json.loads((art/'rules-before.json').read_text())
faction={'gdi':'eagle','nod':'blackh','soviet':'russia','china':'chinatnk','scrin':'reaper'}[a.family]
actors=sorted({n for c in catalog if c['family'] in (a.family,'shared') for n in c['actors']}|{'oilp','oil_platform','swiss_bank'})
legacy=[]
actors+=legacy
with ZipFile(a.source) as z:data={n:z.read(n) for n in z.namelist()}
t=data['map.yaml'].decode();t=t.replace('Faction: england','Faction: '+faction);t=re.sub(r'ScreenshotTicks: \d+','ScreenshotTicks: 1000000',t)
if '\tWorld:\n' not in t:t=t.replace('Rules:\n','Rules:\n\tWorld:\n',1)
if '\tPlayer:\n' not in t:t=t.replace('Rules:\n','Rules:\n\tPlayer:\n',1)
t=t.replace('\tWorld:\n','\tWorld:\n\t\tAlliedBuildingIconReviewProbe:\n\t\t\tExpected: '+', '.join('iconreview.'+n for n in actors)+'\n\t\t\tAllowedLegacy: '+', '.join(legacy+['iconreview.'+n for n in legacy])+'\n',1)
t=t.replace('\tPlayer:\n','\tPlayer:\n\t\tProvidesPrerequisite@ICONGALLERY:\n\t\t\tPrerequisite: fixture.icons\n',1)
aliases=''
for row in baseline:
    if row['Actor'] not in actors:continue
    view=next(v for v in row['Views'] if v['Faction']==('nod' if faction=='blackh' else faction))
    aliases+='\tICONREVIEW.'+row['RuleKey']+':\n\t\tInherits: '+row['RuleKey']+'\n\t\tRenderSprites:\n\t\t\tImage: '+view['Image']+'\n\t\tBuildable:\n\t\t\tPrerequisites: ~fixture.icons\n'
# Keep aliases inside Rules even when Actors follows it in the source map.
lines=t.splitlines(keepends=True)
start=next(i for i,line in enumerate(lines) if line.startswith('Rules:'))
end=next((i for i in range(start+1,len(lines)) if lines[i].strip() and not lines[i].startswith(('\t',' ','#'))),len(lines))
t=''.join(lines[:end])+aliases+''.join(lines[end:])
# Injected icon-queue prerequisites do not change the real actor/gameplay rules.
data['map.yaml']=t.encode()
with ZipFile(a.destination,'w',ZIP_DEFLATED) as z:
 for n,b in data.items():z.writestr(n,b)
print(a.family,len(actors),'expected portraits')
