"""Native Chinese twin Gatling: source preservation, shared stages, projection and fixed bib."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,unittest,re,math
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/chinese gatling';P=ART/'pass-03';I=ART/'integration';B=json.loads((I/'binding.json').read_text())
class ChineseGatlingArtTests(unittest.TestCase):
 def test_sources_and_actual_saved_rig(self):
  for name,sha in B['source_hashes'].items():self.assertEqual(hashlib.sha256((ART/name).read_bytes()).hexdigest(),sha)
  a=json.loads((I/'separation-audit.json').read_text());self.assertTrue(a['fixed_vertices_unchanged']);self.assertTrue(a['all_gun_parts_parented_to_yaw']);self.assertGreater(a['vertical_clearance'],.1);self.assertEqual(a['barrel_tubes'],12)
  self.assertEqual(hashlib.sha256((P/'chinese-gatling.blend').read_bytes()).hexdigest(),a['blend_sha256'])
  for i in range(1,4):self.assertTrue((ART/f'pass-{i:02d}/chinese-gatling.blend').exists())
 def test_native_pixels_and_compass(self):
  runtime=ROOT/'mods/ca/bits/china/buildings'
  with Image.open(runtime/'gatling-base.png') as a,Image.open(ART/B['platform_review']/('supplied-ring.png' if B.get('user_supplied_ring') else 'platform.png')) as b:self.assertEqual(a.tobytes(),b.tobytes())
  with Image.open(runtime/'gatling-turret32.png') as atlas:
   self.assertEqual(atlas.info['FrameAmount'],'32');self.assertEqual(atlas.info['FrameSize'],'320,320')
   for i,n in enumerate(B['frame_order']):
    im=atlas.crop((i%8*320,i//8*320,(i%8+1)*320,(i//8+1)*320))
    with Image.open(P/f'review/turret-{n:02d}.png') as original:self.assertEqual(im.tobytes(),original.tobytes())
    bbox=im.getbbox();self.assertGreater(min(bbox[:2]),0);self.assertLess(max(bbox[2:]),320)
    t=n*math.pi/16;dx=(-math.sin(t)+math.cos(t))/math.sqrt(2);dy=(-math.sin(t)-math.cos(t))/math.sqrt(8);bearing=math.degrees(math.atan2(-dx,-2*dy))%360;self.assertLess(abs((bearing-i*11.25+180)%360-180),.001)
 def test_supplied_platform_native_cut_and_unchanged_mount(self):
  source=ART/B['platform_source'];self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),B['platform_source_sha256'])
  with Image.open(source) as im:a=np.array(im.convert('RGBA'));colors={tuple(p) for p in a.reshape(-1,4)}
  with Image.open(ROOT/'mods/ca/bits/china/buildings/gatling-base.png') as im:
   if B.get('user_supplied_ring'):
    self.assertEqual(im.size,(86,54));self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/china/buildings/gatling-base.png').read_bytes()).hexdigest(),json.loads((ART/'ring-alignment/registration.json').read_text())['source_sha256'])
   else:
    self.assertEqual(im.size,(128,112));a=np.array(im);self.assertTrue({tuple(p) for p in a[a[:,:,3]>0]}<=colors)
  if B.get('user_supplied_ring'):
   self.assertLess(abs(22-27+B['body_offset'][1]+B['mount'][2]*64/1448),.02);self.assertEqual(32-27+B['body_offset'][1],0)
  else:self.assertAlmostEqual(39-56+B['body_offset'][1],-604*64/1448,places=6)
  old=ART/'platform-cut/integration/before-gatling-turret32.png';current=ROOT/'mods/ca/bits/china/buildings/gatling-turret32.png';self.assertEqual(old.read_bytes(),current.read_bytes())
 def test_stationary_base_and_full_render_occlusion(self):
  with Image.open(P/'base-raw.png') as im:base=im.convert('RGBA')
  with Image.open(P/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
  for facing,number in [(4,1),(12,9),(20,17),(28,25)]:
   with Image.open(P/f'turret-{facing:02d}-raw.png') as turret,Image.open(P/f'assembly-{number:02d}-raw.png') as full:self.assertEqual(Image.alpha_composite(base,turret.convert('RGBA')).tobytes(),full.convert('RGBA').tobytes())
 def test_selection_encloses_art(self):
  half=B['decoration_bounds']*128/1448/2;bottom=B['decoration_bounds']*64/1448/2
  for name,offset,mount in [('base',B['body_offset'][1],0)]+[(f'turret-{n:02d}',-34,B['mount'][2]) for n in range(32)]:
   path=ROOT/'mods/ca/bits/china/buildings/gatling-base.png' if name=='base' else P/'review'/(name+'.png')
   with Image.open(path) as im:a=np.array(im);width,height=im.size
   scale=B['body_scale'] if name=='base' else .65
   y,x=np.where(a[:,:,3]>0);x=(x+.5-width/2)*scale;y=(y+.5-height/2+offset)*scale-mount*64/1448
   self.assertTrue(np.all(abs(x)<=half));self.assertTrue(np.all(y<=bottom-.5*abs(x)));self.assertTrue(np.all(y>=-bottom-B['selection_height']+.5*abs(x)))
 def test_gameplay_and_all_eight_weapon_profiles_preserved(self):
  old=traits((I/'rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','chgatling'))
  self.assertIn('Image: chgatling.native',new['RenderSprites']);self.assertIn('Dimensions: 1,1',new['Building']);self.assertIn('QuantizedFacings: 32',new['BodyOrientation']);self.assertIn('SharedBarrelSequence',new)
  self.assertEqual(old['WithIdleOverlay@CHINABIB'].strip(),new['WithIdleOverlay@CHINABIB'].strip())
  shared=[k for k in new if k.startswith('SharedBarrelArmament@')];self.assertEqual(len(shared),8)
  for key in shared:
   value=new.pop(key);self.assertIn('LocalOffset: 623,-257,834, 623,257,834',value);new[key.replace('SharedBarrelArmament@','Armament@')]=value
  new.pop('SharedBarrelSequence')
  for data in (old,new):
   for key in ('RenderSprites','BodyOrientation','WithSpriteBody','WithSpriteTurret','IsometricSelectable'):data.pop(key,None)
   data['Building']=re.sub(r'\n\tLocalCenterOffset: [^\n]+','',data['Building'])
   data['Building']=re.sub(r'\n\tFootprintOffset: 0,0','',data['Building'])
   if 'ActorPreviewPlaceBuildingPreview' in data:data['ActorPreviewPlaceBuildingPreview']=re.sub(r'\n\tAnimated: false','',data['ActorPreviewPlaceBuildingPreview'])
   data['Turreted']=re.sub(r'\n\tOffset: [^\n]+','',data['Turreted'])
   for key in data:
    if key.startswith('Armament@GAT'):data[key]=re.sub(r'\n\tLocalOffset: [^\n]+','',data[key])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  for name,before in json.loads((I/'weapons-before.json').read_text()).items():self.assertEqual(before.strip(),resolved('--resolved-weapons',name).strip())
 def test_resolved_assets_and_short_flash(self):
  old=traits(resolved('--resolved-sequences','chgatling'));new=traits(resolved('--resolved-sequences','chgatling.native'))
  for name in ('idle','damaged-idle','base','damaged-base','dead'):self.assertIn('Filename: bits/china/buildings/gatling-base.png',new[name]);self.assertIn('Offset: 0,'+str(B['body_offset'][1]),new[name]);self.assertIn('Scale: 1',new[name])
  for name in ('turret','damaged-turret'):self.assertIn('Filename: bits/china/buildings/gatling-turret32.png',new[name]);self.assertIn('Facings: 32',new[name])
  for value in ['Filename: minigun.shp','Length: 2','Stride: 2','Frames: 0,2,6,8,12,14,18,20,24,26,30,32,36,38,42,44','Tick: 40','Scale: 1']:self.assertIn(value,new['muzzle'])
  for name in ('construction','icon'):self.assertEqual(old[name].strip(),new[name].strip())
if __name__=='__main__':unittest.main()
