"""Nod factory split, retired airstrip and unaffected GDI regressions."""
from pathlib import Path
import os,subprocess,unittest
from test_soviet_building_art import traits,resolved
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/nod-war-factory'
class NodWarFactoryTests(unittest.TestCase):
 def test_all_nod_factions_and_no_gdi_menu_leak(self):
  f=traits(resolved('--resolved-rules','WEAP.NOD'));con=traits(resolved('--resolved-rules','AFAC.Nod'))
  self.assertIn('Prerequisites: proc.td, ~structures.nod',f['Buildable'])
  for faction in ('nod','blackh','marked','legion'):
   self.assertIn(faction,con['ProvidesPrerequisite@nod']);self.assertNotIn(faction,con['ProvidesPrerequisite@weap.tdFAC'])
  self.assertIn('Prerequisite: weap.td',f['ProvidesPrerequisite@sharedfactory'])
  self.assertIn('Produces: Vehicle, Paradrop',f['Production']);self.assertNotIn('ProductionAirdropCA',f)
 def test_preserve_legion_factory_mechanics_and_gdi(self):
  before=(ART/'gdi-before.yaml').read_text();self.assertEqual(resolved('--resolved-rules','WEAP.TD'),before)
  original=traits(before);nod=traits(resolved('--resolved-rules','WEAP.NOD'))
  for key in ('Health','Valued','Power','Building','HitShape','Exit@1','Production','GeneratedModeProductionQueue','RallyPoint','PrimaryBuilding','ProductionBar'):
   self.assertEqual(original.get(key),nod.get(key),key)
  self.assertNotIn('WithIdleOverlay@GDIBIB',nod);self.assertIn('Image: nodbib3x3',nod['WithIdleOverlay@NODBIB'])
  self.assertNotIn('WithProductionDoorOverlayCA',nod)
 def test_png_binding_and_legacy_airstrip(self):
  nod=traits(resolved('--resolved-sequences','weap.nod.png'))
  for state in ('idle','damaged-idle','dead','place'):
   self.assertIn('Filename: bits/nod/buildings/warfactory.png',nod[state]);self.assertIn('Offset: 0,0',nod[state])
  old=traits(resolved('--resolved-rules','AIRS'))
  self.assertIn('~disabled',old['Buildable']);self.assertIn('Image: astrip',old['RenderSprites']);self.assertNotIn('WithIdleOverlay@NODBIB',old)
  seq=traits(resolved('--resolved-sequences','astrip'));self.assertIn('Filename: astrip.shp',seq['idle']);self.assertIn('Length: 16',seq['active'])
 def test_bot_factory_lists_and_priorities(self):
  for name in ('easy','normal','hard','naval','brutal'):
   text=(ROOT/f'mods/ca/rules/ai/{name}.yaml').read_text()
   for line in text.splitlines():
    if 'VehiclesFactoryTypes:' in line or 'ProductionTypes:' in line and 'NavalProductionTypes:' not in line:self.assertIn('weap.nod',line)
   if name!='brutal':self.assertIn('weap.nod:',text);self.assertNotIn('\tairs:',text)
  text=(ROOT/'mods/ca/rules/nod/ai.yaml').read_text();self.assertIn('weap.nod: 20',text);self.assertIn('weap.nod: 5',text)
 def test_engine_sprites_and_dependencies(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  output=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-nod-war-factory'],cwd=ROOT,env=env,text=True)
  self.assertIn('ground production',output)
if __name__=='__main__':unittest.main()
