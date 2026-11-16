"""Original-pixel hybrid export checks, distinct from historical full-3D controls."""
import json,unittest
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];STUDY=ROOT/'art-source/allies flak/pass-15'
class OriginalBaseTests(unittest.TestCase):
 def test_rear_breech_only_change(self):
  report=json.loads((STUDY/'rear-breech-check.json').read_text());self.assertTrue(report['other_meshes_and_base_unchanged']);self.assertLess(report['max_error'],1e-6);self.assertEqual(report['barrel_group_aft'],.28)
 def test_visible_source_pixels_not_repainted(self):
  source=np.array(Image.open(next(STUDY.parent.glob('*.png'))).convert('RGBA'));visible=np.array(Image.open(STUDY/'original-base-preserved-pixels.png'));base=np.array(Image.open(STUDY/'review/base.png'));mask=visible[:,:,3]>0
  self.assertTrue(np.array_equal(source[mask],visible[mask]));self.assertTrue(np.array_equal(base[70:198,64:192][mask],visible[mask]))
  self.assertGreater(mask.sum(),2500)
 def test_fixed_base_and_32_rotating_turrets(self):
  base=Image.open(STUDY/'review/base.png').convert('RGBA')
  for i in range(32):
   turret=Image.open(STUDY/f'review/turret-{i:03d}.png').convert('RGBA');assembly=Image.open(STUDY/f'review/assembly-{i:03d}.png').convert('RGBA')
   self.assertEqual(Image.alpha_composite(base,turret).tobytes(),assembly.tobytes())
  report=json.loads((STUDY/'original-base.json').read_text());self.assertEqual(report['scale'],1);self.assertFalse(report['runtime_integrated'])
if __name__=='__main__':unittest.main()
