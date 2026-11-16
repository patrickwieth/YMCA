"""Approved SAM art is actually bound; gameplay scope and launch-slot regressions."""
from pathlib import Path
import os,subprocess,unittest
from test_soviet_building_art import traits,resolved,normalize_soviet_sam_visuals
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/nod sam site';BITS=ROOT/'mods/ca/bits/nod/buildings'
class NodSamInstallationTests(unittest.TestCase):
 def test_frozen_pixels_installed(self):
  self.assertEqual((BITS/'sam-base.png').read_bytes(),(ART/'ammo-variants/base.png').read_bytes())
  for n in range(5):self.assertEqual((BITS/f'sam-turret-{n}.png').read_bytes(),(ART/f'ammo-variants/turret-{n}.png').read_bytes())
 def test_actor_and_existing_gameplay(self):
  actor=traits(resolved('--resolved-rules','NSAM'));before=traits((ART/'integration/rules-before.yaml').read_text())
  before['Building']=before['Building'].replace('Footprint: xx','Footprint: x').replace('Dimensions: 2,1','Dimensions: 1,1')
  before['HitShape']=before['HitShape'].replace('-768,-512','-512,-512').replace('768,512','512,512')
  for key in ('Buildable','Health','Valued','Power','Building','HitShape','DetectCloaked','RevealsShroud'):
   self.assertEqual(actor[key],before[key],key)
  self.assertIn('Image: nsam.native',actor['RenderSprites']);self.assertNotIn('AttackPopupTurreted',actor);self.assertNotIn('WithEmbeddedTurretSpriteBody',actor)
  self.assertIn('Weapon: Nike.Nod',actor['Armament']);self.assertIn('ResetOnFire: true',actor['ReloadAmmoPool'])
  # Soviet SAM later received its separately authorized pass12 art; keep the
  # original baseline and normalize only that explicit visual registration.
  self.assertEqual(traits(normalize_soviet_sam_visuals(resolved('--resolved-rules','SAM'))),traits((ART/'integration/soviet-stock-before.yaml').read_text()))
 def test_nod_bib(self):
  actor=traits(resolved('--resolved-rules','NSAM'))
  self.assertIn('Image: nodbib1x1',actor['WithIdleOverlay@NODBIB'])
  self.assertIn('RequiresCondition: !build-incomplete',actor['WithIdleOverlay@NODBIB'])
  self.assertIn('IsDecoration: true',actor['WithIdleOverlay@NODBIB'])
  seq=traits(resolved('--resolved-sequences','nodbib1x1'))
  self.assertIn('Filename: bits/nod/buildings/bib1x1.png',seq['idle'])
  self.assertIn('ZOffset: -4096',seq['idle'])
 def test_60pct_all_visual_states_and_one_cell(self):
  seq=traits(resolved('--resolved-sequences','nsam.native'))
  for name in ('idle','damaged-idle','base','damaged-base','dead','place',*(f'turret-{i}' for i in range(5))):
   self.assertIn('Scale: 0.6',seq[name],name)
  actor=traits(resolved('--resolved-rules','NSAM'))
  self.assertIn('Dimensions: 1,1',actor['Building']);self.assertIn('Footprint: x\n',actor['Building'])
  self.assertNotIn('Footprint: xx',actor['Building'])
  self.assertIn('TopLeft: -512,-512',actor['HitShape']);self.assertIn('BottomRight: 512,512',actor['HitShape'])
 def test_body_raised_on_bib(self):
  from PIL import Image
  seq=traits(resolved('--resolved-sequences','nsam.native'))
  for name in ('idle','damaged-idle','base','damaged-base','dead','place'):
   self.assertIn('Offset: -5,-32',seq[name])
  with Image.open(BITS/'sam-base.png') as im:front=(im.getbbox()[3]-128-32)*.6
  with Image.open(BITS/'bib1x1.png') as im:bibfront=im.width/4
  self.assertLessEqual(abs(front-(bibfront-12)),1.5)
  actor=traits(resolved('--resolved-rules','NSAM'))
  self.assertIn('Offset: 0,-88,611',actor['Turreted'])
  self.assertIn('DecorationBounds: 950, 977, 0, -55',actor['IsometricSelectable'])
 def test_engine_sequences_conditions_and_muzzles(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  output=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-nod-sam-art'],cwd=ROOT,env=env,text=True)
  self.assertIn('128 muzzle projections',output)
if __name__=='__main__':unittest.main()
