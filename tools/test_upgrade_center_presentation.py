"""Resolved selection/plug registration; no gameplay or source-art changes."""
from pathlib import Path
import re, unittest
from test_soviet_building_art import resolved, traits
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'art-source/upgrade-center-selection'
OVERLAYS=('RADAR','SHIELD','SHIELD-ACTIVE','SHIELD-ACTIVE-EFFECT','DZONE')
def field(text,key):
 m=re.search(r'^\t'+re.escape(key)+r': ([^\n]+)',text,re.M)
 return m[1] if m else None
class UpgradeCenterPresentationTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.rules=traits(resolved('--resolved-rules','UPGC'))
  cls.sequences=traits(resolved('--resolved-sequences','upgc.gdi.png'))
 def test_decoration_and_middle_socket(self):
  self.assertEqual(field(self.rules['IsometricSelectable'],'DecorationBounds'),'2896, 2896, 11, -453')
  self.assertEqual(field(self.rules['IsometricSelectable'],'DecorationHeight'),'152')
  b=self.rules['Building'];self.assertEqual(field(b,'Dimensions'),'3,3');self.assertEqual(field(b,'FootprintOffset'),'-1,-1')
  self.assertEqual(field(self.rules['Pluggable'],'Offset'),'0,0')
 def test_native_plug_platform_centers(self):
  for key in OVERLAYS:self.assertEqual(field(self.rules['WithIdleOverlay@'+key],'Offset'),'0,0,0')
  for seq in ('turret-radar','turret-shield','turret-shield-active','turret-drop'):
   self.assertEqual(field(self.sequences[seq],'Offset'),'0,-48')
   # Source platform52,120 in padded104x144; all frames retain the same base.
   self.assertEqual((52-104/2,120-144/2-48),(0,0))
  # Actual BodyOrientationInfo default40-degree fixed-point transform, NOT a guessed screen shift.
  source=(ROOT/'engine/OpenRA.Game/WAngle.cs').read_text()
  table=list(map(int,re.findall(r'\d+',re.search(r'CosineTable\s*=.*?\{(.*?)\}',source,re.S)[1])))
  sine=table[256-40*1024//360];y=sine*768//1024
  self.assertEqual(field(self.rules['Turreted'],'Offset'),'-768,468,0')
  dx,dy=map(float,field(self.sequences['turret-rocket'],'Offset').split(','))
  self.assertAlmostEqual(44-96/2+dx+468*128/1448,0,places=5)
  self.assertAlmostEqual(74-96/2+dy+y*64/1448,0,places=5)
  self.assertEqual(field(self.rules['WithIdleOverlay@SHIELD-ACTIVE-EFFECT'],'Sequence'),'upgrade-shield-effect')
 def test_only_authorized_presentation_fields_changed(self):
  old=traits((OUT/'rules-before.yaml').read_text());new=dict(self.rules)
  for k,fields in {'IsometricSelectable':('DecorationBounds','DecorationHeight'),'Pluggable':('Offset',),**{'WithIdleOverlay@'+n:('Offset','Sequence') if n=='SHIELD-ACTIVE-EFFECT' else ('Offset',) for n in OVERLAYS}}.items():
   for data in (old,new):
    for f in fields:data[k]=re.sub(r'\n\t'+f+r': [^\n]+','',data[k])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
  old=traits((OUT/'sequences-before.yaml').read_text());new=dict(self.sequences)
  new.pop('upgrade-shield-effect')
  for k in ('turret-radar','turret-shield','turret-shield-active','turret-drop','turret-rocket'):
   for data in (old,new):data[k]=re.sub(r'\n\tOffset: [^\n]+','',data[k])
  self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
if __name__=='__main__':unittest.main()
