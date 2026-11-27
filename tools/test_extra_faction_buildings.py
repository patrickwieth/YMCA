"""China/Scrin upper test-map blocks: coverage, preservation and real engine loading."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import os,re,subprocess,tempfile,unittest
from prepare_extra_faction_buildings import GROUPS,placements,prepare,OIL_PLATFORM
ROOT=Path(__file__).resolve().parents[1]
class ExtraFactionBuildingTests(unittest.TestCase):
 def test_complete_standalone_inventory(self):
  excluded={'China':{'chcommand.nukular','chcommand.radar','chbunker.ai','airfield.dockhelper'},'Scrin':{'sfac.nukular','silo.scrin','strm','scol.temp'}}
  for group in GROUPS:
   text=(ROOT/f'mods/ca/rules/{group.lower()}/structures.yaml').read_text()
   actors={n.lower() for n in re.findall(r'^([\w.]+):',text,re.M)}
   self.assertEqual(actors-excluded[group],set(GROUPS[group][2]))
 def test_idempotent_and_existing_actors_unchanged(self):
  with ZipFile(ROOT/'mods/ca/development-maps/testmap1.oramap') as z:text=z.read('map.yaml').decode('utf-8-sig').replace('\r\n','\n')
  updated=prepare(text)
  self.assertEqual(updated,prepare(updated))
  for block in re.findall(r'(?m)^\tActor\d+: [^\n]+\n(?:\t\t[^\n]*\n)*',text):self.assertIn(block,updated)
  self.assertEqual(len(list(placements())),35)
  actor,name,(x,y),faction=OIL_PLATFORM
  self.assertEqual(updated.count(f'\t{actor}: {name}\n'),1)
  self.assertIn(f'\t{actor}: {name}\n\t\tOwner: Multi0\n\t\tLocation: {x},{y}\n\t\tFaction: {faction}',updated)
  for actor,name,(x,y),faction in placements():
   self.assertIn(f'\t{actor}: {name}\n\t\tOwner: Multi0\n\t\tLocation: {x},{y}\n\t\tFaction: {faction}',updated)
   self.assertLess(x+y,76)
   u=(x-y)//2
   self.assertTrue(24<=u<=48 if actor.startswith('TestChina') else 78<=u<=102)
 def test_disposable_map_engine_validation(self):
  source=ROOT/'mods/ca/development-maps/testmap1.oramap';before=source.read_bytes()
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  with tempfile.TemporaryDirectory() as temp:
   target=Path(temp)/'testmap1-debug.oramap'
   subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'tools/prepare-test-game-map.ps1'),'-SourceMap',str(source),'-DestinationMap',str(target)],check=True)
   for command in ('--prepare-building-test-water','--check-extra-building-test'):
    result=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),command,str(target)],env=env,cwd=ROOT,text=True)
    if command.endswith('water'):
     self.assertIn('5 Naval Yard(s)',result)
     self.assertIn('1 Oil Platform(s)',result)
    else:self.assertIn('China18 / Scrin17',result)
   # Keep the checker strict: missing required bibs and unexpected bunker bibs
   # must fail by actor name, not via an uninformative LINQ Single() exception.
   with ZipFile(target) as z:files={n:z.read(n) for n in z.namelist()}
   text=files['map.yaml'].decode('utf-8-sig').replace('\r\n','\n');self.assertIn('\nRules:\n',text)
   for rule,message in [
    ('\tchbarracks:\n\t\t-WithIdleOverlay@CHINABIB:\n','chbarracks: expected exactly one decorative bib, found 0.'),
    ('\tchbunker:\n\t\tWithIdleOverlay@UNEXPECTEDBIB:\n\t\t\tImage: sovietbib2x2\n\t\t\tSequence: idle\n','chbunker: expected no decorative bib, found 1.')]:
    invalid=Path(temp)/'invalid-bib.oramap'
    with ZipFile(invalid,'w',ZIP_DEFLATED) as z:
     for name,data in files.items():z.writestr(name,text.replace('\nRules:\n','\nRules:\n'+rule,1).encode('utf-8') if name=='map.yaml' else data)
    result=subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-extra-building-test',str(invalid)],env=env,cwd=ROOT,text=True,capture_output=True)
    self.assertNotEqual(result.returncode,0);self.assertIn(message,result.stdout+result.stderr);self.assertNotIn('Sequence contains no matching element',result.stdout+result.stderr)
  self.assertEqual(before,source.read_bytes())
if __name__=='__main__':unittest.main()
