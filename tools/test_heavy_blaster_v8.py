"""Native geometry contours preserve alpha, source palette and fine features."""
from art.ion_art_safety import assert_preserved_ion_art
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,unittest
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-07-preview';OUT=BASE/'attempt-08-preview'
class ContourTests(unittest.TestCase):
 def test_only_mask_pixels_change_and_fine_features_survive(self):
  info=json.loads((OUT/'validation.json').read_text());ink=tuple(info['ink_rgb']);labels=json.loads((OUT/'surface-labels.json').read_text());protected={int(k) for k,v in labels.items() if v['protect_cyan'] or 'straight left metal edge' in v['object'] or 'straight right metal edge' in v['object'] or 'bearing rim' in v['object']}
  with Image.open(BASE/'heavy blaster 3.png') as im:palette={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
  self.assertIn(ink,palette)
  for size in (160,192):
   for idx in range(32):
    def load(study,name):
     with Image.open(study/str(size)/name) as im:return np.array(im.convert('RGBA'))
    a=load(OLD,f'turret-{idx:03d}.png');b=load(OUT,f'turret-{idx:03d}.png');self.assertEqual(b.shape,(size,size,4));self.assertTrue(np.array_equal(a[:,:,3],b[:,:,3]))
    with Image.open(OUT/str(size)/f'contour-mask-{idx:03d}.png') as im:mask=np.array(im)>0
    changed=np.any(a!=b,axis=2);self.assertTrue(np.array_equal(changed,mask));self.assertTrue(np.all(b[:,:,:3][changed]==ink));self.assertTrue(np.all(b[:,:,:3][changed].astype(int).sum(axis=1)<a[:,:,:3][changed].astype(int).sum(axis=1)))
    ids=load(OUT,f'surface-id-{idx:03d}.png');code=ids[:,:,:3].astype(int)//16;label=code[:,:,0]+16*code[:,:,1]+256*code[:,:,2];label[ids[:,:,3]<128]=0;p=np.isin(label,list(protected));self.assertTrue(np.array_equal(a[p],b[p]))
    self.assertLessEqual({tuple(c) for c in b[:,:,:3][b[:,:,3]>0]},palette)

 def test_base_only_outer_contour_changes(self):
  for size in (160,192):
   with Image.open(OLD/str(size)/'base.png') as im:a=np.array(im.convert('RGBA'))
   with Image.open(OUT/str(size)/'base.png') as im:b=np.array(im.convert('RGBA'))
   solid=a[:,:,3]>0;edge=np.zeros_like(solid)
   for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):edge|=solid&~np.roll(solid,(dy,dx),(0,1))
   self.assertTrue(np.array_equal(a[:,:,3],b[:,:,3]));self.assertFalse(np.any(np.any(a!=b,axis=2)&~edge))

 def test_original_and_active_assets_unchanged(self):
  self.assertEqual(hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  assert_preserved_ion_art(self)
  self.assertFalse(json.loads((OUT/'validation.json').read_text())['installed'])
if __name__=='__main__':unittest.main()
