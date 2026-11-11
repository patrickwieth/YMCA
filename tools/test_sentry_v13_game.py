"""Native-resolution V13 export, true 32 directions and test-map-only binding."""
from pathlib import Path
import hashlib,json,math,os,re,subprocess,tempfile,unittest
from PIL import Image
from test_soviet_building_art import ROOT,resolved,traits
STUDY=ROOT/'art-source/soviet-sentry/game-v13-32-native'
BINDING=json.loads((STUDY/'game-binding.json').read_text())

class SentryV13GameTests(unittest.TestCase):
 def test_native_render_sheet_and_source_palette(self):
  attachment=json.loads((STUDY/'attachment.json').read_text())
  self.assertEqual(attachment['frame_size'],[120,120])
  self.assertEqual(attachment['native_render_scale'],.5)
  self.assertEqual(BINDING['sequence_scale'],1)
  with Image.open(ROOT/'mods/ca'/BINDING['sheet']) as image:
   self.assertEqual(image.info.get('FrameSize'),'120,120')
   self.assertEqual(image.info.get('FrameAmount'),'32')
   sheet=image.convert('RGBA')
  self.assertEqual(sheet.size,(960,480))
  with Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png') as image:palette=set(image.convert('RGBA').getdata())
  self.assertLessEqual({c for c in sheet.getdata() if c[3]},palette)
  self.assertLessEqual(set(sheet.getchannel('A').getdata()),{0,255})
  unique=set()
  for entry in BINDING['frames']:
   i=entry['frame'];index=entry['render_index']
   frame=sheet.crop(((i%8)*120,(i//8)*120,(i%8+1)*120,(i//8+1)*120))
   with Image.open(STUDY/f'final-turret-{index:03d}.png') as rendered:self.assertEqual(frame.tobytes(),rendered.convert('RGBA').tobytes())
   for prefix,suffix in (('render-','-raw'),('albedo-',''),('weapon-mask-',''),('edges/part-ids-','')):
    with Image.open(STUDY/f'{prefix}{index:03d}{suffix}.png') as raw:self.assertEqual(raw.size,(120,120))
   x0,y0,x1,y1=frame.getbbox();self.assertTrue(x0>0 and y0>0 and x1<120 and y1<120)
   unique.add(frame.tobytes())
  self.assertEqual(len(unique),32)

 def test_engine_sprite_loader_recognizes_all_frames(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-sentry-v13-frames'],cwd=ROOT,env=env,check=True)

 def test_compass_and_stationary_base_attachment(self):
  vectors={e['angle']:e['gun_screen_vector'] for e in json.loads((STUDY/'projected-directions.json').read_text())}
  for e in BINDING['frames']:
   x,y=vectors[e['render_index']];bearing=math.degrees(math.atan2(-x,-2*y))%360
   self.assertLess(abs((bearing-e['frame']*11.25+180)%360-180),.01)
  self.assertGreater(vectors[BINDING['frames'][24]['render_index']][0],0) # east = 768
  self.assertLess(vectors[BINDING['frames'][8]['render_index']][0],0) # west = 256
  self.assertEqual(BINDING['frames'][12]['render_index'],0) # SW = 384
  self.assertTrue(all(abs(v)<.5 for v in BINDING['pixel_alignment_error']))
  source=ROOT/'mods/ca/bits/soviet/buildings/sentry base.png'
  self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),BINDING['source_base_sha256'])
  with Image.open(source) as original,Image.open(ROOT/'mods/ca'/BINDING['base_sheet']) as small:
   self.assertEqual(small.size,(74,43))
   self.assertEqual(small.convert('RGBA').tobytes(),original.convert('RGBA').resize((74,43),Image.Resampling.NEAREST).tobytes())

 def test_ground_contacts_center_on_bib(self):
  points=BINDING['base_ground_contacts'];ox,oy=BINDING['base_sequence_offset']
  self.assertEqual(sum(x+.5+ox-74/2 for x,y in points)/len(points),0)
  self.assertEqual(sum(y+.5+oy-43/2 for x,y in points)/len(points),0)

 def test_testmap_decoration_encloses_every_facing(self):
  import numpy as np
  with Image.open(ROOT/'mods/ca'/BINDING['sheet']) as image:a=np.array(image.convert('RGBA'))
  for i in range(32):
   frame=a[(i//8)*120:(i//8+1)*120,(i%8)*120:(i%8+1)*120]
   ys,xs=np.where(frame[:,:,3]>0);x=xs+.5-4-60;y=ys+.5-24-60-112*64/1448
   self.assertTrue(np.all((x>=-56)&(x<=56)))
   self.assertTrue(np.all(y>=-77+.5*np.abs(x)))
   self.assertTrue(np.all(y<=28-.5*np.abs(x)))

 def test_native_sequences_and_legacy_states(self):
  old=traits(resolved('--resolved-sequences','ftur.soviet'))
  new=traits(resolved('--resolved-sequences','ftur.soviet.v13'))
  self.assertEqual(old.keys(),new.keys())
  for name in old:
   if name in ('turret','damaged-turret'):
    for field in ('Filename: '+BINDING['sheet'],'Offset: -4,-24','Facings: 32','Frames: '+', '.join(map(str,range(32))),'Scale: 1'):
     self.assertIn(field,new[name])
   elif name in ('idle','damaged-idle'):
    for field in ('Filename: '+BINDING['base_sheet'],'Scale: 1','Offset: 0,-3'):self.assertIn(field,new[name])
   else:self.assertEqual(old[name],new[name])

 def test_independent_sentry_does_not_override_flame_tower(self):
  source=ROOT/'mods/ca/maps/testmap1.oramap';before=source.read_bytes()
  default=traits(resolved('--resolved-rules','FTUR'))
  self.assertIn('Image: ftur.soviet',default['RenderSprites'])
  self.assertNotIn('ftur.soviet.v13',default['RenderSprites'])
  with tempfile.TemporaryDirectory() as directory:
   path=Path(directory)/'prototype.oramap'
   subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'tools/prepare-test-game-map.ps1'),'-SourceMap',str(source),'-DestinationMap',str(path)],check=True)
   env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
   command=['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--resolved-rules']
   flame=traits(subprocess.check_output(command+['FTUR',str(path)],cwd=ROOT,env=env,text=True))
   self.assertEqual(default,flame)
   actual=traits(subprocess.check_output(command+['SENTRY.SOVIET',str(path)],cwd=ROOT,env=env,text=True))
   self.assertEqual(actual,traits(resolved('--resolved-rules','SENTRY.SOVIET')))
   self.assertNotIn('Buildable',actual)
   self.assertIn('Image: ftur.soviet.v13',actual['RenderSprites'])
   self.assertIn('InitialFacing: 384',actual['Turreted'])
   self.assertIn('TurnSpeed: 32',actual['Turreted'])
   self.assertIn('QuantizedFacings: 32',actual['BodyOrientation'])
   self.assertIn('Image: sovietbib1x1',actual['WithIdleOverlay@SOVIETBIB'])
   self.assertIn('DecorationHeight: 49',actual['IsometricSelectable'])
   self.assertEqual(default.keys()-{'Buildable'},actual.keys())
   allowed={'RenderSprites':('Image',),'Tooltip':('Name',),'WithIdleOverlay@SOVIETBIB':('Image',),'Turreted':('InitialFacing','TurnSpeed'),'BodyOrientation':('QuantizedFacings',),'IsometricSelectable':('DecorationBounds','DecorationHeight')}
   for name in actual:
    def clean(text):
     for key in allowed.get(name,()):text=re.sub(r'(?m)^[\t ]+'+key+r':[^\n]*\n?','',text)
     return text.strip()
    self.assertEqual(clean(default[name]),clean(actual[name]),name)
  self.assertEqual(source.read_bytes(),before)

if __name__=='__main__':unittest.main()
