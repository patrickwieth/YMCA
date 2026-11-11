"""Native Soviet registration: source preservation, rear contacts and visual envelopes."""
import hashlib,json,unittest,re
from PIL import Image
from calibrate_gdi_building_alignment import ROOT
from calibrate_soviet_building_alignment import ACTOR_NAMES,propose_soviet
from test_gdi_building_alignment import polygon
from audit_allied_selection_shapes import measure
from test_supplied_building_art import block
CONFIG=json.loads((ROOT/'tools/soviet-building-alignment.json').read_text())['entries']

class SovietBuildingAlignmentTests(unittest.TestCase):
 def test_native_sources_and_contact_pixels(self):
  self.assertEqual(len(CONFIG),18)
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    images=[]
    for f in [{'File':e['File'],'SHA256':e['SourceSHA256']}]+e.get('AdditionalSourceImages',[]):
     path=ROOT/'mods/ca'/f['File']
     self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),f['SHA256'])
     with Image.open(path) as im:
      self.assertEqual(list(im.size),e['SourceSize']);images.append(im.convert('RGBA'))
    for x,y in e['SourceGroundContacts']:self.assertGreaterEqual(max(im.getpixel((x,y))[3] for im in images),32)

 def test_rear_contacts_and_frame_stay_on_original_bib(self):
  for e in CONFIG:
   with self.subTest(actor=e['Actor']):
    nx,ny=e['BibCells'];w,h=e['SourceSize'];ox,oy=e['Offset'];x0=ox-w/2;y0=oy-h+w/4
    uv=[(x+x0+2*(y+y0),-(x+x0)+2*(y+y0)) for x,y in e['SourceGroundContacts']]
    if e['Actor']=='tsla':
     self.assertEqual(e['Bib'],'sovietbib1x1');self.assertEqual((nx,ny),(1,1))
     self.assertLessEqual(abs(sum(x+x0 for x,y in e['SourceGroundContacts'])/2),.5)
     self.assertLessEqual(abs(sum(y+y0 for x,y in e['SourceGroundContacts'])/2),.5)
    else:
     self.assertTrue(2<=min(u for u,v in uv)+nx*64<=6)
     self.assertTrue(2<=min(v for u,v in uv)+ny*64<=6)
    tolerance=2 if e['Actor']=='tsla' else 1
    for x,y in e['SourceGroundContacts']+e['FrontContactSamples']:
     self.assertLessEqual(abs(x+x0+2*(y+y0)),nx*64+tolerance)
     self.assertLessEqual(abs(-(x+x0)+2*(y+y0)),ny*64+tolerance)
    if e['Actor']!='tsla':
     for x,y in polygon(e)[0]:
      self.assertLessEqual(abs(x+2*y),nx*64+2);self.assertLessEqual(abs(-x+2*y),ny*64+2)
    else:self.assertTrue(e['SelectionMayOverhangBib'])

 def test_full_silhouette_and_all_missile_frames_inside_selection(self):
  for e in CONFIG:
   files=[e['File']] if e['Actor']!='mslo' else [f'bits/soviet/buildings/missile silo {i}.png' for i in range(7)]
   for f in files:
    with self.subTest(actor=e['Actor'],file=f):
     result=measure({'File':f,'Scale':'1','Anchor':'Bottom','Offset':','.join(map(str,e['Offset'])),'SelectionPolygon':polygon(e)[1]})
     self.assertEqual(result['outside'],0)

 def test_generated_overrides_only_change_visual_fields(self):
  rules=(ROOT/'mods/ca/rules/soviet/building-alignment.yaml').read_text()
  sequences=(ROOT/'mods/ca/sequences/soviet-building-placement.yaml').read_text()
  source=(ROOT/'mods/ca/sequences/soviet-building-art.yaml').read_text()
  for e in CONFIG:
   text=block(rules,ACTOR_NAMES[e['Actor']])
   self.assertIn('DecorationBounds: '+', '.join(map(str,e['DecorationBounds'])),text)
   self.assertIn('DecorationHeight: '+str(e['DecorationHeight']),text)
   self.assertEqual(set(re.findall(r'^\t([^\t:]+):',text,re.M)),{'IsometricSelectable'})
   states=re.findall(r'^\t([^\t:]+):',block(source,e['Image']),re.M)
   placement=block(sequences,e['Image'])
   self.assertEqual(states,re.findall(r'^\t([^\t:]+):',placement,re.M))
   self.assertEqual(re.findall(r'^\t\t([^:]+):',placement,re.M),['Offset']*len(states))
   self.assertEqual(placement.count('Offset: '+','.join(map(str,e['Offset']))),len(states))

 def test_calibration_reproducible_and_export_matches(self):
  geometry=json.loads((ROOT/'docs/modular/soviet-building-alignment-geometry.json').read_text())
  regenerated=json.loads(json.dumps([propose_soviet(e) for e in geometry]))
  self.assertEqual(regenerated,CONFIG)
  for e,c in zip(geometry,CONFIG):
   self.assertEqual(e['DecorationBounds'],c['DecorationBounds']);self.assertEqual(e['Height'],c['DecorationHeight'])
   self.assertEqual(e['Offset'],','.join(map(str,c['Offset'])))
   self.assertEqual(e['GroundCorners'],polygon(c)[0]);self.assertEqual(e['SelectionPolygon'],polygon(c)[1])

if __name__=='__main__':unittest.main()
