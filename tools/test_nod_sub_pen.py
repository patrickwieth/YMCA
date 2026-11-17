from pathlib import Path
import hashlib,os,subprocess,unittest
from test_soviet_building_art import traits,resolved
ROOT=Path(__file__).resolve().parents[1]
class NodSubPenTests(unittest.TestCase):
 def test_source_and_direct_binding(self):
  path=ROOT/'mods/ca/bits/nod/buildings/submarine pen.png'
  self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),'261ce2d16bece3880dd3189ffc04358270704f3755abcfeb437ca845a8130ac5')
  seq=traits(resolved('--resolved-sequences','spennod'))
  for state in ('idle','damaged-idle','place'):
   self.assertIn('Filename: bits/nod/buildings/submarine pen.png',seq[state]);self.assertIn('Start: 0',seq[state]);self.assertIn('Scale: 1',seq[state])
 def test_only_selection_envelope_changed(self):
  before=traits((ROOT/'art-source/nod-sub-pen/rules-before.yaml').read_text());after=traits(resolved('--resolved-rules','SPEN.nod'))
  self.assertEqual(before.keys(),after.keys())
  for name in before:
   if name!='IsometricSelectable':self.assertEqual(before[name],after[name],name)
 def test_engine_loads(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-nod-sub-pen-art'],cwd=ROOT,env=env,check=True)
if __name__=='__main__':unittest.main()
