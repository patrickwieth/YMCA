"""Broader V5, true native 320px renders and source-perimeter base restoration."""
from pathlib import Path
from PIL import Image
import unittest,json,hashlib,numpy as np
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-05';OUT=STUDY/'export'

class HeavyBlasterV5Tests(unittest.TestCase):
 def test_source_preservation_and_real_width_change(self):
  self.assertEqual(hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  geometry=json.loads((STUDY/'geometry.json').read_text());self.assertEqual(geometry['native_pixels_per_world_unit'],40)
  hull=next(v for v in geometry['width_changes'] if v['object']=='closed barrel and receiver housing')
  self.assertAlmostEqual((hull['after_x'][1]-hull['after_x'][0])/(hull['before_x'][1]-hull['before_x'][0]),1.65,places=5)
  self.assertTrue((STUDY/'heavy-blaster-v5.blend').exists())
  for name in ('roof-pass-01','roof-pass-02','roof-pass-03'):self.assertTrue((STUDY/name/'heavy-blaster-v5.blend').exists())

 def test_base_visible_original_pixels_and_stationarity(self):
  with Image.open(STUDY/'base-modeled-raw.png') as a,Image.open(STUDY/'base-control-raw.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  with Image.open(BASE/'heavy blaster 3.png') as src,Image.open(STUDY/'stationary-source-cut-mask.png') as mask,Image.open(OUT/'heavy-blaster-base.png') as base:
   reference=Image.new('RGBA',(320,320));reference.alpha_composite(src.convert('RGBA').resize((256,256),Image.Resampling.NEAREST),(32,32))
   selected=np.array(mask)>0;self.assertGreater(int(selected.sum()),10000)
   np.testing.assert_array_equal(np.array(base)[selected],np.array(reference)[selected])
   # Original muzzle lip must not become a permanent decoration on the base.
   self.assertEqual(base.getpixel((68,195))[3],0)

 def test_native_export_and_selection_bounds(self):
  r=json.loads((OUT/'binding.json').read_text());self.assertEqual(r['frame_size'],[320,320]);self.assertFalse(r['turret_resampled']);self.assertEqual(r['scale'],1)
  with Image.open(BASE/'heavy blaster 3.png') as src:colors={c for c in src.convert('RGBA').getdata() if c[3]==255}
  with Image.open(OUT/'heavy-blaster-base.png') as base,Image.open(OUT/'heavy-blaster-turret32.png') as sheet:
   self.assertEqual(sheet.size,(2560,1280));self.assertEqual(sheet.text['FrameSize'],'320,320');self.assertEqual(sheet.text['FrameAmount'],'32');unique=set()
   for i in range(32):
    im=sheet.crop(((i%8)*320,(i//8)*320,(i%8+1)*320,(i//8+1)*320)).convert('RGBA');unique.add(im.tobytes())
    self.assertLessEqual({c for c in im.getdata() if c[3]},colors);self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255})
    with Image.open(STUDY/f'turret-{(20+i)%32:03d}-raw.png') as raw:self.assertEqual(raw.size,(320,320))
    box=im.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),320)
    a=np.array(Image.alpha_composite(base.convert('RGBA'),im));y,x=np.where(a[:,:,3]>0);sx=x+.5-174;sy=y+.5-208
    self.assertTrue(np.all(abs(sx)<168));self.assertTrue(np.all(sy<=84-.5*abs(sx)));self.assertLessEqual(float((-84+.5*abs(sx)-sy).max()),r['selection_height'])
   self.assertEqual(len(unique),32)

if __name__=='__main__':unittest.main()
