"""Validate and select Liberty Island; keep the previous shellmap intact."""
from pathlib import Path
import os,subprocess,json,shutil
from activate_two_sides_shellmap import read_yaml,visibility,set_visibility
R=Path(__file__).resolve().parents[1];MAPS=R/'mods/ca/maps';NEW=MAPS/'liberty-stream-shellmap/map.yaml'
BACK=R/'art-source/liberty-shellmap/previous-stream-selection'

def main():
 env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
 subprocess.run(['dotnet',str(R/'engine/bin/OpenRA.Utility.dll'),str(R/'mods/ca'),'--check-liberty-shellmap',str(NEW.parent)],cwd=R,env=env,check=True)
 BACK.mkdir(parents=True,exist_ok=True);record=BACK/'selection.json'
 entries=json.loads(record.read_text()) if record.exists() else []
 known={e['path'] for e in entries};changes=[]
 for path in list(MAPS.rglob('map.yaml'))+list(MAPS.glob('*.oramap')):
  old=visibility(read_yaml(path));flags=[f.strip() for f in old.split(',')]
  if path!=NEW and 'Shellmap' not in flags:continue
  new='Shellmap' if path==NEW else ', '.join(f for f in flags if f!='Shellmap') or '0'
  if old==new:continue
  relative=path.relative_to(R).as_posix()
  if relative not in known:
   dest=BACK/path.relative_to(MAPS);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest)
   entries.append({'path':relative,'visibility':old})
  changes.append((path,new))
 record.write_text(json.dumps(entries,indent=2)+'\n')
 for path,value in changes:set_visibility(path,value)
 print('Liberty Island selected. Previous shellmaps retained; visibility backup:',record)

if __name__=='__main__':main()
