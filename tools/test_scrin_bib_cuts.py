"""Source preservation/native-pixel checks for the review-only Scrin bib cuts."""
from pathlib import Path
import hashlib,json,unittest
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'art-source/scrin bib'
class ScrinBibCuts(unittest.TestCase):
 def test_original_unchanged(self):
  old=json.loads((ART/'studies/initial-cuts/cut-paste-manifest.json').read_text())
  self.assertEqual(hashlib.sha256((ART/old['source']).read_bytes()).hexdigest(),old['source_sha256'])
 def test_seam_repair_preserves_shapes(self):
  for n in (1,2,3):
   with Image.open(ART/f'bib{n}x{n}.png') as current,Image.open(ART/'studies/before-seam-repair'/f'bib{n}x{n}.png') as before:
    self.assertEqual(current.size,before.size);self.assertEqual(current.getchannel('A').tobytes(),before.getchannel('A').tobytes())
    self.assertNotEqual(current.tobytes(),before.tobytes())
 def test_only_unscaled_source_colors_and_solid_interior(self):
  meta=json.loads((ART/'cut-paste-manifest.json').read_text())
  with Image.open(ART/meta['source']) as im:a=np.array(im.convert('RGBA'))
  colors={tuple(p) for p in a[a[:,:,3]>0]}
  for e in meta['outputs']:
   with self.subTest(name=e['name']),Image.open(ART/e['name']) as im:
    a=np.array(im);n=e['cells'][0];self.assertEqual(im.size,(128*n,64*n+26));self.assertEqual(im.getbbox(),(0,0,im.width,im.height))
    self.assertTrue({tuple(p) for p in a[a[:,:,3]>0]}<=colors)
    y,x=np.indices(a.shape[:2]);cx,cy=e['top_surface_center'];interior=abs(x-cx)+2*abs(y-cy)<=64*n-8
    self.assertTrue(np.all(a[:,:,3][interior]==255))
if __name__=='__main__':unittest.main()
