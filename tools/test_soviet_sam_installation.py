"""Soviet SAM pass12 pixel identity, isolated gameplay and engine-resolved bindings."""
from pathlib import Path
import hashlib,json,unittest
from PIL import Image
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1]
S=ROOT/'art-source/soviet sam site'
D=ROOT/'mods/ca/bits/soviet/buildings'
class SovietSamInstallationTests(unittest.TestCase):
 def test_source_and_twelve_archived_passes(self):
  source=hashlib.sha256((S/'sam_site-887352960-0.png').read_bytes()).hexdigest()
  for p in range(1,13):
   stage=S/f'pass-{p:02d}'
   self.assertTrue((stage/'soviet-sam.blend').exists())
   self.assertEqual(json.loads((stage/'geometry.json').read_text())['source_sha256'],source)
  g=json.loads((S/'pass-12/geometry.json').read_text())
  self.assertEqual(g['facings'],list(range(32)));self.assertEqual(g['yaw_start_degrees'],0)
 def test_native_pixel_identity_and_direction_order(self):
  self.assertEqual((D/'sam-base.png').read_bytes(),(S/'pass-12/review/base.png').read_bytes())
  with Image.open(D/'sam-turret32.png') as sheet:
   self.assertEqual(sheet.text['FrameSize'],'192,192');self.assertEqual(sheet.text['FrameAmount'],'32')
   for i in range(32):
    frame=sheet.crop((i%8*192,i//8*192,i%8*192+192,i//8*192+192))
    with Image.open(S/f'pass-12/review/turret-{(20+i)%32:03d}.png') as src:self.assertEqual(frame.tobytes(),src.tobytes())
 def test_gameplay_preserved_except_visual_registration(self):
  before=traits((S/'integration-pass12/SAM-before.yaml').read_text());after=traits(resolved('--resolved-rules','SAM'))
  before['Buildable']=before['Buildable'].replace('Prerequisites: dome, ~structures.soviet','Prerequisites: dome, ~structures.soviet, ~!structures.nkorea')
  before['IsometricSelectable']=before['IsometricSelectable'].replace('DecorationHeight: 30','DecorationHeight: 99')
  before['HitShape']=before['HitShape'].replace('-768,-512','-512,-512').replace('768,512','512,512')
  before['WithIdleOverlay@SOVIETBIB']=before['WithIdleOverlay@SOVIETBIB'].replace('sovietbib2x2','sovietbib1x1')
  self.assertIn('Dimensions: 1,1',after['Building']);self.assertIn('Footprint: x',after['Building'])
  visual={'RenderSprites','WithSpriteBody','WithEmbeddedTurretSpriteBody','WithSpriteTurret','BodyOrientation','Turreted','Armament'}
  self.assertEqual({k:v for k,v in before.items() if k not in visual},{k:v for k,v in after.items() if k not in visual})
  self.assertEqual(before['Turreted'].strip(),after['Turreted'].replace('\n\tOffset: 0,0,726','').strip())
  self.assertEqual(before['Armament'].replace('0,0,320','496,-291,953').replace('MuzzleSequence: muzzle','MuzzleSequence: muzzle-native'),after['Armament'])
  self.assertIn('QuantizedFacings: 32',after['BodyOrientation']);self.assertIn('CameraPitch: 85',after['BodyOrientation'])
  self.assertIn('RequiresCondition: !build-incomplete',after['WithSpriteTurret'])
 def test_unrelated_inherited_actors_unchanged(self):
  for name in ('FLAK.SOVIET.VOXEL','FLAK.SOVIET.SPRITE','NSAM'):
   before=traits((S/f'integration-pass12/{name}-before.yaml').read_text());after=traits(resolved('--resolved-rules',name))
   if name=='NSAM':
    before['Turreted']=before['Turreted'].replace('0,-317,1493','0,-88,611')
    before['Armament']=before['Armament'].replace('371,200,1522, 1250,-200,1522, 627,-312,1522, 994,312,1522','112,60,913, 375,-60,913, 188,-94,913, 298,94,913')
    self.assertIn('UseClassicPerspectiveFudge: false',after['BodyOrientation'])
    after['BodyOrientation']=after['BodyOrientation'].replace('\n\tUseClassicPerspectiveFudge: false','')
    before['Building']=before['Building'].replace('Footprint: xx','Footprint: x').replace('Dimensions: 2,1','Dimensions: 1,1')
    before['HitShape']=before['HitShape'].replace('-768,-512','-512,-512').replace('768,512','512,512')
    before['IsometricSelectable']=before['IsometricSelectable'].replace('Height: 0','Height: 60').replace('DecorationHeight: 160','DecorationHeight: 60').replace('Bounds: 1024, 1024','Bounds: 950, 977, 0, -55').replace('DecorationBounds: 2896, 1448, 0, 0','DecorationBounds: 950, 977, 0, -55')
    bib=after.pop('WithIdleOverlay@NODBIB')
    self.assertIn('Image: nodbib1x1',bib)
    self.assertIn('RequiresCondition: !build-incomplete',bib)
   if name=='FLAK.SOVIET.SPRITE':
    before['BodyOrientation']=before['BodyOrientation'].replace('UseClassicPerspectiveFudge: true','UseClassicPerspectiveFudge: false')
    before['Armament']=before['Armament'].replace('1337,0,1603','669,0,1603')
    build=after.pop('Buildable')
    self.assertIn('Prerequisites: dome, ~structures.nkorea',build)
    self.assertIn('Queue: Defense',build)
   self.assertEqual(before,after,name)
 def test_faction_gates_and_ai(self):
  sam=traits(resolved('--resolved-rules','SAM'));flak=traits(resolved('--resolved-rules','FLAK.SOVIET.SPRITE'))
  self.assertIn('Prerequisites: dome, ~structures.soviet, ~!structures.nkorea',sam['Buildable'])
  self.assertIn('Prerequisites: dome, ~structures.nkorea',flak['Buildable'])
  self.assertNotIn('Buildable',traits(resolved('--resolved-rules','FLAK.SOVIET.VOXEL')))
  for name in ('easy','normal','hard','naval','brutal'):
   text=(ROOT/f'mods/ca/rules/ai/{name}.yaml').read_text()
   self.assertIn('AntiAirTypes: sam, flak.soviet.sprite,',text)
   self.assertIn('StaticAntiAirTypes: agun, sam, flak.soviet.sprite,',text)
   if name!='brutal':self.assertIn('\n\t\t\tflak.soviet.sprite:',text)
 def test_engine_resolved_sprite_paths(self):
  seq=traits(resolved('--resolved-sequences','sam.soviet.native'))
  for name in ('base','idle','damaged-base','damaged-idle','dead'):
   self.assertIn('Filename: bits/soviet/buildings/sam-base.png',seq[name]);self.assertIn('Offset: 0,-50',seq[name])
  for name in ('turret','damaged-turret'):
   self.assertIn('Filename: bits/soviet/buildings/sam-turret32.png',seq[name]);self.assertIn('Facings: 32',seq[name]);self.assertIn('Offset: 0,-7',seq[name])
  for name in ('base','idle','damaged-base','damaged-idle','dead','place','turret','damaged-turret'):
   self.assertIn('Scale: 0.75',seq[name])
  self.assertIn('Length: 10',seq['construction'])
if __name__=='__main__':unittest.main()
