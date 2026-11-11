"""Native GDI art/bib/selection registration tests; never edits source artwork."""
import hashlib,json,unittest
from pathlib import Path
from PIL import Image
from audit_allied_selection_shapes import measure
from test_supplied_building_art import block
ROOT=Path(__file__).resolve().parents[1]
CONFIG=json.loads((ROOT/'tools/gdi-building-alignment.json').read_text())['entries']

def polygon(entry):
 w,d,ox,oy=entry['DecorationBounds'];scale=1448;tx,ty=128,64
 x,y=int(ox*tx/scale),int(oy*ty/scale)
 left=[x-int(w*tx/(2*scale)),y];right=[left[0]+int(w*tx/scale),y]
 top=[x,y-int(d*ty/(2*scale))];bottom=[x,top[1]+int(d*ty/scale)]
 h=entry['DecorationHeight']
 return [top,left,bottom,right],[[top[0],top[1]-h],[left[0],left[1]-h],left,bottom,right,[right[0],right[1]-h]]

class GdiBuildingAlignmentTests(unittest.TestCase):
 def test_all_thirteen_source_images_and_native_dimensions(self):
  self.assertEqual(len(CONFIG),13)
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    path=ROOT/'mods/ca'/e['File']
    self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),e['SourceSHA256'],
                     'Source art changed: review its registration, do not rewrite the PNG to satisfy this test')
    with Image.open(path) as im:self.assertEqual(list(im.size),e['SourceSize'])

 def test_rear_ground_contacts_and_selection_stay_on_bib(self):
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    nx,ny=e.get('BibCells',e.get('GroundEnvelopeCells'));w,h=e['SourceSize'];ox,oy=e['Offset']
    x0=ox-w/2;y0=oy-h+w/4
    uv=[(x+x0+2*(y+y0),-(x+x0)+2*(y+y0)) for x,y in e['SourceGroundContacts']]
    if e.get('PlacementPolicy')=='centered-visual-ground-envelope':
     self.assertEqual(e['Actor'],'hpad.td');self.assertEqual(e['DecorationBounds'][3],0)
     self.assertGreaterEqual(min(u for u,v in uv)+nx*64,2);self.assertGreaterEqual(min(v for u,v in uv)+ny*64,2)
    else:
     self.assertTrue(2<=min(u for u,v in uv)+nx*64<=6)
     self.assertTrue(2<=min(v for u,v in uv)+ny*64<=6)
    for x,y in e['SourceGroundContacts']+e['FrontContactSamples']:
     u=x+x0+2*(y+y0);v=-(x+x0)+2*(y+y0)
     self.assertLessEqual(abs(u),nx*64+1);self.assertLessEqual(abs(v),ny*64+1)
    for x,y in polygon(e)[0]:
     self.assertLessEqual(abs(x+2*y),nx*64+2)
     self.assertLessEqual(abs(-x+2*y),ny*64+2)

 def test_rectangular_airfield_bib_is_unscaled_source_composition(self):
  art=ROOT/'mods/ca/bits/gdi/buildings'
  with Image.open(art/'bib4x4.png') as original:
   original=original.convert('RGBA');composite=Image.new('RGBA',(576,288))
   composite.alpha_composite(original,(64,0));composite.alpha_composite(original,(0,32))
  with Image.open(art/'bib4x5.png') as actual:self.assertEqual(actual.convert('RGBA').tobytes(),composite.tobytes())

 def test_selection_contains_source_art_with_rounding_tolerance(self):
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    ground,selection=polygon(e)
    result=measure({'File':e['File'],'Scale':'1','Anchor':'Bottom',
                    'Offset':','.join(map(str,e['Offset'])),'SelectionPolygon':selection})
    self.assertEqual(result['outside'],0)
    self.assertEqual(result['left_over'],0)
    self.assertEqual(result['right_over'],0)

 def test_rules_match_calibration_without_gameplay_or_mouse_changes(self):
  rules=(ROOT/'mods/ca/rules/gdi/building-art.yaml').read_text()
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    text=block(rules,'SYRD.gdi' if e['Actor']=='syrd.gdi' else e['Actor'].upper())
    self.assertIn('DecorationBounds: '+', '.join(map(str,e['DecorationBounds'])),text)
    self.assertIn('DecorationHeight: '+str(e['DecorationHeight']),text)
    for key in ('Bounds','Height','Building','HitShape','Exit','Power','Valued'):
     self.assertNotRegex(text,r'(?m)^\t{1,2}'+key+r'(?:@[^:]+)?:')

 def test_nod_and_colorpicker_do_not_inherit_gdi_decoration_bounds(self):
  rules=(ROOT/'mods/ca/rules/gdi/building-art.yaml').read_text()
  for actor in ('NUKE.Nod','HQ.Nod','REP.Nod','HPAD.TD.Nod','PROC.TD.Nod','fact.colorpicker'):
   self.assertIn('-DecorationBounds:',block(rules,actor))
  self.assertIn('DecorationHeight: 30',block(rules,'fact.colorpicker'))
  # AFAC.Nod retains its own explicit 3x3 Nod decoration, not a null override.
  self.assertNotIn('-DecorationBounds:',block(rules,'AFAC.Nod'))

if __name__=='__main__':unittest.main()
