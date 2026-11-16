"""Material-study invariants; these checks do NOT judge artistic likeness."""
from pathlib import Path
from PIL import Image
import unittest,json,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/scrin shard launcher';P=ART/'pass-43';OLD=ART/'pass-30'
class ShardMaterialTests(unittest.TestCase):
 def test_saved_geometry_and_rotation_preserved(self):
  a=json.loads((P/'saved-scene-audit.json').read_text());self.assertTrue(a['material_only_geometry_unchanged']);self.assertEqual(a['geometry_reference_pass'],30)
  self.assertTrue(a['fixed_vertices_unchanged']);self.assertTrue(a['all_moving_parts_yaw_parented']);self.assertEqual(a['fixed_shards'],4)
  self.assertEqual(a['blend_sha256'],hashlib.sha256((P/'shard-launcher.blend').read_bytes()).hexdigest())
  sweep=json.loads((P/'swept-collision-audit.json').read_text());self.assertGreaterEqual(sweep['samples'],64);self.assertFalse(sweep['collisions'])
  old=json.loads((OLD/'geometry.json').read_text());new=json.loads((P/'geometry.json').read_text())
  for key in ('frame_size','ground_origin','pivot','muzzle_model','elevation_degrees'):self.assertEqual(old[key],new[key])
 def test_alpha_unchanged_and_all_native_frames_retained(self):
  for f in P.glob('*-raw.png'):
   if f.name.startswith('native-full-'):
    facing=int(f.name.split('-')[2])
    with Image.open(OLD/'base-raw.png') as b,Image.open(OLD/f'turret-{facing:02d}-raw.png') as t:old=Image.alpha_composite(b.convert('RGBA'),t.convert('RGBA'))
   else:old=Image.open(OLD/f.name).convert('RGBA')
   with Image.open(f) as new:self.assertEqual(new.getchannel('A').tobytes(),old.getchannel('A').tobytes(),f.name)
  for i in range(32):
   with Image.open(P/f'review/turret-{i:02d}.png') as im:
    self.assertEqual(im.size,(384,384));bbox=im.getbbox();self.assertGreater(min(bbox[:2]),0);self.assertLess(max(bbox[2:]),384)
 def test_raw_split_occlusion_and_static_base(self):
  with Image.open(P/'base-raw.png') as im:base=im.convert('RGBA')
  with Image.open(P/'base-control-raw.png') as control:self.assertEqual(base.tobytes(),control.convert('RGBA').tobytes())
  for n in range(32):
   with Image.open(P/f'turret-{n:02d}-raw.png') as turret,Image.open(P/f'native-full-{n:02d}-raw.png') as full,Image.open(P/f'independent-parts/turret-{n:02d}-raw.png') as independent:
    self.assertEqual(Image.alpha_composite(base,turret.convert('RGBA')).tobytes(),full.convert('RGBA').tobytes())
    self.assertEqual(turret.getchannel('A').tobytes(),independent.getchannel('A').tobytes())
 def test_crystal_cuts_exclude_foreign_gray_armor(self):
  source=np.array(Image.open(ART/'shard_launcher-332414650-0.png').convert('RGBA'));colors=set(map(tuple,source[source[:,:,3]>250,:3]))
  for name in ('spear','battery0','battery1','battery2','battery3'):
   image=np.array(Image.open(ART/'material-reference/source38'/(name+'-facets.png')));rgb=image[:,:,:3].astype(int)
   self.assertLessEqual(set(map(tuple,rgb.reshape(-1,3))),colors);self.assertTrue(np.all(rgb[:,:,1]+rgb[:,:,2]-2*rgb[:,:,0]>100))
 def test_organic_fields_cover_broad_surfaces_without_changing_crystals(self):
  report=json.loads((P/'material-report.json').read_text());fields=report['relief_fields']
  for name in ('Main receiver cocoon','Deep accelerator trough','Fixed convex standing shin 0','Rotating curved side carapace -1','Receiver continuous carapace field'):
   self.assertTrue(any(e['object']==name and e.get('vascular_field') for e in fields),name)
  shared=next(e for e in fields if e['object']=='Receiver continuous carapace field');self.assertEqual(len(shared['shared_objects']),5)
  self.assertTrue(any(e.get('cap_chart') for e in fields))
  with Image.open(P/'reference-ids.png') as im:ids=np.array(im).astype(int)
  code=ids[:,:,0]+256*ids[:,:,1];mask=np.isin(code,[int(k) for k,v in report['part_roles'].items() if v=='cyan'])
  with Image.open(P/'reference-raw.png') as im:new=np.array(im)
  with Image.open(ART/'pass-38/reference-raw.png') as im:old=np.array(im)
  self.assertTrue(np.array_equal(new[mask],old[mask]))
 def test_original_runtime_and_protected_files_unchanged(self):
  self.assertEqual(hashlib.sha256((ART/'shard_launcher-332414650-0.png').read_bytes()).hexdigest(),'2b07087a6ecf8b2db760d42832deb313fa7476a158897dfea6b0cc25c3c43464')
  for name,sha in json.loads((ART/'integration-pass30/binding.json').read_text())['assets'].items():self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/scrin/buildings'/name).read_bytes()).hexdigest(),sha)
  for name,sha in json.loads((ART/'integration-pass30/protected-before.json').read_text()).items():self.assertEqual(hashlib.sha256((ROOT/name).read_bytes()).hexdigest(),sha)
  for stage in range(30,44):self.assertTrue((ART/f'pass-{stage:02d}/shard-launcher.blend').exists())
if __name__=='__main__':unittest.main()
