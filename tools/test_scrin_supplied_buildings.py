"""Direct original paths, native placement, unchanged gameplay and retained UI/construction."""
from pathlib import Path
import json,hashlib,unittest,re
import numpy as np
from PIL import Image
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/scrin-building-bindings';B=json.loads((ART/'binding.json').read_text());OLD=json.loads((ART/'rules-before.json').read_text())
class ScrinSuppliedBuildingTests(unittest.TestCase):
 def test_originals_and_selection_envelope(self):
  for actor,b in B.items():
   with self.subTest(actor=actor):
    p=ROOT/'mods/ca'/b['filename'];self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),b['sha256'])
    with Image.open(p) as im:y,x=np.where(np.array(im.convert('RGBA'))[:,:,3]>0)
    x=(x+.5-128)*b['scale'];y=(y+.5-192+b['offset'][1])*b['scale'];half=128*b['scale'];depth=64*b['scale']
    self.assertTrue(np.all(np.abs(x)<=half));self.assertTrue(np.all(y<=depth-.5*np.abs(x)))
    self.assertTrue(np.all(y>=-depth-b['height']+.5*np.abs(x)))
 def test_gameplay_and_direct_actor_binding(self):
  for actor,b in B.items():
   with self.subTest(actor=actor):
    old=traits(OLD[actor]);new=traits(resolved('--resolved-rules',actor));self.assertIn('Image: '+b['image'],new['RenderSprites'])
    self.assertNotIn('WithBuildingBib',new)
    if b['palette']:
     self.assertIn('Palette: '+b['palette'],new['WithSpriteBody'])
     self.assertIn('IsPlayerPalette: false',new['WithSpriteBody'])
    for key in ('WithIdleOverlay@orb','WithIdleOverlay@pylon'):
     if actor=='GRAV':self.assertNotIn(key,new);old.pop(key,None)
    for d in (old,new):
     for key in ('IsometricSelectable','WithBuildingBib','WithIdleOverlay@SCRINBIB'):d.pop(key,None)
     for key,fields in [('WithSpriteBody','Palette|IsPlayerPalette'),('WithDeathAnimation','DeathSequencePalette|DeathPaletteIsPlayerPalette')]:
      if key in d:d[key]=re.sub(r'\n\t(?:'+fields+r'): [^\n]+','',d[key])
     d['RenderSprites']=re.sub(r'\n\tImage: [^\n]+','',d['RenderSprites'])
    self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
 def test_headquarter_finish_and_variant(self):
  a=ROOT/'art-source/scrin-headquarter';data=json.loads((a/'finish.json').read_text())
  self.assertEqual(hashlib.sha256((a/'headquarter-original.png').read_bytes()).hexdigest(),data['source_sha256'])
  self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/scrin/buildings/headquarter.png').read_bytes()).hexdigest(),data['finished_sha256'])
  self.assertLess(data['changed_pixels'],200)
  with Image.open(a/'headquarter-original.png') as original,Image.open(a/'headquarter-finished.png') as finished:
   self.assertEqual(original.crop((0,0,256,197)).tobytes(),finished.crop((0,0,256,197)).tobytes())
  old=traits(OLD['SFAC.Nukular']);new=traits(resolved('--resolved-rules','SFAC.Nukular'))
  self.assertIn('Image: sfac.supplied',new['RenderSprites'])
  for d in (old,new):
   for key in ('RenderSprites','IsometricSelectable','WithBuildingBib','WithIdleOverlay@SCRINBIB'):d.pop(key,None)
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
 def test_sequences_and_tech_center_assignment(self):
  self.assertEqual(B['SCRT']['filename'],'bits/scrin/buildings/tech center.png')
  self.assertFalse(any(b['filename'].endswith('/foundry.png') for b in B.values()))
  for actor,b in B.items():
   with self.subTest(actor=actor):
    old=traits(resolved('--resolved-sequences',actor.lower()));new=traits(resolved('--resolved-sequences',b['image']))
    for state in ('idle','damaged-idle','place','dead')+(('build','damaged-build','pdox','damaged-pdox') if actor=='SFAC' else ()):
     self.assertIn('Filename: '+b['filename'],new[state]);self.assertIn('Length: 1',new[state]);self.assertIn('Anchor: Bottom',new[state])
     self.assertIn('Offset: 0,'+str(b['offset'][1]),new[state])
    for state in ('icon','construction'):self.assertEqual(old[state].strip(),new[state].strip())
if __name__=='__main__':unittest.main()
