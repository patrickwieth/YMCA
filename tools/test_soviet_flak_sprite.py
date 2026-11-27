"""Rendered Soviet flak replacement: assets, engine-resolved traits and test maps."""
from pathlib import Path
from zipfile import ZipFile
import unittest
from PIL import Image
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1]
class SovietFlakSpriteTests(unittest.TestCase):
 def test_original_sprite_export_installed(self):
  for source,dest,count in (('base.png','flak-base.png',1),('turret32.png','flak-turret32.png',32)):
   p=ROOT/'mods/ca/bits/soviet/buildings'/dest
   self.assertEqual(p.read_bytes(),(ROOT/'art-source/soviet flak/pass-15/review'/source).read_bytes())
   with Image.open(p) as im:self.assertEqual(im.text['FrameSize'],'192,192');self.assertEqual(im.text['FrameAmount'],str(count))
 def test_matching_pedestal_fit_and_original_asset_preserved(self):
  import hashlib,json
  p=ROOT/'mods/ca/bits/soviet/buildings/flak base.png'
  self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),'d61954e39d64f49941a797f0173010a8f430321ad186194b0113cc5d36bc4c72')
  geometry=json.loads((ROOT/'art-source/soviet flak/pass-15/geometry.json').read_text())
  self.assertLessEqual(geometry['ground_diamond_max'],64)
  seq=traits(resolved('--resolved-sequences','flak.soviet.sprite'))
  for state in ('idle','damaged-idle','base','damaged-base'):
   self.assertIn('Filename: bits/soviet/buildings/flak-base.png',seq[state]);self.assertIn('Scale: 1',seq[state]);self.assertIn('Offset: 0,-40',seq[state])
 def test_actor_is_sprite_only(self):
  fields=traits(resolved('--resolved-rules','FLAK.SOVIET.SPRITE'))
  self.assertNotIn('RenderVoxels',fields);self.assertFalse(any(k.startswith('WithVoxel') for k in fields))
  self.assertIn('WithSpriteBody',fields);self.assertIn('WithSpriteTurret',fields)
  self.assertIn('Dimensions: 1,1',fields['Building'])
  self.assertIn('Image: sovietbib1x1',fields['WithIdleOverlay@SOVIETBIB'])
  self.assertIn('Offset: 0,0,243',fields['Turreted'])
  self.assertIn('UseClassicPerspectiveFudge: false',fields['BodyOrientation'])
  self.assertIn('LocalOffset: 669,0,1603',fields['Armament'])
  self.assertIn('Weapon: SOVIET-CLUSTER-AA',fields['Armament']);self.assertIn('Recoil: 0',fields['Armament'])
  self.assertIn('Sequence: construction',fields['WithMakeAnimation'])
  self.assertIn('WithMuzzleOverlay',fields);self.assertIn('MuzzleSequence: muzzle-cluster',fields['Armament'])
  sam=traits(resolved('--resolved-rules','SAM'));self.assertIn('Image: sam.soviet',sam['RenderSprites']);self.assertIn('Weapon: Nike',sam['Armament'])
 def test_dedicated_map_uses_sprite_replacement(self):
  with ZipFile(ROOT/'mods/ca/development-maps/soviet-flak-voxel-test.oramap') as z:text=z.read('map.yaml').decode('utf-8-sig')
  self.assertIn('Title: Flak Sprite Test (local art)',text);self.assertIn('Actor37: flak.soviet.sprite',text)
  self.assertNotIn('flak.soviet.voxel',text.lower())
  self.assertIn('Weapon: SOVIET-CLUSTER-AA.Test',text)
if __name__=='__main__':unittest.main()
