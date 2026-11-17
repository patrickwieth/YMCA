"""Approved Sonic art integration; snapshots guard gameplay, not GPU appearance."""
from pathlib import Path
import re,unittest
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
class SonicInstallationTests(unittest.TestCase):
 def test_approved_native_pixels(self):
  for source,dest,count in (('base.png','sonic-base.png',1),('turret32.png','sonic-turret32.png',32)):
   path=ROOT/'mods/ca/bits/gdi/buildings'/dest
   self.assertEqual((ROOT/'art-source/gdi sonic turret/pass-22/review'/source).read_bytes(),path.read_bytes())
   with Image.open(path) as im:self.assertEqual(im.text['FrameSize'],'224,224');self.assertEqual(im.text['FrameAmount'],str(count))
 def test_reduced_scale_and_registration(self):
  from test_soviet_building_art import resolved,traits
  seq=traits(resolved('--resolved-sequences','stwr.native'))
  for state in ('idle','damaged-idle','base','damaged-base','dead','turret','damaged-turret'):self.assertIn('Scale: 0.65',seq[state])
  data=traits(resolved('--resolved-rules','STWR'))
  self.assertIn('Offset: 0,0,599',data['Turreted']);self.assertIn('LocalOffset: 957,0,677',data['Armament'])
 def test_gameplay_preserved(self):
  folder=ROOT/'art-source/gdi sonic turret/integration-pass22'
  def traits(name):
   blocks=re.split(r'(?=^[^\s][^\n]*:\s*$)',(folder/name).read_text(),flags=re.M)
   return {b.splitlines()[0]:b for b in blocks if b.strip()}
  old,new=traits('rules-before.yaml'),traits('rules-after.yaml')
  visual={'RenderSprites:','BodyOrientation:','WithEmbeddedTurretSpriteBody:','WithSpriteBody:','WithSpriteTurret:','IsometricSelectable:','WithBuildingBib:','WithIdleOverlay@GDIBIB:'}
  for key in old.keys()|new.keys():
   if key in visual:continue
   a,b=old.get(key,''),new.get(key,'')
   field='Offset' if key=='Turreted:' else 'LocalOffset' if key=='Armament:' else None
   if field:
    a=re.sub(r'^\t'+field+r':.*\n','',a,flags=re.M);b=re.sub(r'^\t'+field+r':.*\n','',b,flags=re.M)
   self.assertEqual(a,b,key)
  self.assertIn('~disabled',new['Buildable:'])
  self.assertIn('RequiresCondition: !build-incomplete',new['WithSpriteTurret:'])
 def test_direct_binding_and_stationary_cables(self):
  text=(ROOT/'mods/ca/sequences/sonic-building-art.yaml').read_text()
  self.assertIn('Filename: bits/gdi/buildings/sonic-base.png',text)
  self.assertIn('Filename: bits/gdi/buildings/sonic-turret32.png',text)
  self.assertIn('Offset: 0,-52',text);self.assertIn('Offset: 0,-11',text)
  self.assertIn('ca|sequences/sonic-building-art.yaml',(ROOT/'mods/ca/mod.yaml').read_text())
  import json
  g=json.loads((ROOT/'art-source/gdi sonic turret/pass-22/geometry.json').read_text())
  cables=[o for o in g['objects'] if o['name'].startswith('SONIC fixed supply loop')]
  self.assertEqual(len(cables),2);self.assertTrue(all(o['stationary'] for o in cables))
if __name__=='__main__':unittest.main()
