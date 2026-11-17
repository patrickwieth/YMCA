"""China construction bindings: size selection and coverage of engine inventory."""
from pathlib import Path
import re,unittest
ROOT=Path(__file__).resolve().parents[1]
class ConstructionTests(unittest.TestCase):
 def test_size_specific_sequences_cover_resolved_buildings(self):
  text=(ROOT/'mods/ca/sequences/building-construction.yaml').read_text()
  blocks={b.splitlines()[0].rstrip(':'):b for b in re.split(r'(?=^[^\s#][^\n]*:\s*$)',text,flags=re.M) if b.strip() and not b.startswith('#')}
  rows=(ROOT/'docs/modular/building-construction/engine-check.tsv').read_text().splitlines()
  self.assertGreater(len(rows),100)
  for row in rows:
   actor,image,x,y,sequence=row.split('\t');area=int(x)*int(y);size='small' if area<=2 else 'medium' if area<=9 else 'big';stages={'small':2,'medium':3,'big':4}[size]
   self.assertEqual(sequence,'construction',actor);block=blocks[image.lower()]
   self.assertIn('\tconstruction:',block)
   for i in range(1,stages+1):self.assertIn(f'Filename: make-{size}{i}.png',block)
   self.assertIn(f'\t\tLength: {stages*5}\n',block)
   self.assertIn('\t\tScale: 1\n',block)
 def test_building_only_default_and_manifest(self):
  rules=(ROOT/'mods/ca/rules/defaults.yaml').read_text()
  self.assertIn('WithMakeAnimation:\n\t\tSequence: construction\n\t\tCondition: build-incomplete',rules)
  self.assertIn('WithMakeAnimation:\n\tTerrainLightSource:',rules) # Blossom trees retain their own make.
  self.assertIn('ca|sequences/building-construction.yaml',(ROOT/'mods/ca/mod.yaml').read_text())
if __name__=='__main__':unittest.main()
