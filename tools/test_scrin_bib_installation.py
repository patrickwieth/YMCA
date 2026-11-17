"""Installed Scrin plating: native pixels, resolved inheritance and gameplay isolation."""
from pathlib import Path
import json,hashlib,unittest
from test_soviet_building_art import resolved,traits
from building_presentation_test_helpers import normalize_building_presentation
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/scrin bib';OUT=ART/'integration';B=json.loads((OUT/'binding.json').read_text());OLD=json.loads((OUT/'rules-before.json').read_text())
class ScrinBibInstallation(unittest.TestCase):
 def test_promoted_native_cuts_and_protected_files(self):
  for name,sha in B['hashes'].items():
   for p in [ART/name,ROOT/'mods/ca/bits/scrin/buildings'/name]:self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),sha)
  for f,sha in json.loads((OUT/'protected-before.json').read_text()).items():self.assertEqual(hashlib.sha256((ROOT/f).read_bytes()).hexdigest(),sha,f)
 def test_all_actors_and_variants_have_only_decorative_changes(self):
  for actor,b in B['actors'].items():
   with self.subTest(actor=actor):
    old=traits(OLD[actor]);new=traits(resolved('--resolved-rules',actor));self.assertNotIn('WithBuildingBib',new)
    bibs=[v for k,v in new.items() if k.startswith('WithIdleOverlay@') and 'Image: scrinbib' in v];self.assertEqual(len(bibs),1)
    for value in ['Image: '+b['image'],'Palette: effect','RequiresCondition: !build-incomplete','IsDecoration: true']:self.assertIn(value,bibs[0])
    old.pop('WithBuildingBib',None);new.pop('WithIdleOverlay@SCRINBIB');normalize_building_presentation(old);normalize_building_presentation(new);self.assertEqual({k:v.strip() for k,v in old.items()},{k:v.strip() for k,v in new.items()})
 def test_native_surface_anchor_and_depth(self):
  for n in (1,2,3):
   s=traits(resolved('--resolved-sequences',f'scrinbib{n}x{n}'))['idle']
   for value in [f'Filename: bits/scrin/buildings/bib{n}x{n}.png','Scale: 1','Anchor: Bottom','Offset: 0,23','ZOffset: -4096','Length: 1']:self.assertIn(value,s)
if __name__=='__main__':unittest.main()
