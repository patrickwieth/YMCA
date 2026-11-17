"""Fixed EMP integration regressions; no claim of live GPU/flight verification."""
from pathlib import Path
import hashlib,json,subprocess,os,unittest
import numpy as np
from PIL import Image
from test_soviet_building_art import traits,resolved
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/gdi-emp-launcher'
class EmpLauncherTests(unittest.TestCase):
 def test_supplied_art_and_extracted_missile(self):
  report=json.loads((ART/'calibration.json').read_text())
  for name,key in (('emp launcher.png','loaded_sha256'),('emp launcher empty.png','empty_sha256')):
   p=ROOT/'mods/ca/bits/gdi/buildings'/name;self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),report[key])
  with Image.open(ART/'extracted-missile.png') as im:
   self.assertEqual(im.getbbox(),(50,13,124,59));extracted=np.array(im)
  with Image.open(ROOT/'mods/ca/bits/gdi/buildings/emp launcher.png') as im:original=np.array(im)
  mask=extracted[:,:,3]>0;self.assertTrue(np.array_equal(original[mask],extracted[mask]))
  with Image.open(ROOT/'mods/ca/bits/gdi/buildings/emp-missile32.png') as im:
   self.assertEqual(im.text['FrameSize'],'128,128');self.assertEqual(im.text['FrameAmount'],'32')
   for i in range(32):
    box=im.crop((i%8*128,i//8*128,(i%8+1)*128,(i//8+1)*128)).getbbox()
    self.assertGreater(min(box[:2]),0);self.assertLess(max(box[2:]),128)
 def test_fixed_launcher_and_readiness_reload(self):
  f=traits(resolved('--resolved-rules','PATR'))
  self.assertNotIn('Turreted',f);self.assertNotIn('WithEmbeddedTurretSpriteBody',f);self.assertIn('AttackOmni',f)
  self.assertIn('ReloadAmmoOnSupportPowerCharged',f);self.assertNotIn('ReloadAmmoPool',f)
  self.assertIn('InitialAmmo: 0',f['AmmoPool']);self.assertIn('AmmoCondition: loaded-rocket',f['AmmoPool'])
  self.assertIn('Sequence: empty',f['WithSpriteBody']);self.assertIn('RequiresCondition: loaded-rocket && !build-incomplete',f['WithIdleAnimation'])
  self.assertIn('LocalYaw: 256',f['Armament']);self.assertIn('LocalOffset: 0,-931,3249',f['Armament'])
  for state,file in (('idle','emp launcher.png'),('damaged-idle','emp launcher.png'),('empty','emp launcher empty.png'),('damaged-empty','emp launcher empty.png')):
   seq=traits(resolved('--resolved-sequences','patr.gdi.png'))[state]
   self.assertIn('Filename: bits/gdi/buildings/'+file,seq);self.assertIn('Facings: 1',seq);self.assertIn('Scale: 1',seq)
 def test_emp_effect_stats_and_cooldown_preserved(self):
  old=traits((ART/'rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','PATR'))
  for key in ('Buildable','Valued','Health','Building','Power','AttackOrderPower@EMPMISSILE','RevealsShroud','DetectCloaked'):
   self.assertEqual(old.get(key),new.get(key),key)
  before=traits((ART/'weapon-before.yaml').read_text());after=traits(resolved('--resolved-weapons','THLauncher.Fixed'))
  for key in before:
   if key!='Projectile':self.assertEqual(before[key],after[key],key)
  self.assertIn('MissileCA',after['Projectile']);self.assertIn('HomingActivationDelay: 6',after['Projectile']);self.assertIn('InitializeFacingFromVelocity: true',after['Projectile'])
 def test_larger_bib_is_only_decoration_and_legacy_missile_is_selected(self):
  f=traits(resolved('--resolved-rules','PATR'))
  self.assertIn('Dimensions: 2,2',f['Building']);self.assertIn('Footprint: xx xx',f['Building'])
  self.assertIn('Image: gdibib3x3.emp',f['WithIdleOverlay@GDIBIB']);self.assertNotIn('WithBuildingBib',f)
  seq=traits(resolved('--resolved-sequences','gdibib3x3.emp'))['idle']
  self.assertIn('Filename: bits/gdi/buildings/bib3x3.png',seq);self.assertIn('Offset: 0,32',seq)
  p=traits(resolved('--resolved-weapons','THLauncher.Fixed'))['Projectile']
  self.assertIn('Image: thawk',p);self.assertIn('IsPlayerPalette: true',p);self.assertNotIn('emp-launch-missile',p)
 def test_engine_load_and_launch_projection(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  output=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-emp-launcher'],cwd=ROOT,env=env,text=True)
  self.assertIn('fixed loaded/empty launcher',output);self.assertIn('launch alignment',output)
if __name__=='__main__':unittest.main()
