"""Shared GDI yellow: source integrity, palette, scoped pixels and actual bindings."""
from pathlib import Path
from PIL import Image
import hashlib,json,numpy as np,unittest
from test_soviet_building_art import resolved,traits
ROOT=Path(__file__).resolve().parents[1];REPORT=ROOT/'art-source/gdi-color-match';INFO=json.loads((REPORT/'binding.json').read_text())
class GdiColorTests(unittest.TestCase):
 def test_reference_files_unchanged_and_no_reference_overrides(self):
  for name,data in INFO['references'].items():
   path=ROOT/'mods/ca/bits/gdi/buildings'/name;self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),data['sha256'])
   self.assertFalse(any(e['source'].endswith('/'+name) for e in INFO['entries']))
  refs=set()
  for name in INFO['references']:
   with Image.open(ROOT/'mods/ca/bits/gdi/buildings'/name) as im:refs|={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
  self.assertLessEqual({tuple(c) for c in INFO['target_palette']},refs)

 def test_only_masked_rgb_changes_and_metadata_exact(self):
  palette={tuple(c) for c in INFO['target_palette']}
  for e in INFO['entries']:
   source=ROOT/'mods/ca'/e['source'];target=ROOT/'mods/ca'/e['output']
   self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),e['source_sha256']);self.assertEqual(hashlib.sha256(target.read_bytes()).hexdigest(),e['output_sha256'])
   with Image.open(source) as im:a=np.array(im.convert('RGBA'));metadata=im.text.copy();size=im.size
   with Image.open(target) as im:b=np.array(im.convert('RGBA'));self.assertEqual(im.size,size);self.assertEqual(im.text,metadata)
   self.assertTrue(np.array_equal(a[:,:,3],b[:,:,3]));changed=np.any(a!=b,axis=2)
   with Image.open(REPORT/(source.stem+'-mask.png')) as im:self.assertTrue(np.array_equal(changed,np.array(im)>0))
   with Image.open(REPORT/(source.stem+'-protected.png')) as im:protected=np.array(im)>0
   self.assertFalse(np.any(changed&protected));self.assertLessEqual({tuple(c) for c in b[:,:,:3][changed]},palette)
   # All cool light / steel / black contour pixels stay byte-identical.
   allowed={tuple(m['from']) for m in e['mapping']}
   self.assertLessEqual({tuple(c) for c in a[:,:,:3][changed]},allowed)
   self.assertEqual(int(changed.sum()),e['changed_pixels'])

 def test_override_changes_filenames_only_and_resolves(self):
  text=(ROOT/'mods/ca/sequences/gdi-building-colors.yaml').read_text()
  for line in text.splitlines():
   if line.startswith('\t\t'):self.assertTrue(line.startswith('\t\tFilename: '))
  for image,states in INFO['sequence_images'].items():
   actual=traits(resolved('--resolved-sequences',image))
   for state,path in states.items():self.assertIn('Filename: '+path,actual[state])
  for image,name in (('awep.gdi.png','weapons factory.png'),('eye.gdi.png','communication center.png')):
   self.assertIn('Filename: bits/gdi/buildings/'+name,traits(resolved('--resolved-sequences',image))['idle'])

if __name__=='__main__':unittest.main()
