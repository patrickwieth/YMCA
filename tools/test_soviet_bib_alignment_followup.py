"""Targeted conyard/power alignment; unrelated replaced Soviet art stays out of scope."""
from pathlib import Path
import hashlib,json,unittest
from calibrate_soviet_building_alignment import propose_soviet
from calibrate_gdi_building_alignment import ROOT
from test_soviet_building_art import resolved,traits,gameplay_hash
from audit_allied_selection_shapes import measure
from test_gdi_building_alignment import polygon
OUT=ROOT/'art-source/soviet-bib-alignment'
CONFIG={e['Actor']:e for e in json.loads((ROOT/'tools/soviet-building-alignment.json').read_text())['entries']}
class SovietBibAlignmentFollowup(unittest.TestCase):
 def test_reviewed_ground_center_selection_and_reproducibility(self):
  geometry=json.loads((OUT/'geometry-after.json').read_text())
  for g in geometry[:2]:
   e=CONFIG[g['Actor']];self.assertEqual(json.loads(json.dumps(propose_soviet(g))),e)
   w,h=e['SourceSize'];ox,oy=e['Offset'];cx,cy=e['SourceGroundCenter']
   self.assertLessEqual(abs(cx+ox-w/2),.5);self.assertLessEqual(abs(cy+oy-h+w/4),.5)
   self.assertEqual(g['DecorationBounds'],e['DecorationBounds']);self.assertEqual(g['SelectionPolygon'],polygon(e)[1])
   self.assertEqual(measure(g)['outside'],0)
 def test_runtime_inheritance_and_nonvisual_gameplay_unchanged(self):
  for actor,offset,bib in [('FACT.Soviet','-1,-9',4),('FACT.Nukular.Soviet','-1,-9',4),('POWR.Soviet','0,-12',3)]:
   text=resolved('--resolved-rules',actor);self.assertEqual(gameplay_hash(text),gameplay_hash((OUT/(actor+'.rules-before.yaml')).read_text()))
   fields=traits(text);self.assertIn(f'Image: sovietbib{bib}x{bib}',fields['WithIdleOverlay@SOVIETBIB'])
   image='powr.soviet' if actor=='POWR.Soviet' else 'fact.soviet';seq=traits(resolved('--resolved-sequences',image))
   for state in ('idle','damaged-idle'):self.assertIn('Offset: '+offset,seq[state]);self.assertIn('Anchor: Bottom',seq[state]);self.assertIn('Scale: 1',seq[state])
 def test_sources_bibs_and_protected_assets_not_modified(self):
  for name,sha in json.loads((OUT/'protected-before.json').read_text()).items():self.assertEqual(hashlib.sha256((ROOT/name).read_bytes()).hexdigest(),sha)
 def test_only_requested_calibrations_changed(self):
  old=json.loads((OUT/'before-soviet-building-alignment.json').read_text())['entries']
  for e in old:
   if e['Actor'] not in ('fact.soviet','powr.soviet'):self.assertEqual(e,CONFIG[e['Actor']])
  for name,directory in [('soviet-building-placement.yaml','sequences'),('building-alignment.yaml','rules/soviet')]:
   before=traits((OUT/('before-'+name)).read_text());after=traits((ROOT/'mods/ca'/directory/name).read_text())
   for actor,block in before.items():
    if actor.lower() not in ('fact.soviet','powr.soviet'):self.assertEqual(block,after[actor])
if __name__=='__main__':unittest.main()
