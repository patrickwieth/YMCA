"""Approved AA pass11 integration; art registration is not a live firing test."""
import os,re,subprocess,unittest
from pathlib import Path
from PIL import Image
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/gdi aa gun';RUNTIME=ROOT/'mods/ca/bits/gdi/buildings'
class GdiAaArtTests(unittest.TestCase):
 def test_approved_pixels_and_direct_resolved_bindings(self):
  seq=traits(resolved('--resolved-sequences','cram.native'))
  for stem,count in (('base',1),('turret32',32)):
   with Image.open(ART/'pass-11/review'/(stem+'.png')) as approved,Image.open(RUNTIME/('aa-gun-'+stem+'.png')) as actual:
    self.assertEqual(approved.convert('RGBA').tobytes(),actual.convert('RGBA').tobytes());self.assertEqual(actual.text['FrameSize'],'192,192');self.assertEqual(actual.text['FrameAmount'],str(count))
  for state in ('base','damaged-base','idle','damaged-idle'):
   self.assertIn('Filename: bits/gdi/buildings/aa-gun-base.png',seq[state]);self.assertIn('Offset: 0,-40',seq[state])
  for state in ('turret','damaged-turret'):
   self.assertIn('Filename: bits/gdi/buildings/aa-gun-turret32.png',seq[state]);self.assertIn('Facings: 32',seq[state]);self.assertIn('UseClassicFacings: False',seq[state]);self.assertIn('Offset: 0,-2',seq[state])
 def test_gameplay_preserved_across_approved_aa_and_allied_art(self):
  def gameplay(text):
   data=traits(text)
   # Global construction replacement is visual; retain its condition/trait.
   if 'WithMakeAnimation' in data:data['WithMakeAnimation']=re.sub(r'\n[\t ]+Sequence: construction[\t ]*(?=\n|$)','',data['WithMakeAnimation'])
   for k in list(data):
    if k.startswith(('RenderSprites','WithBuildingBib','WithIdleOverlay','WithSpriteBody','WithSpriteTurret','WithEmbeddedTurretSpriteBody')):del data[k]
   for key,fields in [('IsometricSelectable',('DecorationBounds','DecorationHeight')),('Turreted',('Offset',)),('BodyOrientation',('CameraPitch','UseClassicPerspectiveFudge'))]+[(key,('LocalOffset',)) for key in data if key.startswith('Armament')]:
    for field in fields:data[key]=re.sub(r'(?m)^[\t ]+'+field+r':[^\n]*\n?','',data[key])
   return {k:re.sub(r'\s+','',v) for k,v in data.items()}
  before=(ART/'integration-pass11/before/CRAM.yaml').read_text(encoding='utf-8-sig');after=resolved('--resolved-rules','CRAM')
  self.assertEqual(gameplay(before),gameplay(after))
  # Allied pass15 was subsequently approved: compare gameplay, not its old sprite binding.
  allied=(ART/'integration-pass11/before/AGUN.yaml').read_text(encoding='utf-8-sig');self.assertEqual(gameplay(allied),gameplay(resolved('--resolved-rules','AGUN')))
  fields=traits(after);self.assertNotIn('WithEmbeddedTurretSpriteBody',fields);self.assertIn('Sequence: base',fields['WithSpriteBody']);self.assertIn('Image: gdibib1x1',fields['WithIdleOverlay@GDIBIB'])
 def test_engine_native_loader_traits_and_muzzles(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  output=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-gdi-aa-art'],env=env,cwd=ROOT,text=True)
  self.assertIn('native 1+32 frames',output)
if __name__=='__main__':unittest.main()
