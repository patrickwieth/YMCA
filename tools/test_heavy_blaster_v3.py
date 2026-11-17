"""Validate separate native V3 art exports; this does not claim live game coverage."""
from pathlib import Path
from PIL import Image
import unittest,json,hashlib,math
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';STUDY=BASE/'attempt-03';OUT=STUDY/'export';PASS=STUDY/'pass-05'

class HeavyBlasterV3Tests(unittest.TestCase):
 def test_original_and_prior_attempts_preserved(self):
  sha=hashlib.sha256((BASE/'heavy blaster 3.png').read_bytes()).hexdigest()
  self.assertEqual(sha,'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  self.assertEqual(sha,json.loads((OUT/'validation.json').read_text())['source_sha256'])
  for i in range(1,6):self.assertTrue((STUDY/f'pass-{i:02d}/heavy-blaster-v3.blend').is_file())
  for path in ('attempt-01/heavy-blaster-v1.blend','attempt-02/heavy-blaster-v2.blend'):self.assertTrue((BASE/path).is_file())

 def test_actual_blender_separation_and_stationary_pixels(self):
  r=json.loads((PASS/'separation-check.json').read_text());self.assertTrue(r['base_world_matrices_unchanged']);self.assertGreater(r['stationary_meshes'],5);self.assertGreater(r['rotating_meshes'],10)
  with Image.open(PASS/'base-control-0.png') as a,Image.open(PASS/'base-control-123.75.png') as b,Image.open(PASS/'base-160-raw.png') as exported:
   self.assertEqual(a.tobytes(),b.tobytes());self.assertEqual(a.tobytes(),exported.tobytes())

 def test_native_sheet_metadata_palette_alpha_and_frames(self):
  with Image.open(BASE/'heavy blaster 3.png') as source:colors={c for c in source.convert('RGBA').getdata() if c[3]==255}
  with Image.open(OUT/'heavy-blaster-base.png') as base,Image.open(OUT/'heavy-blaster-turret32.png') as sheet:
   self.assertEqual(base.size,(160,160));self.assertEqual(base.text['FrameAmount'],'1')
   self.assertEqual(sheet.size,(1280,640));self.assertEqual(sheet.text['FrameSize'],'160,160');self.assertEqual(sheet.text['FrameAmount'],'32')
   images=[base.convert('RGBA')];unique=set()
   for i in range(32):
    frame=sheet.crop(((i%8)*160,(i//8)*160,(i%8+1)*160,(i//8+1)*160)).convert('RGBA');images.append(frame);unique.add(frame.tobytes())
    with Image.open(OUT/f'assembly-{i:02d}.png') as composite:self.assertEqual(Image.alpha_composite(base.convert('RGBA'),frame).tobytes(),composite.tobytes())
    with Image.open(PASS/f'rotation-{(20+i)%32:03d}-raw.png') as raw:self.assertEqual(raw.size,(160,160))
   self.assertEqual(len(unique),32)
   for im in images:
    self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255});self.assertLessEqual({c for c in im.getdata() if c[3]},colors)
    box=im.getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),160)

 def test_facing_and_shared_pivot(self):
  r=json.loads((OUT/'validation.json').read_text());a=json.loads((OUT/'attachment.json').read_text())
  self.assertEqual(a['reference_SW_frame'],12);self.assertTrue(a['base_and_turret_canvas_origin_identical']);self.assertEqual(r['runtime_png_scale'],1);self.assertFalse(r['sprite_resampling'])
  for frame in r['frames']:
   angle=math.radians(frame['blender_degrees']);x,y=math.sin(angle),-math.cos(angle);sx=(x+y)/math.sqrt(2);sy=(x-y)/math.sqrt(8)
   bearing=math.degrees(math.atan2(-sx,-2*sy))%360
   self.assertLess(abs((bearing-frame['engine_facing']*360/1024+180)%360-180),1e-6)
  self.assertLessEqual(r['two_layer_vs_3d_assembly_different_pixels'],10)

if __name__=='__main__':unittest.main()
