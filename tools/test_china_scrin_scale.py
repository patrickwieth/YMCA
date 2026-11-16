"""Visual2x, China Soviet foundations/shared yard, and preservation of gameplay."""
from pathlib import Path
import json,re,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];P=ROOT/'art-source/china-scrin-scale2x'
B=json.loads((P/'actors-before.json').read_text());S=json.loads((P/'sequences-before.json').read_text());binding=json.loads((P/'binding.json').read_text())
NEW_SUPPLIED={n.lower() for n in json.loads((ROOT/'art-source/new-faction-buildings/binding.json').read_text())}
ACTUAL={n.lower():n for f in ('china','scrin') for n in re.findall(r'^([\w.]+):',(ROOT/f'mods/ca/rules/{f}/structures.yaml').read_text(),re.M)}
def scale(fields,name):
 m=re.search(r'\n\tScale: ([^\n]+)',fields[name]);d=re.search(r'\n\tScale: ([^\n]+)',fields.get('Defaults',''))
 return float(m[1]) if m else float(d[1]) if d else 1
class ChinaScrinScaleTests(unittest.TestCase):
 def test_body_states_double_icons_and_construction_unchanged(self):
  for image,states in binding['scales'].items():
   with self.subTest(image=image):
    old=traits(S[image]);now=traits(resolved('--resolved-sequences',image))
    for state,expected in states.items():self.assertEqual(scale(now,state),expected,(image,state))
    for state in old:
     if state.startswith('icon') or state=='construction':self.assertEqual(old[state],now[state],(image,state))
 def test_gameplay_preserved_and_china_bibs(self):
  for name,before in B.items():
   with self.subTest(actor=name):
    old=traits(before);now=traits(resolved('--resolved-rules',ACTUAL.get(name,name.upper())))
    if name in ('syrd','spen'):
     self.assertEqual(old,now);continue
    old.pop('IsometricSelectable',None);now.pop('IsometricSelectable',None)
    if name=='swal':
     self.assertNotIn('Buildable',now);old.pop('Buildable',None)
    if name=='chgatling':
     self.assertIn('Image: chgatling.native',now['RenderSprites'])
     self.assertIn('SharedBarrelSequence',now);now.pop('SharedBarrelSequence')
     for key in list(now):
      if key.startswith('SharedBarrelArmament@'):now[key.replace('SharedBarrelArmament@','Armament@')]=now.pop(key)
     for data in (old,now):
      for key in ('RenderSprites','BodyOrientation','WithSpriteBody','WithSpriteTurret'):data.pop(key,None)
      for key in list(data):
       field='LocalOffset' if key.startswith('Armament@GAT') else 'Offset' if key=='Turreted' else None
       if field:data[key]=re.sub(r'\n\t'+field+r': [^\n]+','',data[key])
    if name in NEW_SUPPLIED:
     self.assertIn('.supplied',now['RenderSprites'])
     self.assertNotIn('WithBuildingBib',now)
     if name.startswith('ch') and name!='chsyrd':self.assertIn('WithIdleOverlay@CHINABIB',now)
     else:self.assertNotIn('WithIdleOverlay@CHINABIB',now)
     for data in (old,now):
      data.pop('WithBuildingBib',None)
      data['RenderSprites']=re.sub(r'\n\tImage: [^\n]+','',data['RenderSprites'])
      for key in list(data):
       if key.startswith('WithIdleOverlay@SMOKE'):data[key]=re.sub(r'\n\t(?:Offset|Palette): [^\n]+','',data[key])
      if name.startswith('scol'):data['Armament']=re.sub(r'\n\tLocalOffset: [^\n]+','',data['Armament'])
      if name=='chsilo':data['NukePower']=re.sub(r'\n\tSpawnOffset: [^\n]+','',data['NukePower'])
      if name.startswith('chpropagandatower'):
       for key in data:
        if key.startswith('Armament@'):data[key]=re.sub(r'\n\tLocalOffset: [^\n]+','',data[key])
    if name in ('sfac','reac','rea2','proc.scrin','port','wsph','nerv','scrt','grav','sign'):
     # Supplied PNGs replace these actors' historic2x sprites, not their gameplay.
     self.assertIn('Image: '+name+'.supplied',now['RenderSprites'])
     self.assertNotIn('WithBuildingBib',now)
     old.pop('WithBuildingBib',None)
     for data in (old,now):
      for key,fields in [('WithSpriteBody','Palette|IsPlayerPalette'),('WithDeathAnimation','DeathSequencePalette|DeathPaletteIsPlayerPalette')]:
       if key in data:data[key]=re.sub(r'\n\t(?:'+fields+r'): [^\n]+','',data[key])
     now['RenderSprites']=re.sub(r'\n\tImage: [^\n]+','',now['RenderSprites'])
     if name=='grav':
      for key in ('WithIdleOverlay@orb','WithIdleOverlay@pylon'):
       old.pop(key,None);self.assertNotIn(key,now)
    if name in ('shar','ptur'):
     # Later supplied-art replacement supersedes the old2x visual registration only.
     self.assertIn('Image: '+name+'.native',now['RenderSprites'])
     self.assertNotIn('WithBuildingBib',now)
     for key in ('RenderSprites','BodyOrientation','WithSpriteBody','WithSpriteTurret','WithBuildingBib'):
      old.pop(key,None);now.pop(key,None)
     for key in ('Turreted','Armament@PRIMARY' if name=='shar' else 'Armament'):
      field='Offset' if key=='Turreted' else 'LocalOffset'
      for data in (old,now):data[key]=re.sub(r'\n\t'+field+r': [^\n]+','',data[key])
    if name.startswith('ch') and name!='chsyrd':
     old.pop('WithBuildingBib',None)
     self.assertNotIn('WithBuildingBib',now)
     overlay=now.pop('WithIdleOverlay@CHINABIB')
     self.assertRegex(overlay,r'Image: sovietbib[1-4]x[1-4]')
     self.assertIn('RequiresCondition: !build-incomplete',overlay)
    if name!='chsyrd':
     for trait,value in list(old.items()):
      key='LocalOffset' if trait.startswith('Armament') else 'Offset' if trait.startswith('Turreted') else None
      if key:
       old[trait]=re.sub(r'(\n\t'+key+r': )([^\n]+)',lambda m:m[1]+','.join(str(2*int(n.strip())) for n in m[2].split(',')),value)
    self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in now.items()})
 def test_chinese_yard_replacement_preserves_soviet(self):
  soviet=traits(resolved('--resolved-rules','SPEN'));china=traits(resolved('--resolved-rules','CHSYRD'))
  self.assertIn('Image: spen.soviet',soviet['RenderSprites']);self.assertIn('Image: chsyrd.supplied',china['RenderSprites'])
  self.assertIn('TerrainTypes: Water',china['Building'])
  native=traits(resolved('--resolved-sequences','chsyrd.supplied'))
  for name in ('idle','damaged-idle'):
   self.assertIn('Filename: bits/china/buildings/naval yard.png',native[name]);self.assertEqual(scale(native,name),1.5)
  states=traits(resolved('--resolved-sequences','spen.soviet'))
  for name in ('idle','damaged-idle'):
   self.assertIn('Filename: bits/soviet/buildings/shipyard.png',states[name]);self.assertEqual(scale(states,name),1)
if __name__=='__main__':unittest.main()
