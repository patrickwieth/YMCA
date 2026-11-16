"""Supplied China/Scrin originals, variants and gameplay isolation."""
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,re,unittest
from test_soviet_building_art import resolved,traits
from building_presentation_test_helpers import normalize_building_presentation
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/new-faction-buildings';B=json.loads((OUT/'binding.json').read_text());OLD=json.loads((OUT/'rules-before.json').read_text())
class NewFactionBuildingsTests(unittest.TestCase):
 def test_requested_china_placement_and_pdl_selection(self):
  expected={'chweap':[36,2],'chairport':[8,28],'chbarracks':[0,20],'chsupply':[0,19],'chinternetcenter':[0,20],'chpower':[0,12],'chpower2':[0,24],'chpropagandatower':[0,-13],'chpropagandatower.pdl':[0,-13]}
  for actor,offset in expected.items():
   b=B[actor];r=traits(resolved('--resolved-rules',actor));selection=r['IsometricSelectable'];bounds=f"{b['bounds']}, {b['bounds']}";self.assertEqual(b['offset'],offset)
   x=round(offset[0]*b['scale']*1448/128);y=round(b.get('selection_offset',[0,0])[1]*1448/64)
   if x or y:bounds+=f', {x}, {y}'
   if actor.startswith('chpropagandatower'):self.assertEqual(b['height'],80)
   if actor in ('chpower','chpower2'):self.assertIn('Image: sovietbib3x3',r['WithIdleOverlay@CHINABIB'])
   for field in ['Bounds','DecorationBounds']:self.assertIn(field+': '+bounds,selection)
   for field in ['Height','DecorationHeight']:self.assertRegex(selection,r'(?m)^\t'+field+': '+str(b['height'])+r'$')
   if actor=='chinternetcenter':self.assertIn('Image: sovietbib4x4',r['WithIdleOverlay@CHINABIB'])
 def test_originals_and_protected_files_unchanged(self):
  for file,sha in json.loads((OUT/'source-hashes.json').read_text()).items():self.assertEqual(hashlib.sha256((ROOT/file).read_bytes()).hexdigest(),sha,file)
 def test_all_supplied_files_bound_directly(self):
  names={Path(b['filename']).name for b in B.values() if '/china/' in b['filename']}|{'radar.png'}
  supplied={p.name for p in (ROOT/'mods/ca/bits/china/buildings').glob('*.png') if not p.name.startswith('gatling-')};self.assertEqual(names,supplied)
  prior=json.loads((ROOT/'art-source/scrin-building-bindings/binding.json').read_text())
  scrin={Path(b['filename']).name for b in list(B.values())+list(prior.values()) if '/scrin/' in b['filename']}
  supplied_scrin={p.name for p in (ROOT/'mods/ca/bits/scrin/buildings').glob('*.png') if p.name!='foundry.png' and not p.name.startswith(('shard-launcher-','plasma-cannon-','bib'))};self.assertEqual(scrin,supplied_scrin)
  for actor,b in B.items():
   with self.subTest(actor=actor):
    r=traits(resolved('--resolved-rules',actor));self.assertIn('Image: '+b['image'],r['RenderSprites']);s=traits(resolved('--resolved-sequences',b['image']))
    for state in b['states']:
     self.assertIn('Filename: '+b['filename'],s[state]);self.assertIn('Frames: 0',s[state]);self.assertIn('Length: 1',s[state]);self.assertIn('Offset: '+','.join(map(str,b['offset'])),s[state])
 def test_selection_contains_visible_pixels(self):
  for actor,b in B.items():
   with self.subTest(actor=actor),Image.open(ROOT/'mods/ca'/b['filename']) as im:
    self.assertEqual(im.mode,'RGBA');y,x=np.where(np.array(im)[:,:,3]>0);x=(x+.5-b['size']/2)*b['scale'];y=(y+.5-b['size']*.75+b['offset'][1])*b['scale']-b.get('selection_offset',[0,0])[1];hw=b['bounds']*128/1448/2;hh=b['bounds']*64/1448/2
    self.assertTrue(np.all(abs(x)<=hw));self.assertTrue(np.all(y<=hh-.5*abs(x)));self.assertTrue(np.all(y>=-hh-b['height']+.5*abs(x)))
 def test_gameplay_footprints_docking_and_variants_preserved(self):
  for actor in B:
   with self.subTest(actor=actor):
    old=traits(OLD[actor]);new=traits(resolved('--resolved-rules',actor));self.assertNotIn('WithBuildingBib',new)
    if B[actor].get('bib'):
     self.assertEqual(re.sub(r'Image: sovietbib\dx\d','Image: '+B[actor]['bib'],old['WithIdleOverlay@CHINABIB'].strip()),new['WithIdleOverlay@CHINABIB'].strip());self.assertIn('Image: '+B[actor]['bib'],new['WithIdleOverlay@CHINABIB'])
    else:self.assertNotIn('WithIdleOverlay@CHINABIB',new)
    for data in (old,new):
     normalize_building_presentation(data)
     for key in ['IsometricSelectable','WithBuildingBib','WithIdleOverlay@CHINABIB','WithIdleOverlay@SCRINBIB']:data.pop(key,None)
     data['RenderSprites']=re.sub(r'\n\tImage: [^\n]+','',data['RenderSprites'])
     for key in list(data):
      if key.startswith('WithIdleOverlay@SMOKE'):data[key]=re.sub(r'\n\t(?:Offset|Palette): [^\n]+','',data[key])
     if actor.startswith('SCOL'):data['Armament']=re.sub(r'\n\tLocalOffset: [^\n]+','',data['Armament'])
     if actor=='chsilo':data['NukePower']=re.sub(r'\n\tSpawnOffset: [^\n]+','',data['NukePower'])
     if actor.startswith('chpropagandatower'):
      for key in data:
       if key.startswith('Armament@'):data[key]=re.sub(r'\n\tLocalOffset: [^\n]+','',data[key])
    self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  self.assertEqual(traits(OLD['chcommand.radar']),traits(resolved('--resolved-rules','chcommand.radar')))
 def test_icons_construction_repair_and_effects(self):
  for actor,b in B.items():
   parent='chcommand' if actor=='chcommand.Nukular' else 'scol' if actor=='SCOL.Temp' else actor.lower()
   old=traits(resolved('--resolved-sequences',b.get('sequence_parent',parent)));new=traits(resolved('--resolved-sequences',b['image']))
   for key in old:
    if key.startswith('icon') or key in ['construction','emp-overlay','chrono-overlay','mind-overlay']:self.assertEqual(old[key].strip(),new[key].strip(),(actor,key))
  s=traits(resolved('--resolved-sequences','srep.supplied'))
  for state in ['idle','damaged-idle','active','damaged-active']:self.assertIn('Filename: bits/scrin/buildings/repair bay.png',s[state]);self.assertIn('ZOffset: -1535',s[state])
  radar=traits(resolved('--resolved-sequences','chcommand.supplied'))['radar'];self.assertIn('Filename: bits/china/buildings/radar.png',radar);offset=list(map(float,re.search(r'Offset: ([^\n]+)',radar)[1].split(',')));self.assertAlmostEqual((offset[0]+11)*1.5,1,places=4);self.assertAlmostEqual((offset[1]+125)*1.5,-1,places=4)
  rift=traits(resolved('--resolved-sequences','rift.supplied'))['active-overlay'];self.assertIn('Filename: bits/scrin/buildings/rift generator.png',rift);self.assertIn('Length: 13',rift);self.assertIn('BlendMode: Additive',rift)
 def test_reactor_smoke_palette_scale_and_anchor(self):
  for actor in ['chpower','chpower2']:
   r=traits(resolved('--resolved-rules',actor));overlays=[v for k,v in r.items() if k.startswith('WithIdleOverlay@SMOKE')]
   self.assertEqual(len(overlays),1 if actor=='chpower' else 2)
   for overlay in overlays:self.assertIn('Palette: effect',overlay)
   s=traits(resolved('--resolved-sequences',actor+'.supplied'))['idle-anim2']
   for value in ['Scale: 2','Alpha: 0.8','Offset: -9,-14','Length: 15','Tick: 60']:self.assertIn(value,s)
 def test_silo_launch_and_shipyard(self):
  b=B['chsilo'];r=traits(resolved('--resolved-rules','chsilo'));raw=re.search(r'SpawnOffset: (.*)',r['NukePower'])[1];f,l,z=map(int,raw.split(','));x,y=b['launch_pixel']
  self.assertEqual(f,0);self.assertLess(abs(l*128/1448-(x-128)*b['scale']),.1);self.assertLess(abs(-z*64/1448-(y-192+b['offset'][1])*b['scale']),.1)
  yard=traits(resolved('--resolved-rules','CHSYRD'));self.assertIn('TerrainTypes: Water',yard['Building']);self.assertEqual(len([k for k in yard if k.startswith('Exit@')]),4);self.assertIn('Produces: Ship, Submarine, Boat',yard['Production'])
  self.assertIn('Image: spen.soviet',traits(resolved('--resolved-rules','SPEN'))['RenderSprites'])
 def test_storm_column_emitter_registration(self):
  for actor in ['SCOL','SCOL.Temp']:
   b=B[actor];r=traits(resolved('--resolved-rules',actor));self.assertIn('Dimensions: 1,1',r['Building']);self.assertIn('Weapon: StormColumnZap',r['Armament'])
   raw=re.search(r'LocalOffset: (.*)',r['Armament'])[1];values=[int(n) for n in raw.split(',')];self.assertEqual(len(values),15)
   for i,(x,y) in enumerate(b['emitter_pixels']):
    f,l,z=values[i*3:i*3+3];self.assertEqual(f,0);self.assertLess(abs(l*128/1448-(x-128)*b['scale']),.1);self.assertLess(abs(-z*64/1448-(y-192+b['offset'][1])*b['scale']),.1)
if __name__=='__main__':unittest.main()
