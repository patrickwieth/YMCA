"""Native supplied GDI Guard Tower binding and unchanged gameplay baseline."""
import os,subprocess,hashlib,json,unittest,zipfile
from PIL import Image
from test_soviet_building_art import ROOT,resolved,traits
STUDY=ROOT/'art-source/gdi-guard-tower'

class GuardTowerArtTests(unittest.TestCase):
 def test_gameplay_preserved_except_muzzle_attachment(self):
  before=traits((STUDY/'resolved-before.yaml').read_text(encoding='utf-8-sig'));after=traits(resolved('--resolved-rules','GTWR'))
  allowed={'RenderSprites','WithMakeAnimation','WithBuildingBib','IsometricSelectable','WithIdleOverlay@GDIBIB'}
  def comparable(data):return {k:'\n'.join(line for line in v.splitlines() if not (k=='Turreted' and line.strip().startswith('Offset:'))).strip() for k,v in data.items() if k not in allowed}
  self.assertEqual(comparable(before),comparable(after));self.assertIn('Offset: 0,0,2007',after['Turreted'])
  for field in ('Priority: 4','Height: 0','Bounds: 1024, 1024'):self.assertIn(field,after['IsometricSelectable'])
  for field in ('Image: gdibib1x1','Sequence: idle','RequiresCondition: !build-incomplete','IsDecoration: true'):self.assertIn(field,after['WithIdleOverlay@GDIBIB'])
  self.assertIn('Image: gtwr.gdi.png',after['RenderSprites']);self.assertNotIn('WithMakeAnimation',after);self.assertNotIn('WithBuildingBib',after)

 def test_original_png_and_real_loader(self):
  source=ROOT/'mods/ca/bits/gdi/buildings/guard tower.png'
  self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),'550479cc38b06c5e64cc1e9e8fbc1346b3cb81019ac5f6904db729a6489e7d76')
  with Image.open(source) as im:self.assertEqual(im.size,(128,150))
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-guard-tower-art'],cwd=ROOT,env=env,check=True)

 def test_sequences_and_existing_map_actor(self):
  old=traits(resolved('--resolved-sequences','gtwr'));new=traits(resolved('--resolved-sequences','gtwr.gdi.png'))
  for key in ('icon','muzzle'):self.assertEqual(old[key].strip(),new[key].strip())
  for key in ('idle','damaged-idle','dead'):
   for field in ('Filename: bits/gdi/buildings/color-matched/guard tower.png','Start: 0','Scale: 1','Anchor: Center','Offset: -1,-56'):self.assertIn(field,new[key])
  with zipfile.ZipFile(ROOT/'mods/ca/maps/testmap1.oramap') as z:text=z.read('map.yaml').decode('utf-8-sig')
  self.assertRegex(text,r'Actor75: gtwr\r?\n\t\tOwner: Multi0\r?\n\t\tLocation: 141,-15')

if __name__=='__main__':unittest.main()
