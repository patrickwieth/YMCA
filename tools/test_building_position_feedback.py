"""Exact requested sprite/selection translations, unchanged gameplay and native Nod bib."""
from pathlib import Path
import json,re,hashlib,unittest
from PIL import Image
import numpy as np
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/nod-bib-followup'
def normalize_followup(fields):
 if 'Building' in fields:fields['Building']=re.sub(r'\n\t(?:Dimensions|Footprint|FootprintOffset|LocalCenterOffset): [^\n]+','',fields['Building'])
 if 'RenderSprites' in fields:fields['RenderSprites']=re.sub(r'\n\tHideBodyWhileBuilding: true','',fields['RenderSprites'])
 if 'WithIdleOverlay@SCRINBIB' in fields:fields['WithIdleOverlay@SCRINBIB']=re.sub(r'\n\tRequiresCondition: [^\n]+','',fields['WithIdleOverlay@SCRINBIB'])
 # These later authorized changes are validated exactly by --check-bib-occupancy.
 return fields
class BuildingPositionFeedbackTests(unittest.TestCase):
 def test_requested_relative_positions_and_gameplay(self):
  records=json.loads((OUT/'position-feedback.json').read_text());scrin={e['actor']:e for e in json.loads((ROOT/'art-source/scrin-rear-alignment/plan.json').read_text())}
  for e in records:
   actor=e['actor'];old=traits((OUT/(actor.lower()+'-before.yaml')).read_text());new=traits(resolved('--resolved-rules',actor));seq=traits(resolved('--resolved-sequences',e['image']))
   for name,value in e['sequences'].items():
    actual=[float(x) for x in re.search(r'\n\tOffset: ([^\n]+)',seq[name])[1].split(',')];self.assertTrue(np.allclose(actual,value,atol=1e-4),(actor,name,actual,value))
   expected_bounds=[2896,2896,11,-453] if actor=='UPGC' else e['decoration_bounds']
   if expected_bounds:self.assertIn('DecorationBounds: '+', '.join(map(str,expected_bounds)),new['IsometricSelectable'])
   else:self.assertIn('DecorationOffset: '+','.join(map(str,e['decoration_offset'])),new['IsometricSelectable'])
   for key,value in e['effects'].items():
    if actor=='UPGC' and key in {'WithIdleOverlay@'+n for n in ('RADAR','SHIELD','SHIELD-ACTIVE','SHIELD-ACTIVE-EFFECT','DZONE')}:value=[0,0,0]
    self.assertIn('Offset: '+','.join(map(str,value)),new[key],(actor,key))
   if actor=='UPGC':
    # Later explicit visual fit/plug centering; full invariants in test_upgrade_center_presentation.py.
    self.assertIn('DecorationHeight: 152',new['IsometricSelectable'])
    new['IsometricSelectable']=new['IsometricSelectable'].replace('DecorationHeight: 152','DecorationHeight: 176')
    self.assertIn('Offset: 0,0',new['Pluggable']);new['Pluggable']=re.sub(r'\n\tOffset: 0,0','',new['Pluggable'])
    self.assertIn('Sequence: upgrade-shield-effect',new['WithIdleOverlay@SHIELD-ACTIVE-EFFECT'])
    new['WithIdleOverlay@SHIELD-ACTIVE-EFFECT']=new['WithIdleOverlay@SHIELD-ACTIVE-EFFECT'].replace('Sequence: upgrade-shield-effect','Sequence: emp-overlay')
   if actor in scrin and scrin[actor]['new_bib_cells']!=scrin[actor]['old_bib_cells']:
    n=scrin[actor]['new_bib_cells'];self.assertIn(f'Image: scrinbib{n}x{n}',new['WithIdleOverlay@SCRINBIB']);old.pop('WithIdleOverlay@SCRINBIB');new.pop('WithIdleOverlay@SCRINBIB')
   if actor=='FIX.Soviet':
    self.assertIn('Image: sovietbib4x3',new['WithIdleOverlay@SOVIETBIB'])
    old['WithIdleOverlay@SOVIETBIB']=old['WithIdleOverlay@SOVIETBIB'].replace('Image: sovietbib4x4','Image: sovietbib4x3')
   if actor=='WEAP.NOD':
    self.assertIn('Image: nodbib4x3',new['WithIdleOverlay@NODBIB'])
    old['WithIdleOverlay@NODBIB']=old['WithIdleOverlay@NODBIB'].replace('Image: nodbib3x3','Image: nodbib4x3')
   if actor=='GRAV':
    self.assertIn('BobDistance: -68',new['HoversAboveFoundation']);self.assertIn('RequiresCondition: !build-incomplete',new['HoversAboveFoundation']);new.pop('HoversAboveFoundation')
   if actor=='AFLD.GDI':
    self.assertIn('ExitCell: 2,1',new['Exit@1']);self.assertIn('SpawnOffset: 362,362,0',new['Exit@1'])
    for data in (old,new):data['Exit@1']=re.sub(r'\n\t(?:ExitCell|SpawnOffset): [^\n]+','',data['Exit@1'])
   if actor=='AFAC.Nod':
    self.assertIn('IntoActor: amcv.nod',new['Transforms'])
    old['Transforms']=re.sub(r'IntoActor: amcv(?=\n|$)','IntoActor: amcv.nod',old['Transforms'])
   for data in (old,new):
    normalize_followup(data)
    data['IsometricSelectable']=re.sub(r'\n\tDecoration(?:Bounds|Offset): [^\n]+','',data['IsometricSelectable'])
    for key in data:
     if key.startswith('WithIdleOverlay'):data[key]=re.sub(r'\n\tOffset: [^\n]+','',data[key])
   self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()},actor)
 def test_nod_bib_source_pixels_and_factory_only(self):
  meta=json.loads((OUT/'bib4x3-binding.json').read_text());a=np.array(Image.open(ROOT/meta['source']).convert('RGBA'));b=np.array(Image.open(ROOT/'mods/ca/bits/nod/buildings/bib4x3.png').convert('RGBA'));self.assertEqual(b.shape,(231,448,4));colors={tuple(p) for p in a.reshape(-1,4)};self.assertTrue({tuple(p) for p in b[b[:,:,3]>0]}<=colors)
  old=traits((OUT/'weap.nod-before.yaml').read_text());new=traits(resolved('--resolved-rules','WEAP.NOD'));self.assertIn('Image: nodbib4x3',new['WithIdleOverlay@NODBIB'])
  old.pop('WithIdleOverlay@NODBIB');new.pop('WithIdleOverlay@NODBIB')
  # The later explicit30px total right/down move is checked exactly by the placement test above.
  for data in (old,new):
   normalize_followup(data)
   data['IsometricSelectable']=re.sub(r'\n\tDecorationBounds: [^\n]+','',data['IsometricSelectable'])
   for key in data:
    if key.startswith('WithIdleOverlay'):data[key]=re.sub(r'\n\tOffset: [^\n]+','',data[key])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  for f,sha in json.loads((OUT/'protected-before.json').read_text()).items():self.assertEqual(hashlib.sha256((ROOT/f).read_bytes()).hexdigest(),sha,f)
 def test_fusion_name_and_soviet_barracks_shadow(self):
  old=traits((OUT/'apwr-before.yaml').read_text());new=traits(resolved('--resolved-rules','APWR'));self.assertIn('Name: Fusion Power Plant',new['Tooltip']);old.pop('Tooltip');new.pop('Tooltip');self.assertEqual(normalize_followup(old),normalize_followup(new))
  soviet=traits(resolved('--resolved-rules','APWR.Soviet'));self.assertIn('Cost: 1500',soviet['Valued']);self.assertIn('Amount: 800',soviet['Power']);self.assertNotIn('Fusion',soviet['Tooltip'])
  b=traits(resolved('--resolved-rules','BARR'));self.assertNotIn('WithIdleOverlay@SOVIETBIB',b);self.assertIn('ShadowColor: 00000070',b['WithShadow@BUILDINGFEEDBACK'])
if __name__=='__main__':unittest.main()
