"""Consistent line ownership instead of screen-dependent doubled diagonals."""
from art.ion_art_safety import assert_preserved_ion_art
from pathlib import Path
from PIL import Image
import numpy as np,json,unittest
from art.contour_geometry import owned_boundaries
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OLD=BASE/'attempt-07-preview';OUT=BASE/'attempt-09-preview';MASKS=BASE/'attempt-08-preview'
class StableContourTests(unittest.TestCase):
 def test_diagonal_is_one_sided_and_rotation_mirror_equivariant(self):
  y,x=np.indices((17,17));labels=np.where(x>=y,2,1);labels[[0,-1],:]=0;labels[:,[0,-1]]=0;p=np.zeros_like(labels,dtype=bool);edge=owned_boundaries(labels,p)
  self.assertTrue(np.all(labels[edge]==2))
  for i in range(4):self.assertTrue(np.array_equal(np.rot90(edge,i),owned_boundaries(np.rot90(labels,i),np.rot90(p,i))))
  self.assertTrue(np.array_equal(edge[:,::-1],owned_boundaries(labels[:,::-1],p[:,::-1])))
  self.assertEqual(int(edge[2:15,2:15].sum()),13)

 def test_protected_strip_keeps_pixels(self):
  labels=np.zeros((12,12),int);labels[1:-1,1:-1]=1;labels[1:-1,5:7]=2;p=labels==2;edge=owned_boundaries(labels,p)
  self.assertFalse(np.any(edge&p));self.assertTrue(np.any(edge))

 def test_finished_masks_alpha_palette_and_outer_silhouette(self):
  ink=json.loads((OUT/'validation.json').read_text())['ink_rgb']
  with Image.open(BASE/'heavy blaster 3.png') as im:palette={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
  for size in (160,192):
   for i in range(32):
    with Image.open(OLD/str(size)/f'turret-{i:03d}.png') as im:a=np.array(im.convert('RGBA'))
    with Image.open(OUT/str(size)/f'turret-{i:03d}.png') as im:b=np.array(im.convert('RGBA'))
    with Image.open(OUT/str(size)/f'contour-mask-{i:03d}.png') as im:mask=np.array(im)>0
    self.assertEqual(b.shape,(size,size,4));self.assertTrue(np.array_equal(a[:,:,3],b[:,:,3]));self.assertTrue(np.array_equal(mask,np.any(a!=b,axis=2)));self.assertTrue(np.all(b[:,:,:3][mask]==ink))
    solid=a[:,:,3]>0;outer=np.zeros_like(solid)
    for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):outer|=solid&~np.roll(solid,(dy,dx),(0,1))
    self.assertTrue(np.array_equal(a[outer],b[outer]));self.assertLessEqual({tuple(c) for c in b[:,:,:3][solid]},palette)
  assert_preserved_ion_art(self)
if __name__=='__main__':unittest.main()
