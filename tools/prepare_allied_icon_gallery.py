"""Create an isolated, all-Allied-buildings icon gallery. Never install as a normal map."""
import argparse, subprocess, sys
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
p=argparse.ArgumentParser()
p.add_argument('source',type=Path);p.add_argument('destination',type=Path)
p.add_argument('--faction',choices=('england','france','germany'),default='england')
a=p.parse_args()
subprocess.run([sys.executable,str(Path(__file__).with_name('prepare_production_icon_probe.py')),str(a.source),str(a.destination)],check=True)
with ZipFile(a.destination) as z:files={n:z.read(n) for n in z.namelist()}
t=files['map.yaml'].decode().replace('Faction: england','Faction: '+a.faction)
t=t.replace('ProductionPaletteReviewProbe:', 'AlliedBuildingIconReviewProbe:')
# Make every country variant and optional building visible in this fixture only.
# PBOX has an explicit negative England prerequisite, unlike HBOX.
t=t.replace('Rules:\n','Rules:\n\tPBOX:\n\t\tBuildable:\n\t\t\tPrerequisites: ~structures.allies\n',1)
prerequisites=['structures.england','structures.france','structures.germany','tent','anypower','proc','weap','dome','fix','syrd','atek','radar','tier3','conyardbuildable','promotion.oil_pumps','global.swiss_banks','disabled','techlevel.superw']
providers=''.join(f'\t\tProvidesPrerequisite@ICONGALLERY{i}:\n\t\t\tPrerequisite: {prereq}\n' for i,prereq in enumerate(prerequisites))
t=t.replace('\tPlayer:\n','\tPlayer:\n'+providers,1)
files['map.yaml']=t.encode()
with ZipFile(a.destination,'w',ZIP_DEFLATED) as z:
 for name,data in files.items():z.writestr(name,data)
