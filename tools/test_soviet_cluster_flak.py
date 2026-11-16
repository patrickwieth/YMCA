"""Resolved balance and real headless cluster-dispatch tests (not live rendering)."""
from pathlib import Path
import os,subprocess,unittest
from test_soviet_building_art import traits,resolved
ROOT=Path(__file__).resolve().parents[1]
class SovietClusterFlakTests(unittest.TestCase):
 def test_quarter_frequency_equal_total_payload(self):
  allied=traits(resolved('--resolved-weapons','ZSU-23'));aa=traits(resolved('--resolved-weapons','SOVIET-CLUSTER-AA'));payload=traits(resolved('--resolved-weapons','SOVIET-CLUSTER-PAYLOAD'))
  self.assertEqual(allied['ReloadDelay'].strip(),'8');self.assertEqual(aa['ReloadDelay'].strip(),'32')
  self.assertNotIn('Warhead@1Dam',aa);self.assertIn('Explosions: small_explosion_air',aa['Warhead@2Eff']);self.assertIn('AirburstCluster',aa['Warhead@Cluster']);self.assertIn('Delay: 4',aa['Warhead@Cluster'])
  self.assertIn('Damage: 4000',payload['Warhead@1Dam']);self.assertNotIn('Projectile',payload)
  self.assertEqual(aa['ValidTargets'].strip(),'Air');self.assertEqual(payload['ValidTargets'].strip(),'Air')
  self.assertIn('ValidTargets: Air\n',payload['Warhead@1Dam']);self.assertIn('Range: 0, 0c512, 0c768, 3c768',payload['Warhead@1Dam'])
 def test_ground_testing_keeps_new_cluster_behavior(self):
  test=traits(resolved('--resolved-weapons','SOVIET-CLUSTER-AA.Test'));payload=traits(resolved('--resolved-weapons','SOVIET-CLUSTER-PAYLOAD.Test'))
  self.assertEqual(test['ReloadDelay'].strip(),'32');self.assertIn('Weapon: SOVIET-CLUSTER-PAYLOAD.Test',test['Warhead@Cluster']);self.assertIn('ValidTargets: Air, Ground, Water',payload['Warhead@1Dam'])
  normal=traits(resolved('--resolved-rules','FLAK.SOVIET.SPRITE'))
  self.assertIn('Weapon: SOVIET-CLUSTER-AA\n',normal['Armament'])
 def test_engine_dispatch_and_muzzle_frames(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  for command,expected in (('--check-soviet-cluster-flak','equal nominal DPS'),('--check-soviet-flak-sprite','muzzle max')):
   output=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),command],cwd=ROOT,env=env,text=True)
   self.assertIn(expected,output)
if __name__=='__main__':unittest.main()
