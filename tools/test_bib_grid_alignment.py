"""Actual resolved grid centers, co-translated attachments and bunker mine-cell registration."""
from pathlib import Path
import json,subprocess,os,tempfile,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/bib-grid-alignment'
class BibGridAlignmentTests(unittest.TestCase):
 def test_actual_grid_and_preserved_local_registration(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
  raw=subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--export-allied-selection-geometry','--all-bibs'],cwd=ROOT,env=env,text=True)
  current={e['Actor']:e for e in json.loads(raw)};before={e['Actor']:e for e in json.loads((OUT/'resolved-before.json').read_text())};plan=json.loads((OUT/'applied.json').read_text())
  with tempfile.TemporaryDirectory() as temp:
   p=Path(temp);(p/'geometry.json').write_text(raw)
   subprocess.run(['python',str(ROOT/'tools/audit_bib_grid_alignment.py'),str(p/'geometry.json'),str(p/'audit.json')],check=True)
   for e in json.loads((p/'audit.json').read_text()):self.assertTrue(e.get('already_aligned'),e)
  for e in plan:
   a=current[e['actor']];b=before[e['actor']];self.assertEqual(a['BuildingCenterOffset'],e['new_center_offset']);self.assertEqual(a['DecorationOffset'],e['decoration_offset'])
   self.assertEqual(a['Bibs'],b['Bibs']);self.assertEqual(a['BuildingDimensions'],b['BuildingDimensions'])
   if e['actor'] in {'chgatling','chpower','chpower2','chweap'}:continue # Subsequent platform/relative-placement requests; separately tested.
   self.assertEqual(a['GameplayAndAttachmentsHash'],e['preserved_non_anchor_rule_hash'],e['actor'])
   for field in ['Offset','Scale','File']:self.assertEqual(a[field],b[field])
   self.assertLessEqual(max(abs(x-y) for p,q in zip(a['SelectionPolygon'],b['SelectionPolygon']) for x,y in zip(p,q)),1)
 def test_bunker_body_and_selection_share_mine_grid_center(self):
  for actor in ['chbunker','chbunker2','chbunker.advinf','chbunker2.advinf','chbunker.ai']:
   t=traits(resolved('--resolved-rules',actor));self.assertIn('Dimensions: 2,2',t['Building']);self.assertIn('Footprint: xx xx',t['Building']);self.assertIn('LocalCenterOffset: 0,0,0',t['Building']);self.assertNotIn('WithIdleOverlay@CHINABIB',t)
   self.assertIn('Locations: 0,-1, 1,-1, -1,0, -1,1, 2,0, 2,1, 0,2, 1,2',t['SpawnNeighboringActors@minefield'])
   self.assertIn('Bounds: 2896, 2896',t['IsometricSelectable'])
if __name__=='__main__':unittest.main()
