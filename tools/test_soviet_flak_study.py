"""Soviet flak sizing/export checks, not artistic approval or SAM integration."""
from pathlib import Path
import hashlib,json,unittest
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/soviet flak';STUDY=ART/'pass-11';REVIEW=STUDY/'review'
class SovietFlakStudyTests(unittest.TestCase):
 def test_source_and_one_cell_ground_envelope(self):
  g=json.loads((STUDY/'geometry.json').read_text())
  for file,key in (('flak.png','source_sha256'),('flak base.png','base_source_sha256')):
   self.assertEqual(hashlib.sha256((ART/file).read_bytes()).hexdigest(),g[key])
  self.assertEqual(g['ground_footprint'],'1x1');self.assertLess(g['ground_diamond_max'],64);self.assertEqual(g['runtime_scale'],1);self.assertFalse(g['game_installed'])
 def test_separate_axial_recoil_preserves_accepted_shape(self):
  report=json.loads((STUDY/'recoil-check.json').read_text());self.assertTrue(report['rest_geometry_exact']);self.assertTrue(report['only_barrel_group_moves']);self.assertTrue(report['returns_to_rest']);self.assertEqual(report['facings_checked'],32);self.assertLess(report['max_axial_error'],1e-5)
  rig=json.loads((STUDY/'recoil.json').read_text());self.assertIn('Hollow main barrel',rig['barrel_members']);self.assertFalse(any('yoke' in n.lower() or n.startswith('Fixed') for n in rig['barrel_members']))
  self.assertEqual(len(list((STUDY/'recoil').glob('assembly-*.png'))),32)
 def test_closed_geometry_and_main_bore(self):
  report=json.loads((STUDY/'geometry-check.json').read_text());self.assertEqual(report['pass'],11);self.assertLess(report['ground_diamond_max'],64);self.assertTrue(report['fixed_and_moving_parentage']);self.assertEqual(len(report['hollow_barrels']),1);self.assertGreater(report['closed_meshes_outward'],0)
  g=json.loads((STUDY/'geometry.json').read_text());self.assertEqual(g['barrel_elevation_degrees'],42);self.assertLess(g['lower_cylinder_axis'][2],0)
 def test_palette_native_frames_and_engine_order(self):
  colors=set()
  for file in ('flak.png','flak base.png'):
   with Image.open(ART/file) as im:colors|={c[:3] for c in im.convert('RGBA').getdata() if min(c[:3])<243}
  with Image.open(REVIEW/'turret32.png') as sheet:
   self.assertEqual(sheet.text['FrameSize'],'192,192');self.assertEqual(sheet.text['FrameAmount'],'32');self.assertEqual(sheet.size,(1536,768))
   for i in range(32):
    with Image.open(REVIEW/f'turret-{(20+i)%32:03d}.png') as im:
     a=np.array(im.convert('RGBA'));self.assertLessEqual(set(np.unique(a[:,:,3])),{0,255});self.assertLessEqual({tuple(c) for c in a[:,:,:3][a[:,:,3]>0]},colors)
     b=im.getbbox();self.assertGreater(min(b[:2]),0);self.assertLess(max(b[2:]),192)
     self.assertEqual(sheet.crop(((i%8)*192,(i//8)*192,(i%8+1)*192,(i//8+1)*192)).tobytes(),im.tobytes())
 def test_fixed_base_and_exact_separate_layer_controls(self):
  with Image.open(STUDY/'base-raw.png') as im:base=im.convert('RGBA')
  with Image.open(STUDY/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
  for i in (0,4,12,20,28):
   with Image.open(STUDY/f'turret-{i:03d}-raw.png') as im:composite=Image.alpha_composite(base,im.convert('RGBA'))
   with Image.open(STUDY/('assembly-reference-raw.png' if i==0 else f'assembly-control-{i:03d}-raw.png')) as im:self.assertEqual(composite.tobytes(),im.convert('RGBA').tobytes())
if __name__=='__main__':unittest.main()
