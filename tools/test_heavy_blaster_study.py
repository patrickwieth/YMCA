"""Checks for the preliminary Heavy Blaster study, not a game-integration claim."""
from pathlib import Path
from PIL import Image
import json,hashlib,unittest
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'art-source/gdi ion cannon';OUT=BASE/'attempt-01'

class HeavyBlasterStudyTests(unittest.TestCase):
 def test_source_and_cut_provenance(self):
  source=BASE/'heavy blaster 3.png';digest=hashlib.sha256(source.read_bytes()).hexdigest()
  report=json.loads((OUT/'texture-provenance.json').read_text())
  self.assertEqual(digest,report['source_sha256'])
  self.assertEqual(digest,'54893beff2c0dc4721962ae40029fc072313ba1af910be638a8fd9fe198c02b9')
  with Image.open(source) as im:colors=set(im.convert('RGBA').getdata())
  for name,cut in report['cuts'].items():
   with Image.open(OUT/'textures'/f'{name}.png') as im:
    self.assertEqual(list(im.size),cut['size'])
    self.assertLessEqual({c for c in im.convert('RGBA').getdata() if c[3]},colors)

 def test_four_real_rendered_study_views(self):
  self.assertTrue((OUT/'heavy-blaster-v1.blend').is_file())
  with Image.open(BASE/'heavy blaster 3.png') as im:colors=set(im.convert('RGBA').getdata())
  unique=set()
  for angle in (0,45,180,315):
   with Image.open(OUT/f'render-{angle:03d}-raw.png') as raw:self.assertEqual(raw.size,(320,320))
   with Image.open(OUT/f'study-{angle:03d}.png') as im:
    im=im.convert('RGBA');self.assertEqual(im.size,(320,320))
    self.assertLessEqual(set(im.getchannel('A').getdata()),{0,255})
    self.assertLessEqual({c for c in im.getdata() if c[3]},colors)
    box=im.getbbox();self.assertTrue(min(box[:2])>0 and max(box[2:])<320)
    unique.add(im.tobytes())
  self.assertEqual(len(unique),4)

if __name__=='__main__':unittest.main()
