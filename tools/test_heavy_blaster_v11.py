"""Platform centering, stationary bearing and unchanged approved turret art."""
from pathlib import Path
from PIL import Image
import numpy as np,json,unittest,math
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-11-centering'
class CenteredPlatformTests(unittest.TestCase):
 def test_plinth_centroid_and_translation(self):
  g=json.loads((OUT/'geometry.json').read_text());x,y,z=g['model_translation'];self.assertAlmostEqual(20*(x+y)/math.sqrt(2),6,places=5);self.assertAlmostEqual(20*(x-y)/math.sqrt(8),4,places=5);self.assertEqual(z,0)
  cx,cy=g['plinth_centroid_frame_at_deck_height'];cy-=20*(g['pivot_height']-g['deck_height'])*math.sqrt(3)/2;px,py=g['pivot_frame'];self.assertLess(math.hypot(px-cx,py-cy),.5)

 def test_stationary_base_and_exact_turret_bytes(self):
  with Image.open(OUT/'base-centered-raw.png') as a,Image.open(OUT/'base-control-raw.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  self.assertEqual((OUT/'export/heavy-blaster-turret32.png').read_bytes(),(BASE/'attempt-10-preview/export/heavy-blaster-turret32.png').read_bytes())
  for n in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):self.assertEqual((OUT/'export'/n).read_bytes(),(ROOT/'mods/ca/bits/gdi/buildings/experimental'/n).read_bytes())

 def test_all_shifted_views_fit_visual_envelope(self):
  with Image.open(OUT/'base-centered.png') as im:base=np.array(im.convert('RGBA'))
  for i in range(32):
   with Image.open(BASE/'attempt-10-preview/160'/f'turret-{i:03d}.png') as im:turret=np.array(im.convert('RGBA'))
   for a,dx,dy in ((base,0,0),(turret,6,4)):
    yy,xx=np.where(a[:,:,3]>0);x=xx+.5-87+dx;y=yy+.5-104+dy;self.assertTrue(np.all(abs(x)<84));self.assertTrue(np.all(y<=42-.5*abs(x)));self.assertTrue(np.all(-42+.5*abs(x)-y<=66))
if __name__=='__main__':unittest.main()
