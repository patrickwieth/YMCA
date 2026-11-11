"""Ion Defense centered V11 binding: preserve gameplay, validate real PNG loader and map."""
import os,subprocess,tempfile,zipfile,unittest,hashlib
from pathlib import Path
from PIL import Image
from test_soviet_building_art import ROOT,resolved,traits
STUDY=ROOT/'art-source/gdi ion cannon/attempt-11-centering';ASSETS=ROOT/'mods/ca/bits/gdi/buildings/experimental'

class IonDefenseArtTests(unittest.TestCase):
 def test_gameplay_unchanged(self):
  before=traits((ROOT/'art-source/gdi ion cannon/attempt-04/integration/resolved-before.yaml').read_text(encoding='utf-8-sig'));after=traits(resolved('--resolved-rules','Ion_Defense'))
  allowed={'RenderSprites','IsometricSelectable','WithShadow','WithBuildingBib','WithEmbeddedTurretSpriteBody','WithSpriteTurret','WithIdleOverlay@GDIBIB'}
  attachment_fields={'BodyOrientation':'CameraPitch:','Turreted':'Offset:','Armament':'LocalOffset:'}
  def gameplay(data):
   return {k:'\n'.join(line for line in v.splitlines() if not (k in attachment_fields and line.strip().startswith(attachment_fields[k]))).strip() for k,v in data.items() if k not in allowed}
  self.assertEqual(gameplay(before),gameplay(after))
  self.assertIn('CameraPitch: 85',after['BodyOrientation']);self.assertIn('Offset: 0,0,226',after['Turreted']);self.assertIn('LocalOffset: 1631,0,675',after['Armament'])
  for field in ('Priority: 4','Height: 0','Bounds: 1024, 1024'):self.assertIn(field,after['IsometricSelectable'])
  self.assertIn('DecorationBounds: 1901, 1901, 0, 0',after['IsometricSelectable']);self.assertIn('DecorationHeight: 66',after['IsometricSelectable'])
  self.assertIn('Image: ion_defense.native',after['RenderSprites']);self.assertIn('Sequence: base',after['WithSpriteBody']);self.assertIn('Recoils: false',after['WithSpriteTurret'])
  for key in ('WithBuildingBib','WithShadow','WithEmbeddedTurretSpriteBody'):self.assertNotIn(key,after)
  for field in ('Image: gdibib2x2','Sequence: idle','RequiresCondition: !build-incomplete','IsDecoration: true'):self.assertIn(field,after['WithIdleOverlay@GDIBIB'])

 def test_real_engine_frame_loader_and_native_source_copy(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-ion-defense-art'],cwd=ROOT,env=env,check=True)
  for name,count in (('heavy-blaster-base.png',1),('heavy-blaster-turret32.png',32)):
   self.assertEqual((ASSETS/name).read_bytes(),(STUDY/'export'/name).read_bytes())
   with Image.open(ASSETS/name) as im:
    self.assertEqual(im.text['FrameSize'],'160,160');self.assertEqual(im.text['FrameAmount'],str(count))
    if count==1:
     with Image.open(STUDY/'base-centered.png') as original:self.assertEqual(im.convert('RGBA').tobytes(),original.convert('RGBA').tobytes())
    else:
     for i in range(32):
      idx=(20+i)%32;box=((i%8)*160,(i//8)*160,(i%8+1)*160,(i//8+1)*160)
      with Image.open(ROOT/'art-source/gdi ion cannon/attempt-10-preview/160'/f'turret-{idx:03d}.png') as original:self.assertEqual(im.crop(box).convert('RGBA').tobytes(),original.convert('RGBA').tobytes())

 def test_sequences_and_original_icon_muzzle(self):
  old=traits(resolved('--resolved-sequences','ion_defense'));new=traits(resolved('--resolved-sequences','ion_defense.native'))
  for key in ('icon','muzzle','emp-overlay','chrono-overlay'):self.assertEqual(old[key].strip(),new[key].strip())
  for name in ('idle','damaged-idle','turret','damaged-turret','base','damaged-base'):
   self.assertIn('Scale: 1',new[name]);self.assertIn('Anchor: Center',new[name]);self.assertIn('Offset: '+('-1,-10' if name in ('turret','damaged-turret') else '-7,-24'),new[name]);self.assertIn('Start: 0',new[name])
  for name in ('idle','damaged-idle','turret','damaged-turret'):self.assertIn('Facings: 32',new[name]);self.assertIn('UseClassicFacings: False',new[name])

 def test_existing_testmap_slot_uses_actual_actor(self):
  source=ROOT/'mods/ca/maps/testmap1.oramap';before=source.read_bytes()
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/'testmap1-debug.oramap'
   subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'tools/prepare-test-game-map.ps1'),'-SourceMap',str(source),'-DestinationMap',str(target)],check=True)
   with zipfile.ZipFile(target) as z:text=z.read('map.yaml').decode('utf-8-sig')
   self.assertRegex(text,r'Actor84: ion_defense\r?\n\t\tOwner: Multi0\r?\n\t\tLocation: 169,-35')
   env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
   maprules=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--resolved-rules','Ion_Defense',str(target)],cwd=ROOT,env=env,text=True)
   self.assertEqual(resolved('--resolved-rules','Ion_Defense').strip(),maprules.strip())
  self.assertEqual(source.read_bytes(),before)

if __name__=='__main__':unittest.main()
