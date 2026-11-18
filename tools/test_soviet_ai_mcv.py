"""Soviet starting/repacked MCV variants must be handled by every AI difficulty."""
import unittest
from test_soviet_building_art import resolved,traits
from test_current_building_followups import field

def items(block,key):
 return {s.strip() for s in (field(block,key) or '').split(',')}

class SovietAiMcv(unittest.TestCase):
 def test_starting_units_and_transform_targets(self):
  world=traits(resolved('--resolved-rules','World'))
  for key in ('mcvonlysoviet','mcvdoubledsoviet','lightsoviet','heavysoviet'):
   start=world['StartingUnits@'+key]
   self.assertTrue({'russia','iraq','nkorea'}<=items(start,'Factions'))
   self.assertEqual(field(start,'BaseActor'),'mcv.soviet')
  for actor,target in [('MCV.Soviet','fact.soviet'),('MCV.Nukular.Soviet','fact.nukular.soviet')]:
   r=traits(resolved('--resolved-rules',actor));self.assertEqual(field(r['Transforms'],'IntoActor'),target)
 def test_ai_deploys_and_does_not_send_mcvs_to_combat(self):
  player=traits(resolved('--resolved-rules','Player'))
  for key,block in player.items():
   if key.startswith('McvManagerBotModule'):
    self.assertTrue({'mcv.soviet','mcv.nukular.soviet'}<=items(block,'McvTypes'),key)
    self.assertTrue({'fact.soviet','fact.nukular.soviet'}<=items(block,'ConstructionYardTypes'),key)
    self.assertIn('weap.soviet',items(block,'McvFactoryTypes'),key)
   if key.startswith('SquadManagerBotModule'):
    self.assertTrue({'mcv.soviet','mcv.nukular.soviet'}<=items(block,'ExcludeFromSquadsTypes'),key)
   if key.startswith('BaseBuilderBotModule'):
    self.assertTrue({'fact.soviet','fact.nukular.soviet'}<=items(block,'ConstructionYardTypes'),key)

if __name__=='__main__':unittest.main()
