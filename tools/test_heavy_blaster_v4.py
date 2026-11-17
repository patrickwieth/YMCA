"""V4 mechanical checks and native art-export regressions, not live-game tests."""
from pathlib import Path
from PIL import Image
import unittest,json,hashlib
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-04';PASS=STUDY/'pass-05';OUT=STUDY/'export'

class HeavyBlasterV4Tests(unittest.TestCase):
 def test_source_preserved_and_mechanism_connected(self):
  self.assertEqual(hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest(),'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  check=json.loads((PASS/'mechanism-check.json').read_text())
  self.assertGreaterEqual(len(check['coaxial_parts']),9);self.assertEqual(check['axis_yz'],[.57,2.13]);self.assertTrue(check['two_anchor_pads']);self.assertLess(check['gap_native_pixels'],.2)
  changes=json.loads((PASS/'changes.json').read_text());self.assertEqual(changes['source_texture_lod'],1024);self.assertFalse(changes['base_changed'])
  self.assertIn('V4 front root brace',changes['new_parts']);self.assertIn('V4 rear root brace',changes['new_parts'])
  for i in range(1,6):self.assertTrue((STUDY/f'pass-{i:02d}/heavy-blaster-v4.blend').exists())

 def test_fixed_base_is_pixel_identical(self):
  with Image.open(OUT/'heavy-blaster-base.png') as a,Image.open(BASE/'attempt-03/export/heavy-blaster-base.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  with Image.open(PASS/'base-160-raw.png') as a,Image.open(PASS/'base-rotation-control.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  self.assertEqual(json.loads((OUT/'attachment.json').read_text()),json.loads((BASE/'attempt-03/export/attachment.json').read_text()))

 def test_native_frames_metadata_and_source_palette(self):
  with Image.open(BASE/'heavy blaster 3.png') as src:colors={c for c in src.convert('RGBA').getdata() if c[3]==255}
  with Image.open(OUT/'heavy-blaster-base.png') as base,Image.open(OUT/'heavy-blaster-turret32.png') as sheet:
   self.assertEqual(sheet.size,(1280,640));self.assertEqual(sheet.text['FrameSize'],'160,160');self.assertEqual(sheet.text['FrameAmount'],'32');unique=set()
   for i in range(32):
    frame=sheet.crop(((i%8)*160,(i//8)*160,(i%8+1)*160,(i//8+1)*160)).convert('RGBA');unique.add(frame.tobytes())
    self.assertLessEqual({c for c in frame.getdata() if c[3]},colors);self.assertLessEqual(set(frame.getchannel('A').getdata()),{0,255})
    box=frame.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),160)
    with Image.open(PASS/f'turret-{(20+i)%32:03d}-raw.png') as raw:self.assertEqual(raw.size,(160,160))
    with Image.open(OUT/f'assembly-{i:02d}.png') as assembly:self.assertEqual(assembly.tobytes(),Image.alpha_composite(base.convert('RGBA'),frame).tobytes())
   self.assertEqual(len(unique),32)

if __name__=='__main__':unittest.main()
