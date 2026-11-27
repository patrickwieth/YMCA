"""Plan native source renders, preserving all approved Allied icons and pending Soviet art."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'art-source/faction-building-icons';ART.mkdir(parents=True,exist_ok=True)
records=json.loads((ROOT.parent/'.tmp/faction-icons-before.json').read_text())
# Non-building upgrades/mines and disabled experimental GLA are not building portraits.
EXCLUDED={'barr','apwr.soviet','building.mines','chcommand.radar','glcommand','upgc.bomb','upgc.drop','upgc.hold','upgc.seek'}
SHARED={'oilp','oil_platform','swiss_bank'}
FACTIONS={'gdi':'eagle','nod':'blackh','soviet':'russia','china':'chinatnk','scrin':'reaper','shared':'england'}
ALIASES={'Construction Yard':'Con Yard','Advanced Nuclear Power Plant':'Adv. Power','Nuclear Power Plant':'Power Plant','Advanced Nuclear Plant':'Adv. Reactor','Headquarter (China)':'Command Center','Weapons Factory':'War Factory','Advanced Communications Center':'Ion Cannon','Advanced Guard Tower':'Missile Tower','GDI Tech Center':'Tech Center','Soviet Tech Center':'Tech Center','GDI Barracks':'Barracks','China Barracks':'Barracks','Gravity Stabilizer':'Gravity Port','Field Manipulator':'Manipulator','Signal Transmitter':'Signal Tower','Propaganda Center':'Propaganda','Fortified Bunker':'Fort. Bunker','Speaker Tower (PDL)':'Speaker PDL','EMP Missile Launcher':'EMP Launcher','Point Defense Laser Tower':'Point Defense','Advanced Reactor':'Adv. Reactor','Internet Center':'Internet Ctr','Chemical Tower':'Chemical Tower'}
def family(a):
 p=' '.join(a['Prerequisites']);name=a['Actor']
 if name=='pdlt':return 'shared'
 if any(x in p for x in ('structures.allies','structures.england','structures.france','structures.germany')):return None
 if name.startswith('ch') or 'structures.china' in p:return 'china'
 if '.scrin' in name or 'structures.scrin' in p:return 'scrin'
 if '.nod' in name or 'structures.nod' in p:return 'nod'
 if '.soviet' in name or any(x in p for x in ('structures.soviet','structures.russia','structures.iraq','structures.nkorea')):return 'soviet'
 return 'gdi'
groups={}
for a in records:
 if a['Actor'] in EXCLUDED|SHARED:continue
 f=family(a)
 if f is None:continue
 view=a['Views'][0];image=view['Image'];key=(f,image)
 if key not in groups:
  label=ALIASES.get(a['Name'],a['Name'])
  groups[key]={'key':a['Actor'].replace('.','-'),'actor':a['Actor'],'family':f,'faction':FACTIONS[f],'owner':'Cameo'+f,'image':image,'label':label,'actors':[],'crop':.82,'focusX':.5,'focusY':.38,'facing':768}
 groups[key]['actors'].append(a['Actor'])
# Portraits of tall emitters deliberately show their head/mechanism, not an empty long shaft.
for g in groups.values():
 if g['actor'] in ('obli','tsla','scol','ion_defense','ltur','gtwr','stwr','ptur','shar','chpropagandatower','tpwr'):
  g['crop']=.7;g['focusY']=.30
 if len(g['label'])>14:print('Review long label:',g['actor'],g['label'])
(ART/'catalog.json').write_text(json.dumps(list(groups.values()),indent=2)+'\n')
(ART/'rules-before.json').write_text(json.dumps(records,indent=2)+'\n')
(ART/'approved-allied-hashes.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'mods/ca/bits/allies/icons').glob('*.png')},indent=2)+'\n')
print('Native source portraits:',len(groups),'Actors:',sum(len(g['actors']) for g in groups.values()))
