"""Installed approved art and gameplay-preservation regressions, not live review."""
from pathlib import Path
import re,unittest
ROOT=Path(__file__).resolve().parents[1]
class NodLaserInstallationTests(unittest.TestCase):
 def test_approved_pixels_unchanged(self):
  for source,dest in (('base.png','laser-base.png'),('turret32.png','laser-turret32.png')):
   self.assertEqual((ROOT/'art-source/nod laser turret/pass-10/review'/source).read_bytes(),(ROOT/'mods/ca/bits/nod/buildings'/dest).read_bytes())
 def test_reduced_runtime_scale_and_registration(self):
  from test_soviet_building_art import resolved,traits
  sequences=traits(resolved('--resolved-sequences','ltur.native'))
  for state in ('idle','damaged-idle','base','damaged-base','turret'):
   self.assertIn('Scale: 0.5',sequences[state])
  fields=traits(resolved('--resolved-rules','LTUR'))
  self.assertIn('Offset: 0,0,79',fields['Turreted'])
  for arm in ('Armament','Armament@Marked'):self.assertIn('LocalOffset: 477,0,784',fields[arm])
  self.assertIn('DecorationHeight: 59',fields['IsometricSelectable'])
 def test_gameplay_preserved(self):
  folder=ROOT/'art-source/nod laser turret/integration-pass10'
  def traits(name):
   blocks=re.split(r'(?=^[^\s][^\n]*:\s*$)',(folder/name).read_text(),flags=re.M)
   return {b.splitlines()[0]:b for b in blocks if b.strip()}
  old,new=traits('rules-before.yaml'),traits('rules-after.yaml')
  visual={'RenderSprites:','BodyOrientation:','WithEmbeddedTurretSpriteBody:','WithSpriteBody:','WithSpriteTurret:','IsometricSelectable:'}
  for key in old.keys()|new.keys():
   if key in visual:continue
   a,b=old.get(key,''),new.get(key,'')
   if key=='Turreted:':a=re.sub(r'^\tOffset:.*\n','',a,flags=re.M);b=re.sub(r'^\tOffset:.*\n','',b,flags=re.M)
   if key in ('Armament:','Armament@Marked:'):a=re.sub(r'^\tLocalOffset:.*\n','',a,flags=re.M);b=re.sub(r'^\tLocalOffset:.*\n','',b,flags=re.M)
   self.assertEqual(a,b,key)
if __name__=='__main__':unittest.main()
