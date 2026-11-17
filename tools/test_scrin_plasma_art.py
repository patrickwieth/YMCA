"""Native PTUR preservation, compass, actual pixels, split render occlusion and selection."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,unittest,re,math
from test_soviet_building_art import resolved,traits
from building_presentation_test_helpers import normalize_building_presentation
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/scrin plasma';P=ART/'pass-05';I=ART/'integration-pass05';B=json.loads((I/'binding.json').read_text())
class ScrinPlasmaArtTests(unittest.TestCase):
 def test_original_and_saved_rig(self):
  self.assertEqual(hashlib.sha256((ART/'photon_cannon-1201896784-0.png').read_bytes()).hexdigest(),B['source_sha256'])
  a=json.loads((I/'separation-audit.json').read_text());self.assertTrue(a['fixed_vertices_unchanged']);self.assertTrue(a['all_upper_parts_parented_to_yaw']);self.assertGreater(a['vertical_clearance'],.1)
  self.assertEqual(hashlib.sha256((P/'plasma-cannon.blend').read_bytes()).hexdigest(),a['blend_sha256'])
  for i in range(1,6):self.assertTrue((ART/f'pass-{i:02d}/plasma-cannon.blend').exists())
 def test_native_pixels_and_compass(self):
  runtime=ROOT/'mods/ca/bits/scrin/buildings'
  with Image.open(runtime/'plasma-cannon-base.png') as a,Image.open(P/'review/base.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  with Image.open(runtime/'plasma-cannon-turret32.png') as atlas:
   self.assertEqual(atlas.info['FrameAmount'],'32');self.assertEqual(atlas.info['FrameSize'],'320,320')
   for i,n in enumerate(B['frame_order']):
    im=atlas.crop((i%8*320,i//8*320,(i%8+1)*320,(i//8+1)*320))
    with Image.open(P/f'review/turret-{n:02d}.png') as original:self.assertEqual(im.tobytes(),original.tobytes())
    bbox=im.getbbox();self.assertGreater(min(bbox[:2]),0);self.assertLess(max(bbox[2:]),320)
    t=n*math.pi/16;dx=(-math.sin(t)+math.cos(t))/math.sqrt(2);dy=(-math.sin(t)-math.cos(t))/math.sqrt(8);bearing=math.degrees(math.atan2(-dx,-2*dy))%360;self.assertLess(abs((bearing-i*11.25+180)%360-180),.001)
 def test_split_render_occlusion_and_stationary_base(self):
  with Image.open(P/'base-raw.png') as im:base=im.convert('RGBA')
  with Image.open(P/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
  for n,name in [(16,'reference-raw.png'),(0,'assembly-00-raw.png'),(8,'assembly-08-raw.png'),(24,'assembly-24-raw.png')]:
   with Image.open(P/f'turret-{n:02d}-raw.png') as turret,Image.open(P/name) as full:self.assertEqual(Image.alpha_composite(base,turret.convert('RGBA')).tobytes(),full.convert('RGBA').tobytes())
 def test_selection_covers_all_parts(self):
  half=B['decoration_bounds']*128/1448/2;bottom=B['decoration_bounds']*64/1448/2
  for name,offset,mount in [('base',-72,0)]+[(f'turret-{n:02d}',-20,681) for n in range(32)]:
   with Image.open(P/'review'/(name+'.png')) as im:a=np.array(im)
   y,x=np.where(a[:,:,3]>0);x=(x+.5-160)*.58;y=(y+.5-160+offset)*.58-mount*64/1448
   self.assertTrue(np.all(abs(x)<=half));self.assertTrue(np.all(y<=bottom-.5*abs(x)));self.assertTrue(np.all(y>=-bottom-B['selection_height']+.5*abs(x)))
 def test_gameplay_and_weapon_preserved(self):
  old=traits((I/'rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','PTUR'));self.assertIn('Image: ptur.native',new['RenderSprites']);self.assertIn('Dimensions: 1,1',new['Building']);self.assertNotIn('WithBuildingBib',new)
  self.assertIn('LocalOffset: 362,-22,726, 362,22,726',new['Armament'])
  for data in (old,new):
   normalize_building_presentation(data)
   for key in ('RenderSprites','BodyOrientation','WithBuildingBib','WithSpriteBody','WithSpriteTurret','IsometricSelectable','WithIdleOverlay@SCRINBIB'):data.pop(key,None)
   data['Turreted']=re.sub(r'\n\tOffset: [^\n]+','',data['Turreted']);data['Armament']=re.sub(r'\n\tLocalOffset: [^\n]+','',data['Armament'])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  self.assertEqual((I/'weapon-before.yaml').read_text().strip(),resolved('--resolved-weapons','PlasmaTurretGun').strip())
 def test_resolved_assets_and_effects(self):
  old=traits(resolved('--resolved-sequences','ptur'));new=traits(resolved('--resolved-sequences','ptur.native'))
  for name in ('idle','damaged-idle','base','damaged-base'):self.assertIn('Filename: bits/scrin/buildings/plasma-cannon-base.png',new[name])
  for name in ('turret','damaged-turret'):self.assertIn('Filename: bits/scrin/buildings/plasma-cannon-turret32.png',new[name]);self.assertIn('Facings: 32',new[name])
  for name in ('construction','icon','muzzle'):self.assertEqual(old[name].strip(),new[name].strip())
if __name__=='__main__':unittest.main()
