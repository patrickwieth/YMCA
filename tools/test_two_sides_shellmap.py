"""Structural/runtime preflight for the generated, map-local background battle."""
from pathlib import Path
import json,os,subprocess,unittest
from activate_two_sides_shellmap import read_yaml,visibility
R=Path(__file__).resolve().parents[1];M=R/'mods/ca/maps/two-sides-shellmap'
class TwoSidesShellmap(unittest.TestCase):
 def test_reproducible_layout_and_no_playable_slots(self):
  meta=json.loads((M/'generation.json').read_text());self.assertEqual(meta['preset'],'two-sides');self.assertEqual(meta['seed'],1967)
  text=(M/'map.yaml').read_text();self.assertNotIn('Playable: True',text)
  self.assertIn('Bot: normal',text);self.assertIn('Faction: talon',text);self.assertIn('Faction: russia',text)
  self.assertIn(visibility(text),('0','Shellmap'))
  self.assertTrue((M/'map.bin').is_file());self.assertTrue((M/'map.png').is_file())
 def test_map_local_behavior(self):
  rules=(M/'shellmap-rules.yaml').read_text();script=(M/'shellmap.lua').read_text()
  self.assertIn('ShellmapBattleMonitor:',rules)
  self.assertIn('-ConquestVictoryConditions:',rules);self.assertIn('-CheckpointVictoryConditions:',rules)
  self.assertIn('Trigger.OnAnyProduction',script);self.assertIn('DateTime.Seconds(180)',script)
  self.assertIn('shellmap-core',script)
  self.assertNotIn('SquadValue:',rules)
 def test_actual_engine_map_and_sprites(self):
  env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
  utility=os.environ.get('YMCA_UTILITY',str(R/'engine/bin/OpenRA.Utility.dll'))
  result=subprocess.check_output(['dotnet',utility,str(R/'mods/ca'),'--check-two-sides-shellmap',str(M)],cwd=R,env=env,text=True)
  self.assertIn('complete sprite set passed',result)

if __name__=='__main__':unittest.main()
