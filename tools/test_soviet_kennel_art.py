"""Supplied kennel art: direct binding, untouched pixels and gameplay preservation."""
from pathlib import Path
import hashlib,unittest
from PIL import Image
from test_soviet_building_art import resolved,traits,gameplay_hash
ROOT=Path(__file__).resolve().parents[1]
class SovietKennelTests(unittest.TestCase):
 def test_supplied_png_and_native_sequences(self):
  path=ROOT/'mods/ca/bits/soviet/buildings/kennel.png'
  self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),'1ef7658d72edf495de81db6f858324a85ae81e2ad0987b7c17cda1695c263568')
  with Image.open(path) as im:self.assertEqual(im.size,(128,128))
  seq=traits(resolved('--resolved-sequences','kenn.soviet'))
  for state in ('idle','damaged-idle'):
   for field in ('Filename: bits/soviet/buildings/kennel.png','Start: 0','Length: 1','Scale: 1','Anchor: Bottom','Offset: -1,17'):self.assertIn(field,seq[state])
  self.assertIn('make-small1.png',seq['construction'])
 def test_production_exits_footprint_and_stats_unchanged(self):
  before=(ROOT/'art-source/soviet-kennel/rules-before.yaml').read_text()
  after=resolved('--resolved-rules','KENN');self.assertEqual(gameplay_hash(before),gameplay_hash(after))
  old,new=traits(before),traits(after)
  for key in ('Production','Exit@1','Exit@2','Building','Buildable','Health','Power'):self.assertEqual(old[key],new[key],key)
  self.assertIn('Image: kenn.soviet',new['RenderSprites']);self.assertNotIn('WithBuildingBib',new)
  self.assertIn('Image: sovietbib1x1',new['WithIdleOverlay@SOVIETBIB'])
if __name__=='__main__':unittest.main()
