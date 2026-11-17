"""Launch an isolated, self-terminating shellmap test; never stop existing games.
First build CA with OutputPath=.art-validation/shellmap-runtime/bin (absolute path).
"""
from pathlib import Path
import argparse,os,subprocess,shutil,re,json
p=argparse.ArgumentParser();p.add_argument('--ticks',type=int,default=3000);p.add_argument('--wait',action='store_true');p.add_argument('--map',choices=['two-sides-shellmap','liberty-shellmap','liberty-stream-shellmap'],default='two-sides-shellmap');a=p.parse_args()
R=Path(__file__).resolve().parents[1];W=R/'.art-validation/shellmap-runtime';B=W/'bin'
for f in (R/'engine/bin').rglob('*'):
 if f.is_file():
  dest=B/f.relative_to(R/'engine/bin')
  if not dest.exists():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest)
mod=W/'mods/ca';mod.mkdir(parents=True,exist_ok=True);support=W/'support';support.mkdir(exist_ok=True)
# Discover the normal content directory without sharing settings/logs/map cache.
content_candidates=[Path(os.environ.get('APPDATA',''))/'OpenRA/Content/ca',Path.home()/'OneDrive/Dokumente/OpenRA/Content/ca',Path.home()/'Documents/OpenRA/Content/ca',R/'engine/Support/Content/ca']
content=next(c for c in content_candidates if c.is_dir())
if not (support/'Content').exists():
 if os.name=='nt':subprocess.run(['cmd.exe','/c','mklink','/J',str(support/'Content'),str(content.parent)],check=True)
 else:(support/'Content').symlink_to(content.parent,target_is_directory=True)
text=(R/'mods/ca/mod.yaml').read_text().replace('$ca: ca','^EngineDir|../mods/ca: ca')
text=re.sub(r'MapFolders:\n(?:[^\S\n]*\n|\t[^\n]*\n)*','MapFolders:\n\t^EngineDir|../.art-validation/shellmap-runtime/maps: System\n\n',text)
(mod/'mod.yaml').write_text(text)
for old in (W/'maps').glob('*/map.yaml'):
 old.write_text(re.sub(r'^Visibility:.*$', 'Visibility: 0',old.read_text(),flags=re.M))
shutil.copytree(R/'mods/ca/maps'/a.map,W/'maps'/a.map,dirs_exist_ok=True)
map_yaml=W/'maps'/a.map/'map.yaml'
map_yaml.write_text(re.sub(r'^Visibility:.*$', 'Visibility: Shellmap',map_yaml.read_text(),flags=re.M))
env=dict(os.environ,YMCA_SHELLMAP_REPORT=str(W/'battle.jsonl'),YMCA_SHELLMAP_TEST_TICKS=str(a.ticks))
args=[str(B/'OpenRA.exe'),f'Game.Mod={mod}',f'Engine.EngineDir={R/"engine"}',f'Engine.ModSearchPaths={R/"mods"},{R/"engine/mods"}',f'Engine.SupportDir={support}','Graphics.Mode=Windowed','Graphics.WindowedSize=1280,720','Graphics.CapFramerate=true','Graphics.MaxFramerate=30','Sound.MasterVolume=0','Game.IntroductionPromptVersion=999','Debug.SystemInformationVersionPrompt=999','Debug.SendSystemInformation=false','Debug.CheckVersion=false']
(W/'battle.jsonl').write_text('')
with (W/'game.log').open('w') as log:proc=subprocess.Popen(args,cwd=R,env=env,stdout=log,stderr=subprocess.STDOUT)
(W/'probe-process.json').write_text(json.dumps({'pid':proc.pid,'ticks':a.ticks,'exits_itself':True,'log':str(W/'game.log')},indent=2)+'\n')
print('Started isolated shellmap probe PID',proc.pid,'; it exits itself after',a.ticks,'ticks.',flush=True)
if a.wait:
 try:code=proc.wait(timeout=a.ticks/25+100)
 except subprocess.TimeoutExpired:
  # Only terminate the isolated process created by this probe, never other games.
  proc.terminate();proc.wait();raise AssertionError('Isolated probe stalled; inspect its Lua/game logs.')
 print('Probe exit code:',code)
 rows=[json.loads(line) for line in (W/'battle.jsonl').read_text().splitlines() if line]
 assert code==0 and rows and rows[-1]['ticks']>=a.ticks,(code,rows[-1] if rows else 'no simulation ticks')
 if a.map=='two-sides-shellmap':
  for side in rows[-1]['players']:
   assert side['built']>0 and side['produced']>0 and side['kills']>0,side
  print('Both sides built structures, produced units and scored combat kills.')
 else:
  assert all(side['kills']>0 for side in rows[-1]['players']),rows[-1]
  assert max(len(side.get('units',[])) for row in rows for side in row['players'] if side['player']=='Raiders') <= (48 if a.map=='liberty-stream-shellmap' else 15)
  assert not (W/'support/Logs/lua.log').read_text().strip(),'Lua errors recorded'
  print('Liberty: both sides scored real kills; bounded bomber waves and no Lua errors.')
