"""Retained smaller previews and the now-approved active V10 binding."""
from art.ion_art_safety import assert_preserved_ion_art
from pathlib import Path
from PIL import Image
import unittest,json
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-06-preview'
class HeavyBlasterPreviewTests(unittest.TestCase):
 def test_sizes_and_direct_render_sources(self):
  report=json.loads((OUT/'validation.json').read_text());self.assertFalse(report['installed'])
  for size,count in ((160,32),(192,8)):
   raw=list((OUT/str(size)).glob('turret-???-raw.png'));self.assertEqual(len(raw),count)
   for p in raw:
    with Image.open(p) as im:self.assertEqual(im.size,(size,size))
   with Image.open(OUT/str(size)/'base.png') as im:self.assertEqual(im.size,(size,size))
  with Image.open(OUT/'start-size-comparison.png') as im:self.assertEqual(im.size,(1280,380))

 def test_historical_and_active_ion_assets(self):
  assert_preserved_ion_art(self)

 def test_native_alpha_palette_and_no_clipping(self):
  with Image.open(BASE/'heavy blaster 3.png') as src:colors={c for c in src.convert('RGBA').getdata() if c[3]==255}
  for size in (160,192):
   for p in (OUT/str(size)).glob('assembly-???.png'):
    with Image.open(p) as im:
     im=im.convert('RGBA');self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255});self.assertLessEqual({c for c in im.getdata() if c[3]},colors)
     box=im.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),size)
if __name__=='__main__':unittest.main()
