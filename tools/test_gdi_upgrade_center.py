"""Upgrade Center PNG integration without changing upgrades, weapons or delivery."""
import hashlib,json,re,unittest
from calibrate_gdi_building_alignment import ROOT
from test_soviet_building_art import resolved

def traits(text):
 return {b.split(':',1)[0]:b for b in re.split(r'\n(?=[^\s#][^\n]*:)',text.strip())}

def preserved_traits_hash(text):
 fields=traits(text)
 for name in ('RenderSprites','WithBuildingBib','WithIdleOverlay@GDIBIB'):fields.pop(name,None)
 fields['IsometricSelectable']='\n'.join(line for line in fields['IsometricSelectable'].splitlines()
  if not line.strip().startswith(('DecorationBounds:','DecorationHeight:')))
 return hashlib.sha256(json.dumps({k:re.sub(r'\s+','',v) for k,v in fields.items()},sort_keys=True).encode()).hexdigest()

class GdiUpgradeCenterTests(unittest.TestCase):
 def test_original_png_and_all_nonvisual_rules_preserved(self):
  baseline=json.loads((ROOT/'tools/gdi-upgrade-center-gameplay-baseline.json').read_text())
  self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/gdi/buildings/upgrade center.png').read_bytes()).hexdigest(),baseline['SourceSHA256'])
  text=resolved('--resolved-rules','UPGC')
  self.assertEqual(preserved_traits_hash(text),baseline['BeforeGameplaySHA256'])
  self.assertIn('Image: upgc.gdi.png',text)
  self.assertNotIn('WithBuildingBib:',text)
  self.assertEqual(text.count('WithAmmoPipsDecoration:'),1)

 def test_all_original_module_and_effect_sequences_preserved(self):
  old=traits(resolved('--resolved-sequences','upgc'))
  new=traits(resolved('--resolved-sequences','upgc.gdi.png'))
  body={'idle','active','damaged-idle','damaged-active','dead'}
  self.assertEqual(old.keys(),new.keys())
  for name in old:
   with self.subTest(sequence=name):
    if name not in body:self.assertEqual(old[name],new[name])
    else:
     self.assertIn('Filename: bits/gdi/buildings/color-matched/upgrade center.png',new[name])
     self.assertIn('Offset: 1,-21',new[name])
     self.assertIn('Length: 1',new[name])
     self.assertIn('Scale: 1',new[name])

class GdiNavalYardTests(unittest.TestCase):
 def test_source_water_production_and_all_nonvisual_rules_preserved(self):
  baseline=json.loads((ROOT/'tools/gdi-naval-yard-gameplay-baseline.json').read_text())
  self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/gdi/buildings/naval yard.png').read_bytes()).hexdigest(),baseline['SourceSHA256'])
  text=resolved('--resolved-rules','SYRD.gdi')
  self.assertEqual(preserved_traits_hash(text),baseline['BeforeGameplaySHA256'])
  self.assertIn('Image: gsyrd.gdi.png',text)
  self.assertNotIn('WithIdleOverlay@GDIBIB:',text)
  self.assertNotRegex(text,r'Image: \w*bib\w*')
  self.assertIn('TerrainTypes: Water',text)
  self.assertIn('Footprint: XXX xxx XXX',text)
  self.assertNotIn('WithBuildingBib:',text)

 def test_naval_body_and_inherited_sequences(self):
  old=traits(resolved('--resolved-sequences','gsyrd'))
  new=traits(resolved('--resolved-sequences','gsyrd.gdi.png'))
  self.assertEqual(old.keys(),new.keys())
  for name in old:
   with self.subTest(sequence=name):
    if name not in ('idle','damaged-idle'):self.assertEqual(old[name],new[name])
    else:
     self.assertIn('Filename: bits/gdi/buildings/color-matched/naval yard.png',new[name])
     self.assertIn('Offset: 8,5',new[name])
     self.assertIn('Scale: 1',new[name])
     self.assertIn('Length: 1',new[name])

if __name__=='__main__':unittest.main()
