"""Source-sampled GDI gold and brighter roof emission, previews only."""
from art.ion_art_safety import assert_preserved_ion_art
from pathlib import Path
from PIL import Image
import json,hashlib,numpy as np,unittest
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-09-preview';OUT=BASE/'attempt-10-preview'
class GoldGlowTests(unittest.TestCase):
 def test_source_colors_and_originals(self):
  info=json.loads((OUT/'validation.json').read_text());gold=set()
  for name,ref in info['gdi_sources'].items():
   p=ROOT/'mods/ca/bits/gdi/buildings'/name;self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),ref['sha256'])
   with Image.open(p) as im:colors={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
   sampled={tuple(c) for c in ref['sampled_colors']};self.assertLessEqual(sampled,colors);gold|=sampled
  for m in info['color_mapping']:self.assertIn(tuple(m['after']),gold)
  p=BASE/'heavy blaster 3.png';self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  with Image.open(p) as im:colors={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
  for c in info['glow_palette'].values():self.assertIn(tuple(c),colors)

 def test_changes_scoped_to_armor_and_glow_contours_unchanged(self):
  info=json.loads((OUT/'validation.json').read_text());mapping={tuple(m['before']):tuple(m['after']) for m in info['color_mapping']}
  for size in (160,192):
   for i in range(32):
    with Image.open(OLD/str(size)/f'turret-{i:03d}.png') as im:a=np.array(im.convert('RGBA'))
    with Image.open(OUT/str(size)/f'turret-{i:03d}.png') as im:b=np.array(im.convert('RGBA'))
    with Image.open(OUT/str(size)/f'glow-mask-{i:03d}.png') as im:glow=np.array(im)>0
    with Image.open(OLD/str(size)/f'contour-mask-{i:03d}.png') as im:edge=np.array(im)>0
    self.assertEqual(b.shape,(size,size,4));self.assertTrue(np.array_equal(a[:,:,3],b[:,:,3]));self.assertTrue(np.array_equal(a[edge],b[edge]))
    expected=a.copy()
    for old,new in mapping.items():expected[np.all(a[:,:,:3]==old,axis=2)&(a[:,:,3]>0),:3]=new
    self.assertTrue(np.array_equal(expected[~glow],b[~glow]));self.assertTrue(np.all(a[:,:,3][glow]>0))
  for f in info['frames']:self.assertGreater(f['bright_pixels'],0)

 def test_approved_installation_and_preserved_historical_assets(self):
  assert_preserved_ion_art(self)
  self.assertFalse(json.loads((OUT/'validation.json').read_text())['installed'])
if __name__=='__main__':unittest.main()
