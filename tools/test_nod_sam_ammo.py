"""Visibility-only SAM ammo exports; frozen approved4-round art and geometry."""
from pathlib import Path
import hashlib,json,unittest
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];F=ROOT/'art-source/nod sam site';A=F/'ammo-variants';S=F/'pass-12'
def pixels(p):
 with Image.open(p) as im:return np.array(im.convert('RGBA'))
class NodSamAmmoTests(unittest.TestCase):
 def test_approved_full_state_and_render_control(self):
  manifest=json.loads((A/'manifest.json').read_text())
  self.assertEqual(hashlib.sha256((S/'nod-sam.blend').read_bytes()).hexdigest(),manifest['approved_blend_sha256'])
  self.assertEqual((A/'turret-4.png').read_bytes(),(S/'review/turret32.png').read_bytes())
  self.assertTrue(np.array_equal(pixels(A/'4/full-control-raw.png'),pixels(S/'turret-000-raw.png')))
  for i in range(32):self.assertEqual((A/'4/review'/f'turret-{i:03d}.png').read_bytes(),(S/'review'/f'turret-{i:03d}.png').read_bytes())
 def test_visibility_only_and_consistent_slots(self):
  manifest=json.loads((A/'manifest.json').read_text());previous=set()
  for count in range(5):
   g=json.loads((A/str(count)/'geometry.json').read_text());loaded=set(g['loaded_slots'])
   self.assertEqual(len(loaded),count);self.assertTrue(previous.issubset(loaded));previous=loaded
   self.assertEqual(g['object_geometry_sha256'],manifest['object_geometry_sha256'])
   self.assertTrue(g['visibility_only']);self.assertEqual(len(g['hidden_objects']),(4-count)*15)
   self.assertTrue(set(g['hidden_objects']).issubset(manifest['slot_objects']))
   self.assertFalse(any(name.startswith(('Launch rail','Rail saddle','Saddle fastener')) for name in g['hidden_objects']))
 def test_all_states_full_rotation_and_fixed_base(self):
  base=pixels(S/'review/base.png')
  for count in range(5):
   with Image.open(A/f'turret-{count}.png') as sheet:
    self.assertEqual(sheet.text['FrameSize'],'256,256');self.assertEqual(sheet.text['FrameAmount'],'32')
    self.assertTrue(np.array_equal(pixels(A/str(count)/'review/base.png'),base))
    for i in range(32):
     p=pixels(A/str(count)/'review'/f'turret-{i:03d}.png');self.assertTrue(set(np.unique(p[:,:,3])).issubset({0,255}));box=Image.fromarray(p).getbbox();self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),256)
     j=(i-4)%32;self.assertTrue(np.array_equal(np.array(sheet.crop((j%8*256,j//8*256,(j%8+1)*256,(j//8+1)*256)).convert('RGBA')),p))
     if count==0:
      g=json.loads((A/'0/geometry.json').read_text());ids=pixels(A/'0'/f'turret-{i:03d}-ids.png');labels=ids[:,:,0].astype(int)+256*ids[:,:,1].astype(int)
      hidden_ids=[v for k,v in g['groups'].items() if k.startswith('Missile') or k=='Witness stripes']
      self.assertFalse(np.isin(labels[ids[:,:,3]>128],hidden_ids).any())
if __name__=='__main__':unittest.main()
