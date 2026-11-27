"""Supplied Nod conyard alignment and disabled Gun Turret guard, with originals intact."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,json,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1]
class NodConyardAlignmentTests(unittest.TestCase):
 def test_protected_source_unchanged(self):
  # The old alignment manifest describes earlier supplied pixels. Preserve it;
  # use the frozen pre-feedback snapshot to guard the currently supplied original.
  data=json.loads((ROOT/'art-source/new-faction-buildings/china-placement-followup/second-adjustment/protected-before.json').read_text())
  data={k.replace('\\','/'):v for k,v in data.items()}
  file='mods/ca/bits/nod/buildings/conyard.png';source=ROOT/file
  self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),data[file])
 def test_supplied_png_and_bib_registered(self):
  actor=traits(resolved('--resolved-rules','AFAC.Nod'))
  self.assertIn('Image: nodfact.custom',actor['RenderSprites'])
  self.assertIn('Image: nodbib3x3',actor['WithIdleOverlay@NODBIB'])
  self.assertIn('Dimensions: 3,3',actor['Building']);self.assertIn('LocalCenterOffset: 0,158,0',actor['Building'])
  seq=traits(resolved('--resolved-sequences','nodfact.custom'))
  for name in ('idle','damaged-idle'):
   self.assertIn('Filename: bits/nod/buildings/conyard.png',seq[name])
   self.assertIn('Anchor: Bottom',seq[name]);self.assertIn('Offset: -22,-14',seq[name])
 def test_gun_turret_disabled_but_still_on_testmap(self):
  gun=traits(resolved('--resolved-rules','GUN.Nod'));laser=traits(resolved('--resolved-rules','LTUR'))
  self.assertIn('~disabled',gun['Buildable'])
  self.assertNotIn('~disabled',laser['Buildable'])
  with ZipFile(ROOT/'mods/ca/development-maps/testmap1.oramap') as z:text=z.read('map.yaml').decode('utf-8-sig')
  self.assertIn('Actor15: gun.nod',text)
if __name__=='__main__':unittest.main()
