"""Select the new shellmap after a successful runtime preflight; keep old maps.
Only Visibility is changed. Exact previous map files are backed up. --restore
restores visibility flags without overwriting later map/art edits.
"""
from pathlib import Path
import argparse,json,os,re,shutil,subprocess,zipfile,hashlib
R=Path(__file__).resolve().parents[1];MAPS=R/'mods/ca/maps';NEW=MAPS/'two-sides-shellmap/map.yaml';BACK=R/'art-source/two-sides-shellmap/previous-shellmaps'

def read_yaml(path):
 if path.suffix=='.oramap':
  with zipfile.ZipFile(path) as z:return z.read('map.yaml').decode('utf-8-sig')
 return path.read_text(encoding='utf-8-sig')

def visibility(text):
 return re.search(r'^Visibility: (.*)$',text,re.M)[1]

def set_visibility(path,value):
 old=read_yaml(path);new=re.sub(r'^Visibility:.*$', 'Visibility: '+value,old,flags=re.M)
 if old==new:return
 temp=path.with_name(path.name+'.shellmap-tmp')
 if path.suffix=='.oramap':
  with zipfile.ZipFile(path) as src,zipfile.ZipFile(temp,'w') as dst:
   for info in src.infolist():dst.writestr(info,new.encode('utf-8') if info.filename=='map.yaml' else src.read(info.filename))
 else:temp.write_text(new,encoding='utf-8',newline='\n')
 temp.replace(path)

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--restore',action='store_true');a=parser.parse_args();record=BACK/'selection.json'
 if a.restore:
  for entry in json.loads(record.read_text()):set_visibility(R/entry['path'],entry['visibility'])
  print('Previous shellmap flags restored; map contents retained.');return
 # Do not select a map that the installed runtime cannot load.
 env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
 subprocess.run(['dotnet',str(R/'engine/bin/OpenRA.Utility.dll'),str(R/'mods/ca'),'--check-two-sides-shellmap',str(NEW.parent)],cwd=R,env=env,check=True)
 paths=[NEW]+[p for p in MAPS.rglob('map.yaml') if p!=NEW]+list(MAPS.glob('*.oramap'))
 changes=[]
 for path in paths:
  old=visibility(read_yaml(path));flags=[v.strip() for v in old.split(',')]
  if path!=NEW and 'Shellmap' not in flags:continue
  new='Shellmap' if path==NEW else ', '.join(v for v in flags if v!='Shellmap') or '0'
  if new!=old:changes.append((path,old,new))
 BACK.mkdir(parents=True,exist_ok=True)
 previous=json.loads(record.read_text()) if record.exists() else []
 known={entry['path'] for entry in previous}
 for path,old,new in changes:
  relative=path.relative_to(R).as_posix()
  if relative not in known:
   backup=hashlib.sha256(relative.encode()).hexdigest()[:12]+'-'+path.name
   shutil.copy2(path,BACK/backup);previous.append({'path':relative,'visibility':old,'backup':backup})
 record.write_text(json.dumps(previous,indent=2)+'\n')
 for path,old,new in changes:set_visibility(path,new)
 print('Two Sides GDI/Soviet background selected; old maps retained with previous visibility backed up.')

if __name__=='__main__':main()
