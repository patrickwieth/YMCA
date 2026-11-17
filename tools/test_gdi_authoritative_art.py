"""Canonical supplied PNG bindings, power-plant refresh and gameplay isolation."""
from pathlib import Path
import hashlib,json,re,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];BEFORE=ROOT/'art-source/gdi-power-plant-refresh/before'
class AuthoritativeGdiArtTests(unittest.TestCase):
 def test_runtime_cannot_shadow_supplied_folder(self):
  manifest=(ROOT/'mods/ca/mod.yaml').read_text();self.assertNotIn('ca|sequences/gdi-building-colors.yaml',manifest)
  info=json.loads((ROOT/'art-source/gdi-color-match/binding.json').read_text())
  for image,states in info['sequence_images'].items():
   actual=traits(resolved('--resolved-sequences',image))
   for state,path in states.items():
    self.assertEqual(Path(path).parent.as_posix(),'bits/gdi/buildings');self.assertIn('Filename: '+path,actual[state]);self.assertNotIn('color-matched/',actual[state])
  generator=(ROOT/'tools/art/harmonize_gdi_yellow.py').read_text();self.assertNotIn("sequences/gdi-building-colors.yaml').write_text",generator)
 def test_both_original_power_pngs_bound_without_rescaling(self):
  for actor,image,file,digest in (
   ('NUKE','nuke.gdi.png','power plant.png','bddc2cd6ec016eb637fe31a4509f27d13d726f444e58c8994654754fed89b2d8'),
   ('NUK2','nuk2.gdi.png','advanced power plant.png','ddd8eb4eea527e16d0e059852eddfbe3946601c3466bc20e910e4ea0ed25717d')):
   self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/gdi/buildings'/file).read_bytes()).hexdigest(),digest)
   self.assertIn('Image: '+image,traits(resolved('--resolved-rules',actor))['RenderSprites'])
   sequence=traits(resolved('--resolved-sequences',image))
   for state in ('idle','damaged-idle','dead'):
    for value in ('Filename: bits/gdi/buildings/'+file,'Scale: 1','Start: 0','Length: 1'):self.assertIn(value,sequence[state])
 def test_power_and_nod_gameplay_unchanged(self):
  def gameplay(text):
   fields=traits(text)
   # The approved global construction sequence does not change production stats.
   if 'WithMakeAnimation' in fields:fields['WithMakeAnimation']=re.sub(r'\n[\t ]+Sequence: construction[\t ]*(?=\n|$)','',fields['WithMakeAnimation'])
   for key in list(fields):
    if key.startswith(('RenderSprites','WithIdleOverlay','WithBuildingBib')):del fields[key]
   fields['IsometricSelectable']=re.sub(r'(?m)^[\t ]+(?:DecorationBounds|DecorationHeight):[^\n]*\n?','',fields.get('IsometricSelectable',''))
   return {k:re.sub(r'\s+','',v) for k,v in fields.items()}
  for actor in ('NUKE','NUK2','NUKE.Nod','NUK2.Nod'):
   before=(BEFORE/(actor+'.yaml')).read_text(encoding='utf-8-sig');after=resolved('--resolved-rules',actor);self.assertEqual(gameplay(before),gameplay(after),actor)
   fields=traits(after)
   if actor.endswith('.Nod'):
    self.assertNotIn('WithIdleOverlay@GDIBIB',fields);self.assertIn('Image: nod',fields['RenderSprites']);self.assertNotIn('DecorationBounds:',fields['IsometricSelectable']);self.assertIn('DecorationHeight: 120',fields['IsometricSelectable'])
   else:self.assertIn('Image: gdibib3x3',fields['WithIdleOverlay@GDIBIB']);self.assertNotIn('WithBuildingBib',fields)
 def test_approved_ion_pixels_promoted_without_recoloring(self):
  for name in ('heavy-blaster-base.png','heavy-blaster-turret32.png'):
   art=ROOT/'mods/ca/bits/gdi/buildings';self.assertEqual((art/name).read_bytes(),(art/'color-matched'/name).read_bytes())
if __name__=='__main__':unittest.main()
