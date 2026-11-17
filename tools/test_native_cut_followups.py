"""Pixel provenance and original preservation for the requested platform/bib cuts."""
from pathlib import Path
import hashlib,json,unittest
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
class NativeCutFollowupTests(unittest.TestCase):
 def test_gatling_only_deletes_or_translates_approved_pixels(self):
  root=ROOT/'art-source/chinese gatling/platform-cut';old=np.array(Image.open(root/'pass-03/platform.png').convert('RGBA'));new=np.array(Image.open(root/'pass-04/platform.png').convert('RGBA'));xy=np.load(root/'pass-04/source-pixel-origins.npy');visible=new[:,:,3]>0
  self.assertTrue(np.array_equal(new[visible],old[xy[visible,1],xy[visible,0]]));self.assertEqual(np.where(visible)[1].max()-np.where(visible)[1].min()+1,120)
  y,x=np.indices(visible.shape);bearing=((x-64)/40)**2+((y-44)/24)**2<=1
  self.assertTrue(np.array_equal(old[bearing],new[bearing]))
  rgb=new[:,:,:3].astype(int);self.assertFalse(np.any(visible&(rgb[:,:,0]-rgb[:,:,2]>40)&(rgb[:,:,1]-rgb[:,:,2]>30)))
  # Historical cut remains archived; runtime now uses the subsequently supplied ring.
  binding=json.loads((ROOT/'art-source/chinese gatling/integration/binding.json').read_text())
  if not binding.get('user_supplied_ring'):
   runtime=np.array(Image.open(ROOT/'mods/ca/bits/china/buildings/gatling-base.png').convert('RGBA'));self.assertTrue(np.array_equal(runtime,new))
  meta=json.loads((root/'pass-04/binding.json').read_text());self.assertEqual(hashlib.sha256((root/'pass-03/platform.png').read_bytes()).hexdigest(),meta['previous_platform_sha256'])
 def test_scrin_four_by_four_native_source_colors_and_unchanged_existing_bibs(self):
  root=ROOT/'art-source/scrin bib';meta=json.loads((root/'bib4x4-cut-paste.json').read_text())
  for key,file in [('source_sha256',meta['source']),('cut_source_sha256',meta['cut_source'])]:self.assertEqual(hashlib.sha256((root/file).read_bytes()).hexdigest(),meta[key])
  source=np.array(Image.open(root/meta['source']).convert('RGBA'));colors={tuple(p) for p in source.reshape(-1,4)}
  with Image.open(root/'bib4x4.png') as im:self.assertEqual(im.size,(512,282));a=np.array(im.convert('RGBA'))
  self.assertTrue({tuple(p) for p in a[a[:,:,3]>0]}<=colors);self.assertEqual(meta['top_surface_center'],[256,131]);self.assertFalse(meta['runtime_installed'])
  for n in (1,2,3):
   art=Image.open(root/f'bib{n}x{n}.png').convert('RGBA');runtime=Image.open(ROOT/f'mods/ca/bits/scrin/buildings/bib{n}x{n}.png').convert('RGBA');self.assertEqual(art.tobytes(),runtime.tobytes())
if __name__=='__main__':unittest.main()
