"""Center helipad visuals on the unchanged foundation without changing landing."""
from pathlib import Path
import unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1]
class HelipadPlacementTests(unittest.TestCase):
 def test_only_selection_decoration_moves_in_rules(self):
  before=traits((ROOT/'art-source/gdi-helipad/resolved-before.yaml').read_text(encoding='utf-8-sig'));after=traits(resolved('--resolved-rules','HPAD.TD'))
  self.assertEqual({k:v.strip() for k,v in before.items() if k!='IsometricSelectable'},{k:v.strip() for k,v in after.items() if k!='IsometricSelectable'})
  self.assertEqual(before['IsometricSelectable'].replace('2353, 2353, -12, -973','2353, 2353, -12, 0').strip(),after['IsometricSelectable'].strip())

 def test_only_five_body_offsets_change(self):
  before=traits((ROOT/'art-source/gdi-helipad/sequences-before.yaml').read_text(encoding='utf-8-sig'));after=traits(resolved('--resolved-sequences','hpad2.gdi.png'));states={'idle','damaged-idle','active','damaged-active','dead'}
  self.assertEqual(set(before),set(after))
  for k in before:self.assertEqual((before[k].replace('Offset: -1,-68','Offset: -1,-25') if k in states else before[k]).strip(),after[k].strip())
if __name__=='__main__':unittest.main()
