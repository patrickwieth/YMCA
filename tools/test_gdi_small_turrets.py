"""Actual AA/Sonic study exports and saved geometry controls; no gameplay binding."""
from pathlib import Path
from PIL import Image
import json,hashlib,os,subprocess,unittest,numpy as np
ROOT=Path(__file__).resolve().parents[1]
STUDIES=(('aa','gdi aa gun',192,'3c07334edab877fb11a06f425ce1a8459d06477e72facecb2956cda7c55449b7',4),('sonic','gdi sonic turret',256,'98e7adabaa762627d1cb08d3c8d35fec818bf9835207b80596019e5a58483134',12))
class GdiSmallTurretStudiesTests(unittest.TestCase):
 def test_originals_and_real_saved_geometry(self):
  for kind,folder,size,digest,start in STUDIES:
   path=ROOT/'art-source'/folder;source=next(path.glob('*.png'));self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),digest)
   check=json.loads((path/'pass-05/geometry-check.json').read_text());self.assertEqual(hashlib.sha256((path/'pass-05'/(kind+'-source-model.blend')).read_bytes()).hexdigest(),check['blend_sha256'])
   self.assertTrue(check['parts_parented_separately']);self.assertTrue(check['fixed_matrices_unchanged']);self.assertTrue(check['all_meshes_uv_mapped']);self.assertGreater(check['vertices'],1000);self.assertGreater(check['fixed_meshes'],1);self.assertGreater(check['rotating_meshes'],15);self.assertLess(check['fixed_xy_extent'],check['one_cell_xy_limit'])
   for frame in check['facings']:self.assertLess(frame['yaw_error'],1e-6)

 def test_native_frames_palette_alpha_and_sheet_order(self):
  for kind,folder,size,digest,start in STUDIES:
   path=ROOT/'art-source'/folder;study=path/'pass-05';review=study/'review';report=json.loads((review/'validation.json').read_text());self.assertEqual(report['directions'],32);self.assertEqual(report['clipped_frames'],0);self.assertFalse(report['game_installed']);self.assertEqual(report['scale'],1)
   with Image.open(next(path.glob('*.png'))) as im:colors={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
   with Image.open(review/'turret32.png') as sheet:
    self.assertEqual(sheet.size,(size*8,size*4));self.assertEqual(sheet.text['FrameSize'],f'{size},{size}');self.assertEqual(sheet.text['FrameAmount'],'32')
    for i in range(32):
     idx=(start+i)%32
     with Image.open(study/f'turret-{idx:03d}-raw.png') as raw:self.assertEqual(raw.size,(size,size))
     with Image.open(review/f'turret-{idx:03d}.png') as im:
      a=np.array(im.convert('RGBA'));self.assertLessEqual(set(np.unique(a[:,:,3])),{0,255});self.assertLessEqual({tuple(c) for c in a[:,:,:3][a[:,:,3]>0]},colors);bounds=im.getbbox();self.assertGreater(min(bounds[:2]),0);self.assertLess(max(bounds[2:]),size)
      self.assertEqual(sheet.crop(((i%8)*size,(i//8)*size,(i%8+1)*size,(i//8+1)*size)).tobytes(),im.tobytes())
   with Image.open(review/'base.png') as im:self.assertEqual(im.text['FrameAmount'],'1');self.assertEqual(im.text['FrameSize'],f'{size},{size}')

 def test_stationary_base_and_full_3d_composition(self):
  for kind,folder,size,digest,start in STUDIES:
   path=ROOT/'art-source'/folder/'pass-05'
   with Image.open(path/'base-raw.png') as im:base=im.convert('RGBA')
   with Image.open(path/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
   for idx in (0,4,12,20,28):
    with Image.open(path/f'turret-{idx:03d}-raw.png') as im:composite=Image.alpha_composite(base,im.convert('RGBA'))
    with Image.open(path/('assembly-reference-raw.png' if idx==0 else f'assembly-control-{idx:03d}-raw.png')) as im:self.assertEqual(composite.tobytes(),im.convert('RGBA').tobytes())

 def test_real_engine_png_loaders(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-gdi-small-turret-studies'],cwd=ROOT,env=env,check=True)

if __name__=='__main__':unittest.main()
