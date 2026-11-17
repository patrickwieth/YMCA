"""Liberty shellmap preflight against the actual engine and map package."""
from pathlib import Path
import json,os,subprocess,unittest
R=Path(__file__).resolve().parents[1];M=R/'mods/ca/maps/liberty-shellmap'
class LibertyShellmapTest(unittest.TestCase):
 def test_layout_and_source_binding(self):
  layout=json.loads((M/'layout.json').read_text())
  self.assertEqual(len(layout['cruisers']),10);self.assertEqual(len(layout['flaks']),6)
  self.assertEqual(len(set(map(tuple,layout['cruisers']))),10)
  text=(M/'map.yaml').read_text();self.assertNotIn('Playable: True',text)
  self.assertIn('Tileset: RUBBERDUCK-TEMPERATE',text)
  self.assertIn('bits/misc/buildings/statue of liberty.png',(M/'liberty-sequences.yaml').read_text())
 def test_bounded_continuing_battle(self):
  script=(M/'liberty.lua').read_text()
  for required in ['#raiders < 15','DateTime.Seconds(90)','DateTime.Seconds(12), SpawnWave','Trigger.OnKilled','unit.Attack(target)']:
   self.assertIn(required,script)
  self.assertIn('MinRange: 0',(M/'liberty-weapons.yaml').read_text())
 def test_engine_rules_sprites_and_ground(self):
  utility=os.environ.get('YMCA_UTILITY',str(R/'.art-validation/shellmap-runtime/bin/OpenRA.Utility.dll'))
  env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
  result=subprocess.check_output(['dotnet',utility,str(R/'mods/ca'),'--check-liberty-shellmap',str(M)],cwd=R,env=env,text=True)
  self.assertIn('complete sprite set passed',result)
if __name__=='__main__':unittest.main()
