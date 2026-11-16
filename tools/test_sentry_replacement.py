"""Production FTUR uses the existing Sentry while preserving menu ownership and price."""
import unittest
from test_soviet_building_art import resolved, traits
from test_current_building_followups import field

class SentryReplacement(unittest.TestCase):
 def test_production_actor(self):
  actual=traits(resolved('--resolved-rules','FTUR'))
  source=traits(resolved('--resolved-rules','SENTRY.SOVIET'))
  for key in ('RenderSprites','Turreted','BodyOrientation','AttackTurreted','WithSpriteTurret','WithMuzzleOverlay','WithIdleOverlay@SOVIETBIB','SharedBarrelSequence'):
   self.assertEqual(actual[key].strip(),source[key].strip(),key)
  for level in range(4):
   key=f'SharedBarrelArmament@GAT{level}G'
   self.assertEqual(actual[key].strip(),source[key].strip())
  self.assertNotIn('Armament',actual)
  self.assertEqual(field(actual['Tooltip'],'Name'),'Sentry Gun')
  self.assertEqual(field(actual['Valued'],'Cost'),'600')
  self.assertEqual(field(actual['Building'],'Dimensions'),'1,1')
  self.assertEqual(field(actual['Building'],'Footprint'),'x')
  self.assertEqual(field(actual['Building'],'LocalCenterOffset'),'0,0,0')
  self.assertEqual(field(actual['Buildable'],'Prerequisites'),'barr, ~!structures.iraq, ~!structures.nkorea, ~structures.soviet')
  self.assertNotIn('Buildable',source)
  self.assertNotIn('Buildable',traits(resolved('--resolved-rules','SENTRY.SOVIET.VOXEL')))

if __name__=='__main__':unittest.main()
