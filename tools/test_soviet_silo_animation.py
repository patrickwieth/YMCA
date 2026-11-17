"""Regression: multi-file silo animation must load sprites, not only resolve YAML.
Set YMCA_UTILITY to the current isolated utility if engine/bin is locked by a game.
"""
import unittest
from test_soviet_building_art import resolved, traits
from test_current_building_followups import field

class SovietSiloAnimation(unittest.TestCase):
 def test_pattern_uses_native_reverse_animation(self):
  states=traits(resolved('--resolved-sequences','mslo.soviet'))
  for name in ('active','damaged-active'):
   self.assertEqual(field(states[name],'Length'),'7')
   self.assertEqual(field(states[name],'Reverses'),'true')
   self.assertIsNone(field(states[name],'Frames'))
  for name in ('idle','damaged-idle'):
   self.assertEqual(field(states[name],'Filename'),'bits/soviet/buildings/missile silo 0.png')
 def test_upper_two_by_two_collision(self):
  b=traits(resolved('--resolved-rules','MSLO'))['Building']
  self.assertEqual(field(b,'Dimensions'),'2,2')
  self.assertEqual(field(b,'Footprint'),'xx xx')
  self.assertEqual(field(b,'FootprintOffset'),'0,-1')
  self.assertEqual(field(b,'LocalCenterOffset'),'724,0,0')
 def test_actual_sprite_loading(self):
  output=resolved('--check-placement-passability','--all-sprites')
  self.assertIn('Full default sequence set loaded',output)

if __name__=='__main__':unittest.main()
