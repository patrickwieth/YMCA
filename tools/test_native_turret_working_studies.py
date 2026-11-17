"""Export regressions for working art, not artistic approval or live-game tests."""
from pathlib import Path
import hashlib,json,unittest
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
STUDIES=(('gdi aa gun',11,192,4),('nod laser turret',10,256,20),('allies flak',13,256,20),('gdi sonic turret',22,224,12))
class WorkingTurretExportTests(unittest.TestCase):
 def test_texture_updates_preserve_accepted_meshes(self):
  for folder,iteration in (('gdi aa gun',11),('nod laser turret',8)):
   report=json.loads((ROOT/'art-source'/folder/f'pass-{iteration:02d}'/'texture-only-geometry-check.json').read_text())
   self.assertTrue(report['identical']);self.assertEqual(report['current_pass'],iteration);self.assertGreater(len(report['mesh_fingerprints']),50)
 def test_nod_requested_muzzle_change_is_isolated(self):
  report=json.loads((ROOT/'art-source/nod laser turret/pass-09/muzzle-only-geometry-check.json').read_text())
  self.assertTrue(report['other_geometry_identical']);self.assertEqual(len(report['changed_objects']),7)
 def test_latest_material_only_updates_preserve_forms_and_sonic_base(self):
  for folder,iteration in (('nod laser turret',10),('gdi sonic turret',20)):
   report=json.loads((ROOT/'art-source'/folder/f'pass-{iteration:02d}'/'texture-only-geometry-check.json').read_text());self.assertTrue(report['identical'])
  sonic=ROOT/'art-source/gdi sonic turret'
  self.assertEqual((sonic/'pass-19/review/base.png').read_bytes(),(sonic/'pass-20/review/base.png').read_bytes())
  for i in range(32):
   with Image.open(sonic/f'pass-19/review/turret-{i:03d}.png') as old,Image.open(sonic/f'pass-20/review/turret-{i:03d}.png') as new:self.assertEqual(old.getchannel('A').tobytes(),new.getchannel('A').tobytes())
 def test_sonic_requested_hardware_preserves_main_form(self):
  study=ROOT/'art-source/gdi sonic turret/pass-22'
  report=json.loads((study/'hardware-geometry-check.json').read_text())
  self.assertTrue(report['retained_meshes_identical']);self.assertTrue(report['fixed_cables_stationary']);self.assertTrue(report['large_emitters_unchanged']);self.assertEqual(len(report['removed']),3)
  self.assertIn('SONIC reference concave radar reflector',report['added']);self.assertIn('SONIC fixed supply loop 0',report['added'])
  self.assertEqual(json.loads((study/'geometry.json').read_text())['surface_id_channels'],2)
 def test_allied_flak_geometry_and_wide_surface_ids(self):
  study=ROOT/'art-source/allies flak/pass-13'
  geometry=json.loads((study/'geometry.json').read_text());self.assertEqual(geometry['surface_id_channels'],2)
  report=json.loads((study/'geometry-check.json').read_text());self.assertEqual(len(report['hollow_barrels']),4);self.assertLess(report['ground_diamond_max'],64);self.assertTrue(report['fixed_and_moving_parentage']);self.assertGreater(report['closed_meshes_outward'],0)
  with Image.open(study/'turret-000-ids.png') as im:self.assertGreater(np.asarray(im)[:,:,1].max(),0)
 def test_sources_native_frames_palette_and_metadata(self):
  for folder,iteration,size,start in STUDIES:
   with self.subTest(folder=folder):
    path=ROOT/'art-source'/folder;study=path/f'pass-{iteration:02d}';review=study/'review';g=json.loads((study/'geometry.json').read_text());source=next(path.glob('*.png'));self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),g['source_sha256'])
    with Image.open(source) as im:colors={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
    with Image.open(review/'turret32.png') as sheet:
     self.assertEqual(sheet.size,(size*8,size*4));self.assertEqual(sheet.text['FrameSize'],f'{size},{size}');self.assertEqual(sheet.text['FrameAmount'],'32')
     for i in range(32):
      with Image.open(review/f'turret-{(start+i)%32:03d}.png') as im:
       self.assertEqual(im.size,(size,size));a=np.array(im.convert('RGBA'));self.assertLessEqual(set(np.unique(a[:,:,3])),{0,255});self.assertLessEqual({tuple(c) for c in a[:,:,:3][a[:,:,3]>0]},colors);bounds=im.getbbox();self.assertGreater(min(bounds[:2]),0);self.assertLess(max(bounds[2:]),size);self.assertEqual(sheet.crop(((i%8)*size,(i//8)*size,(i%8+1)*size,(i//8+1)*size)).tobytes(),im.tobytes())
 def test_stationarity_and_full_3d_layer_composites(self):
  for folder,iteration,size,start in STUDIES:
   path=ROOT/'art-source'/folder/f'pass-{iteration:02d}'
   with Image.open(path/'base-raw.png') as im:base=im.convert('RGBA')
   with Image.open(path/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
   for idx in ((0,) if folder.startswith('gdi ') else (0,4,12,20,28)):
    with Image.open(path/f'turret-{idx:03d}-raw.png') as im:composite=Image.alpha_composite(base,im.convert('RGBA'))
    with Image.open(path/('assembly-reference-raw.png' if idx==0 else f'assembly-control-{idx:03d}-raw.png')) as im:self.assertEqual(composite.tobytes(),im.convert('RGBA').tobytes())
if __name__=='__main__':unittest.main()
