"""Pass30: recessed yaw collar,25-degree bore, fixed battery and unchanged gameplay."""
from pathlib import Path
from PIL import Image
import unittest,json,hashlib,re,math
import numpy as np
from test_soviet_building_art import resolved,traits
from building_presentation_test_helpers import normalize_building_presentation
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/scrin shard launcher';P=ART/'pass-30';B=json.loads((ART/'integration-pass30/binding.json').read_text());G=json.loads((P/'geometry.json').read_text());N=G['frame_size']
class ScrinShardLauncherArtTests(unittest.TestCase):
 def test_original_history_and_previous_install_preserved(self):
  self.assertEqual(hashlib.sha256((ART/'shard_launcher-332414650-0.png').read_bytes()).hexdigest(),B['source_sha256'])
  for i in range(1,31):self.assertTrue((ART/f'pass-{i:02d}/shard-launcher.blend').exists())
  with Image.open(P/'review/base.png') as a,Image.open(ROOT/'mods/ca/bits/scrin/buildings/shard-launcher-base.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
  old=json.loads((ART/'integration-pass22/binding.json').read_text())
  for name,sha in old['assets'].items():self.assertEqual(hashlib.sha256((ART/'integration-pass30'/('previous-'+name)).read_bytes()).hexdigest(),sha)
  for name,sha in B['assets'].items():self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/scrin/buildings'/name).read_bytes()).hexdigest(),sha)
 def test_elevation_and_rotary_separation(self):
  self.assertEqual(G['elevation_degrees'],25)
  audit=json.loads((P/'saved-scene-audit.json').read_text())
  self.assertTrue(audit['all_moving_parts_yaw_parented']);self.assertTrue(audit['fixed_vertices_unchanged'])
  self.assertEqual(audit['fixed_shards'],4)
  sweep=json.loads((P/'swept-collision-audit.json').read_text());self.assertGreaterEqual(sweep['samples'],64);self.assertEqual(sweep['collisions'],[])
  self.assertAlmostEqual(audit['measured_bore_degrees'],25,places=2)
  self.assertEqual(audit['blend_sha256'],hashlib.sha256((P/'shard-launcher.blend').read_bytes()).hexdigest())
  with Image.open(P/'base-raw.png') as a,Image.open(P/'base-control-raw.png') as b:self.assertEqual(a.tobytes(),b.tobytes())
 def test_native_pixels_frames_and_compass(self):
  runtime=ROOT/'mods/ca/bits/scrin/buildings'
  with Image.open(runtime/'shard-launcher-turret32.png') as atlas:
   self.assertEqual(atlas.info['FrameSize'],f'{N},{N}');self.assertEqual(atlas.info['FrameAmount'],'32')
   for i,n in enumerate(B['frame_order']):
    frame=atlas.crop((i%8*N,i//8*N,(i%8+1)*N,(i//8+1)*N))
    with Image.open(P/f'review/turret-{n:02d}.png') as original:self.assertEqual(frame.tobytes(),original.tobytes())
    bbox=frame.getbbox();self.assertGreater(min(bbox[:2]),0);self.assertLess(max(bbox[2:]),N)
    a=n*math.pi/16;dx=(-math.sin(a)+math.cos(a))/math.sqrt(2);dy=(-math.sin(a)-math.cos(a))/math.sqrt(8)
    bearing=math.degrees(math.atan2(-dx,-2*dy))%360;self.assertLess(abs((bearing-i*11.25+180)%360-180),.001)
 def test_split_parts_match_full_render_occlusion(self):
  with Image.open(P/'base-raw.png') as im:base=im.convert('RGBA')
  for n,name in [(30,'reference-raw.png'),(8,'assembly-08-raw.png'),(16,'assembly-16-raw.png'),(24,'assembly-24-raw.png')]:
   with Image.open(P/f'turret-{n:02d}-raw.png') as turret,Image.open(P/name) as full:self.assertEqual(Image.alpha_composite(base,turret.convert('RGBA')).tobytes(),full.convert('RGBA').tobytes())
 def test_selection_decoration_encloses_native_frames(self):
  half=1920*128/1448/2;bottom=1920*64/1448/2
  for n in B['frame_order']:
   with Image.open(P/f'review/turret-{n:02d}.png') as im:a=np.array(im)
   yy,xx=np.where(a[:,:,3]>0);x=(xx+.5-N/2)*.58;y=(yy+.5-N/2+B['turret_offset'][1])*.58-B['mount'][2]*64/1448
   self.assertTrue(np.all(np.abs(x)<=half+.5));self.assertTrue(np.all(y>=-bottom-B['selection_height']+.5*np.abs(x)-.5));self.assertTrue(np.all(y<=bottom-.5*np.abs(x)+.5))
 def test_resolved_actor_and_unchanged_gameplay(self):
  old=traits((ART/'integration-pass14/rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','SHAR'))
  self.assertIn('Dimensions: 1,1',new['Building']);self.assertIn('Footprint: x',new['Building']);self.assertIn('Image: shar.native',new['RenderSprites'])
  self.assertNotIn('WithBuildingBib',new);self.assertIn('LocalOffset: 577,-35,1490',new['Armament@PRIMARY']);self.assertIn('Offset: 0,0,375',new['Turreted'])
  for data in (old,new):
   normalize_building_presentation(data)
   for key in ('RenderSprites','BodyOrientation','WithBuildingBib','WithSpriteBody','WithSpriteTurret','IsometricSelectable','WithIdleOverlay@SCRINBIB'):data.pop(key,None)
   data['Turreted']=re.sub(r'\n\tOffset: [^\n]+','',data['Turreted']);data['Armament@PRIMARY']=re.sub(r'\n\tLocalOffset: [^\n]+','',data['Armament@PRIMARY'])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
 def test_sequence_resolution_and_legacy_fallbacks(self):
  seq=traits(resolved('--resolved-sequences','shar.native'));old=traits(resolved('--resolved-sequences','shar'))
  for name in ('idle','damaged-idle','base','damaged-base'):self.assertIn('Filename: bits/scrin/buildings/shard-launcher-base.png',seq[name])
  for name in ('turret','damaged-turret'):
   self.assertIn('Filename: bits/scrin/buildings/shard-launcher-turret32.png',seq[name]);self.assertIn('Facings: 32',seq[name]);self.assertIn('Offset: 0,-59.423128',seq[name])
  for name in ('construction','icon'):self.assertEqual(old[name].strip(),seq[name].strip())
if __name__=='__main__':unittest.main()
