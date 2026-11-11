from pathlib import Path
from PIL import Image
import hashlib,json,math,os,subprocess,unittest
import numpy as np
ROOT=Path(__file__).resolve().parents[1];FOLDER=ROOT/'art-source/nod laser turret';STUDY=FOLDER/'pass-04';OUT=STUDY/'review'
class NodLaserTurretStudyTests(unittest.TestCase):
 def test_source_and_saved_model(self):
  self.assertEqual(hashlib.sha256((FOLDER/'laser3 turret.png').read_bytes()).hexdigest(),'4d77eba222321efb813251506bdfaf412eba9df77125a631970c8e4427abf10e')
  report=json.loads((OUT/'validation.json').read_text());self.assertEqual(hashlib.sha256((STUDY/'nod-laser-model.blend').read_bytes()).hexdigest(),report['blend_sha256']);self.assertEqual(report['directions'],32);self.assertFalse(report['game_installed'])
  check=json.loads((STUDY/'geometry-check.json').read_text());self.assertEqual(check['blend_sha256'],report['blend_sha256']);self.assertTrue(check['parts_parented_separately']);self.assertTrue(check['stationary_matrices_unchanged']);self.assertTrue(check['all_meshes_uv_mapped']);self.assertLess(check['fixed_xy_extent'],check['one_cell_xy_limit'])
  geometry=json.loads((STUDY/'geometry.json').read_text());self.assertFalse(geometry['source_projected_over_whole_model']);self.assertEqual(sum(o['stationary'] for o in geometry['objects']),3);self.assertGreater(sum(not o['stationary'] for o in geometry['objects']),80)
 def test_alpha_palette_native_frames_and_order(self):
  with Image.open(FOLDER/'laser3 turret.png') as im:palette={c[:3] for c in im.convert('RGBA').getdata() if c[3]==255}
  hashes=set()
  with Image.open(OUT/'turret32.png') as sheet:
   self.assertEqual(sheet.size,(2048,1024));self.assertEqual(sheet.text['FrameSize'],'256,256');self.assertEqual(sheet.text['FrameAmount'],'32')
   for i in range(32):
    idx=(20+i)%32
    with Image.open(OUT/f'turret-{idx:03d}.png') as im:
     self.assertEqual(im.size,(256,256));a=np.array(im.convert('RGBA'));self.assertLessEqual(set(np.unique(a[:,:,3])),{0,255});self.assertLessEqual({tuple(c) for c in a[:,:,:3][a[:,:,3]>0]},palette);bounds=im.getbbox();self.assertGreater(min(bounds[:2]),0);self.assertLess(max(bounds[2:]),256);hashes.add(hashlib.sha256(im.tobytes()).hexdigest())
     self.assertEqual(sheet.crop(((i%8)*256,(i//8)*256,(i%8+1)*256,(i//8+1)*256)).tobytes(),im.tobytes())
    theta=idx*math.pi/16;x,y=math.sin(theta),-math.cos(theta);yaw=math.atan2(-(x+y)/math.sqrt(2),-2*(x-y)/math.sqrt(8))*1024/math.tau%1024;self.assertLess(abs((yaw-i*32+512)%1024-512),1e-6)
  self.assertEqual(len(hashes),32)
 def test_stationary_base_and_full_3d_occlusion(self):
  with Image.open(STUDY/'base-raw.png') as im:base=im.convert('RGBA')
  with Image.open(STUDY/'base-control-raw.png') as im:self.assertEqual(base.tobytes(),im.convert('RGBA').tobytes())
  for idx in (0,4,12,20,28):
   with Image.open(STUDY/f'turret-{idx:03d}-raw.png') as im:composite=Image.alpha_composite(base,im.convert('RGBA'))
   with Image.open(STUDY/('assembly-reference-raw.png' if idx==0 else f'assembly-control-{idx:03d}-raw.png')) as im:self.assertEqual(composite.tobytes(),im.convert('RGBA').tobytes())
 def test_engine_loader(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',os.environ.get('NOD_STUDY_UTILITY_DLL',str(ROOT/'engine/bin/OpenRA.Utility.dll')),str(ROOT/'mods/ca'),'--check-nod-laser-turret-study'],cwd=ROOT,env=env,check=True)
if __name__=='__main__':unittest.main()
