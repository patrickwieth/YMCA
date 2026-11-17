"""Nod SAM art/export invariants. These checks are not artistic approval."""
from pathlib import Path
import hashlib,json,unittest
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];F=ROOT/'art-source/nod sam site';D=F/'pass-12';R=D/'review'
def rgba(p):
 with Image.open(p) as im:return np.array(im.convert('RGBA'))
class NodSamStudyTests(unittest.TestCase):
 def test_original_and_known_visible_base_pixels(self):
  source=F/'sam_site-1179307279-0.png';sha=hashlib.sha256(source.read_bytes()).hexdigest();original=rgba(source)
  report=json.loads((F/'source-study/extraction.json').read_text());self.assertEqual(sha,report['source_sha256'])
  mask=np.array(Image.open(F/'source-study/preserved-mask.png'))>0;base=rgba(R/'base.png')
  self.assertTrue(np.array_equal(base[mask],original[mask]));self.assertEqual(int(mask.sum()),report['visible_pixels_preserved'])
  inferred=np.array(Image.open(F/'source-study/inferred-mask.png'))>0;self.assertFalse((mask&inferred).any())
  self.assertTrue(np.array_equal(base,rgba(F/'source-study/base.png')))
  for x,y in ((20,20),(31,167),(232,170),(198,198),(127,215),(224,79),(130,45)):self.assertEqual(base[y,x,3],0)
  self.assertEqual(base[80,180,3],255) # fixed roof radar retained
 def test_twelve_real_archived_iterations(self):
  hashes=[];sha=hashlib.sha256((F/'sam_site-1179307279-0.png').read_bytes()).hexdigest()
  for n in range(1,13):
   p=F/f'pass-{n:02d}';g=json.loads((p/'geometry.json').read_text());self.assertEqual(g['source_sha256'],sha);self.assertEqual(g['missile_count'],4)
   self.assertGreater((p/'nod-sam.blend').stat().st_size,10000)
   hashes.append(hashlib.sha256((p/'review/turret-000.png').read_bytes()).hexdigest())
  self.assertEqual(len(set(hashes)),12)
 def test_native_full_rotation_and_fixed_composition(self):
  base=Image.open(R/'base.png').convert('RGBA');sheet=Image.open(R/'turret32.png').convert('RGBA')
  with Image.open(R/'turret32.png') as im:self.assertEqual(im.text['FrameSize'],'256,256');self.assertEqual(im.text['FrameAmount'],'32')
  palette={tuple(c[:3]) for c in rgba(F/'sam_site-1179307279-0.png').reshape(-1,4)}
  for i in range(32):
   turret=Image.open(R/f'turret-{i:03d}.png').convert('RGBA');box=turret.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),256)
   pixels=np.array(turret);self.assertTrue(set(np.unique(pixels[:,:,3])).issubset({0,255}))
   self.assertTrue({tuple(c[:3]) for c in pixels[pixels[:,:,3]>0]}.issubset(palette))
   self.assertEqual(Image.alpha_composite(base,turret).tobytes(),Image.open(R/f'assembly-{i:03d}.png').convert('RGBA').tobytes())
   j=(i-4)%32;self.assertEqual(sheet.crop((j%8*256,j//8*256,(j%8+1)*256,(j//8+1)*256)).tobytes(),turret.tobytes())
 def test_reference_registration_and_balanced_rack(self):
  g=json.loads((D/'geometry.json').read_text());self.assertAlmostEqual(g['pivot_pixel'][0],119,places=4);self.assertAlmostEqual(g['pivot_pixel'][1],115,places=4)
  self.assertLess(abs(np.mean(g['missile_tail_positions'],axis=0)[0]),.01)
  self.assertFalse(g['runtime_installed']);self.assertTrue(g['radar_holdout'])
  names=[o['name'] for o in g['objects']]
  for k in range(4):self.assertIn(f'Missile {k} red ogive',names);self.assertIn(f'Missile {k} rear exhaust lip',names)
if __name__=='__main__':unittest.main()
