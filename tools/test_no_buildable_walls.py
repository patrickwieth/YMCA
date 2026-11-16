"""No faction may build barriers; existing map objects retain all other traits."""
from pathlib import Path
import json,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1]
class NoBuildableWallsTests(unittest.TestCase):
 def test_all_barriers_retained_without_buildable(self):
  old=json.loads((ROOT/'art-source/new-faction-buildings/walls-before.json').read_text())
  for actor,before in old.items():
   with self.subTest(actor=actor):
    a=traits(before);b=traits(resolved('--resolved-rules',actor));a.pop('Buildable',None);self.assertNotIn('Buildable',b)
    self.assertEqual({k:v.strip() for k,v in a.items()},{k:v.strip() for k,v in b.items()})
if __name__=='__main__':unittest.main()
