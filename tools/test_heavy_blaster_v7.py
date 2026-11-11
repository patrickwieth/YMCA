"""Straight-roof previews: native output and unchanged fixed/game assets."""
from art.ion_art_safety import assert_preserved_ion_art
from pathlib import Path
from PIL import Image
import unittest,json,hashlib
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-07-preview'
class StraightRoofTests(unittest.TestCase):
 def test_centered_straight_roof_geometry(self):
  report=json.loads((OUT/'geometry.json').read_text());a,b=report['conduit_endpoints']
  self.assertEqual(a[0],-.125);self.assertEqual(b[0],-.125)
  self.assertEqual(report['changed_uv_faces'],[3,4,5,6]);self.assertEqual(len(report['changed_housing_vertices']),6)
  for change in report['changed_housing_vertices']:
   x,y,z=change['after'];self.assertEqual(change['before'][:2],[x,y]);expected=a[2]+(y-a[1])/(b[1]-a[1])*(b[2]-a[2])-.047;self.assertAlmostEqual(z,expected,places=6)
  self.assertFalse(report['game_changed']);self.assertFalse(report['muzzle_changed'])

 def test_original_source_fixed_base_and_historical_assets_preserved(self):
  self.assertEqual(hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  for size in (160,192):self.assertEqual((OUT/str(size)/'base.png').read_bytes(),(BASE/'attempt-06-preview'/str(size)/'base.png').read_bytes())
  assert_preserved_ion_art(self)

 def test_all_directions_native_palette_and_clipping(self):
  with Image.open(BASE/'heavy blaster 3.png') as source:colors={c for c in source.convert('RGBA').getdata() if c[3]==255}
  for size in (160,192):
   folder=OUT/str(size);self.assertEqual(len(list(folder.glob('turret-???-raw.png'))),32)
   for i in range(32):
    with Image.open(folder/f'turret-{i:03d}-raw.png') as raw:self.assertEqual(raw.size,(size,size))
    with Image.open(folder/f'assembly-{i:03d}.png') as im:
     self.assertEqual(im.size,(size,size));self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255});self.assertLessEqual({c for c in im.getdata() if c[3]},colors);box=im.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),size)
if __name__=='__main__':unittest.main()
