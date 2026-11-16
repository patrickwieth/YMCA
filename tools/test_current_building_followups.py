"""Exact latest footprint, static-art, voxel integration and selection regressions."""
from pathlib import Path
from PIL import Image
import json,re,unittest,hashlib
import numpy as np
from test_soviet_building_art import resolved,traits
R=Path(__file__).resolve().parents[1]
def field(s,k):
 m=re.search(r'^\t'+re.escape(k)+r': ([^\n]+)',s,re.M);return m[1] if m else None
def drop(s,*keys):
 for k in keys:s=re.sub(r'\n\t'+re.escape(k)+r': [^\n]+','',s)
 return s.strip()
class CurrentBuildingFollowups(unittest.TestCase):
 def test_gdi_factory_solid_three_by_three(self):
  r=traits(resolved('--resolved-rules','WEAP.TD'));b=r['Building']
  self.assertEqual(field(b,'Footprint'),'xxx= xxx= xxx= ====')
  self.assertEqual(field(b,'Dimensions'),'4,4')
  self.assertEqual(field(b,'LocalCenterOffset'),'0,-1448,0')
  self.assertEqual(field(b,'FootprintOffset'),'-1,-1')
  exits=[v for k,v in r.items() if k.startswith('Exit')]
  self.assertTrue(any(field(v,'ExitCell')=='0,2' for v in exits))
 def test_china_factory_mask_and_propaganda_bib(self):
  r=traits(resolved('--resolved-rules','chweap'));self.assertEqual(field(r['Building'],'Footprint'),'xxxx x=xx ==== ====')
  r=traits(resolved('--resolved-rules','chpropaganda'));self.assertEqual(field(r['Building'],'Footprint'),'xxx= xxx= xxx= ====');self.assertEqual(field(r['Building'],'FootprintOffset'),'0,-1');self.assertEqual(field(r['Building'],'LocalCenterOffset'),'724,-1448,0');self.assertEqual(field(r['WithIdleOverlay@CHINABIB'],'Image'),'sovietbib4x4');self.assertEqual(field(r['WithIdleOverlay@CHINABIB'],'Offset'),'0,0,-724')
 def test_soviet_masks_and_repair_destination(self):
  expected={'DOME.Soviet':'xx= xxx xx=','MSLO':'xx xx','TTUR':'x','WEAP.Soviet':'xxxx xxxx ==== ====','FIX.Soviet':'xx= xx= +=='}
  for actor,mask in expected.items():
   r=traits(resolved('--resolved-rules',actor));self.assertEqual(field(r['Building'],'Footprint'),mask)
  r=traits(resolved('--resolved-rules','FIX.Soviet'));self.assertEqual(field(r['Building'],'Dimensions'),'3,3');self.assertEqual(field(r['Building'],'LocalCenterOffset'),'0,724,0');self.assertEqual(field(r['WithIdleOverlay@SOVIETBIB'],'Image'),'sovietbib3x3')
  self.assertEqual(field(r['WithIdleOverlay@SOVIETBIB'],'Offset'),'0,0,724')
  states=traits(resolved('--resolved-sequences','fix.soviet'))
  for state in ('idle','damaged-idle','active','damaged-active'):self.assertEqual(field(states[state],'Offset'),'26,-59')
  s=traits(resolved('--resolved-sequences','sovietbib4x3'));self.assertEqual(field(s['idle'],'Offset'),'32,-16')
  old=np.array(Image.open(R/'mods/ca/bits/soviet/buildings/bib4x4.png').convert('RGBA'));new=np.array(Image.open(R/'mods/ca/bits/soviet/buildings/bib4x3.png').convert('RGBA'));m=new[:,:,3]>0;self.assertEqual(new.shape,(224,448,4));self.assertTrue(np.array_equal(new[m],old[:224,64:512][m]))
 def test_china_selection_only(self):
  r=traits(resolved('--resolved-rules','chweap'));s=r['IsometricSelectable'];self.assertEqual(field(s,'DecorationFootprint'),'4,2');self.assertIsNone(field(s,'DecorationBounds'));self.assertEqual([int(v) for v in field(s,'Bounds').split(',')],[4344,4344,407,0]);self.assertEqual(field(s,'DecorationOffset'),'-441,362,0')
 def test_missile_files_and_nonvisual_rules(self):
  p=R/'art-source/gdi missile tower';old=traits((p/'integration/rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','ATWR'))
  self.assertEqual(field(new['RenderSprites'],'Image'),'atwr.missile.native');self.assertIn('WithVoxelTurret',new);self.assertNotIn('WithBuildingBib',new)
  for key in ('WithBuildingBib','QuantizeFacingsFromSequence','RenderVoxels','WithVoxelTurret'):old.pop(key,None);new.pop(key,None)
  for d in (old,new):
   for key in d:
    if key=='RenderSprites':d[key]=drop(d[key],'Image')
    elif key=='BodyOrientation':d[key]=drop(d[key],'QuantizedFacings','CameraPitch','UseClassicPerspectiveFudge')
    elif key=='IsometricSelectable':d[key]=drop(d[key],'DecorationBounds','DecorationHeight')
    elif key=='Turreted':d[key]=drop(d[key],'Offset')
    elif key.startswith('Armament'):d[key]=drop(d[key],'LocalOffset')
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  for a,b in [('base.png','missile-tower-base.png'),('missile-turret.vxl','missile-tower-turret.vxl'),('missile-turret.hva','missile-tower-turret.hva'),('turret-palette.png','missile-tower-palette.png')]:self.assertEqual((p/'pass-16'/a).read_bytes(),(R/'mods/ca/bits/gdi/buildings'/b).read_bytes())
  seq=traits(resolved('--resolved-sequences','atwr.missile.native'));self.assertEqual(field(seq['idle'],'Filename'),'bits/gdi/buildings/missile-tower-base.png');self.assertEqual(field(seq['idle'],'Offset'),'0,-16')
 def test_chemical_tower_cuts_and_static_binding(self):
  p=R/'art-source/soviet chem tower';meta=json.loads((p/'pass-03/cuts.json').read_text());self.assertEqual(hashlib.sha256((p/'chemtower.png').read_bytes()).hexdigest(),meta['source_sha256'])
  src=np.array(Image.open(p/'chemtower.png').convert('RGBA'));out=np.array(Image.open(p/'pass-06/chemical-tower.png').convert('RGBA'));opaque=src[:166,:,3]>0
  neighbours=sum(np.roll(opaque.astype(int),shift,axis) for shift,axis in ((1,0),(-1,0),(1,1),(-1,1)))
  preserved=opaque & (neighbours>=2)
  self.assertTrue(np.array_equal(src[:166][preserved],out[:166][preserved]))
  self.assertTrue(set(map(tuple,out[out[:,:,3]>0]))<=set(map(tuple,src[src[:,:,3]>0])))
  self.assertFalse(out[186:,:,3].any(), 'No repeated facade below the repaired footing')
  self.assertEqual((p/'pass-06/chemical-tower.png').read_bytes(),(R/'mods/ca/bits/soviet/buildings/chemical-tower.png').read_bytes())
  previous=np.array(Image.open(p/'pass-05/chemical-tower.png').convert('RGBA'))
  self.assertTrue(np.array_equal(previous[48:],out[48:]))
  self.assertEqual(field(traits(resolved('--resolved-rules','TTUR'))['Building'],'LocalCenterOffset'),'0,0,0')
  seq=traits(resolved('--resolved-sequences','ttur.chemical.native'))
  self.assertEqual(field(seq['idle'],'Filename'),'bits/soviet/buildings/chemical-tower.png')
  self.assertEqual(field(seq['idle'],'Offset'),'0,8')
  self.assertEqual(field(traits(resolved('--resolved-rules','TTUR'))['Turreted'],'Offset'),'0,0,1651')
  self.assertEqual(field(traits(resolved('--resolved-rules','TTUR'))['WithIdleOverlay@SOVIETBIB'],'Image'),'sovietbib1x1')
  old=traits((p/'integration/rules-before.yaml').read_text());new=traits(resolved('--resolved-rules','TTUR'));self.assertNotIn('WithSpriteTurret',new);self.assertNotIn('RenderVoxels',new);self.assertEqual(field(new['RenderSprites'],'Image'),'ttur.chemical.native')
  old.pop('WithSpriteTurret')
  # Explicit later request: reduce the old 2x2 bib to 1x1.
  old['WithIdleOverlay@SOVIETBIB']=drop(old['WithIdleOverlay@SOVIETBIB'],'Image')
  new['WithIdleOverlay@SOVIETBIB']=drop(new['WithIdleOverlay@SOVIETBIB'],'Image')
  for d in (old,new):
   d['RenderSprites']=drop(d['RenderSprites'],'Image');d['Turreted']=drop(d['Turreted'],'Offset');d['IsometricSelectable']=drop(d['IsometricSelectable'],'DecorationBounds','DecorationHeight')
   d['Building']=drop(d['Building'],'Dimensions','Footprint','LocalCenterOffset')
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
 def test_tesla_all_charge_levels_start_at_upper_sphere(self):
  r=traits(resolved('--resolved-rules','TSLA'))
  for key in ('Armament','Armament@1','Armament@2','Armament@3'):
   self.assertEqual(field(r[key],'LocalOffset'),'0,0,3270')
  # Final native sprite: 142x204, bottom anchor, placement (2,-3).
  # Sphere center y27 projects to -144.5 pixels; isometric TileScale=1448.
  self.assertLess(abs(3270*64/1448 - 144.5), 1)
if __name__=='__main__':unittest.main()
