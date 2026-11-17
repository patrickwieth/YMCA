"""Real VXL/HVA validation and isolation from sprite Sentry / Flame Tower."""
import json,os,struct,subprocess,tempfile,unittest,zipfile,re
from pathlib import Path
from PIL import Image
import numpy as np
from test_soviet_building_art import ROOT,resolved,traits
STUDY=ROOT/'art-source/soviet-sentry/voxel-01';ASSET=ROOT/'mods/ca/bits/soviet/buildings/experimental'

class SentryVoxelTests(unittest.TestCase):
 def test_binary_voxels_match_export_data(self):
  v=np.load(STUDY/'colored-voxels.npz');raw=(ASSET/'sentry-v13-voxel.vxl').read_bytes()
  self.assertEqual(raw[:16],b'Voxel Animation\0')
  _,limbs,tailers,body_size=struct.unpack_from('<4I',raw,16);self.assertEqual((limbs,tailers),(1,1))
  footer=802+28+body_size;nx,ny,nz,kind=struct.unpack_from('<4B',raw,footer+88)
  self.assertEqual((nx,ny,nz),tuple(v['grid_size']));self.assertEqual(kind,4)
  starts=struct.unpack_from('<'+'i'*(nx*ny),raw,830);data=830+nx*ny*8;actual={}
  for i,start in enumerate(starts):
   if start<0:continue
   p=data+start;z=0
   while z<nz:
    skip,count=raw[p:p+2];p+=2;z+=skip
    for j in range(count):actual[(i%nx,i//nx,z)]=tuple(raw[p:p+2]);p+=2;z+=1
    self.assertEqual(raw[p],count);p+=1
  expected={tuple(map(int,c)):(int(color),int(normal)) for c,color,normal in zip(v['coords'],v['colors'],v['normals'])}
  self.assertEqual(actual,expected);self.assertGreater(len(actual),10000)
  hva=(ASSET/'sentry-v13-voxel.hva').read_bytes();self.assertEqual(struct.unpack_from('<2I',hva,16),(1,1))
  with Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png') as source:colors={c[:3] for c in source.convert('RGBA').getdata() if c[3]==255}
  with Image.open(ASSET/'sentry-voxel-palette.png') as p:
   self.assertEqual(p.mode,'P');rgb=p.convert('RGBA')
   for index in set(v['colors'].tolist()):self.assertIn(rgb.getpixel((index,0))[:3],colors)

 def test_real_engine_readers_and_trait_dependencies(self):
  env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT}/mods,{ROOT}/engine/mods')
  subprocess.run(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),'--check-sentry-voxel'],cwd=ROOT,env=env,check=True)

 def test_distinct_actor_and_side_by_side_map_slot(self):
  sprite=traits(resolved('--resolved-rules','SENTRY.SOVIET'));voxel=traits(resolved('--resolved-rules','SENTRY.SOVIET.VOXEL'))
  self.assertIn('WithSpriteTurret',sprite);self.assertNotIn('RenderVoxels',sprite)
  self.assertNotIn('WithSpriteTurret',voxel);self.assertIn('WithVoxelTurret',voxel)
  self.assertIn('Palette: sentry-voxel-source',voxel['RenderVoxels']);self.assertNotIn('Buildable',voxel)
  for key in ('Armament','AttackTurreted','Building','Turreted','IsometricSelectable','RenderSprites'):self.assertEqual(sprite[key],voxel[key],key)
  source=ROOT/'mods/ca/maps/testmap1.oramap';before=source.read_bytes()
  with tempfile.TemporaryDirectory() as d:
   target=Path(d)/'testmap1-debug.oramap'
   subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'tools/prepare-test-game-map.ps1'),'-SourceMap',str(source),'-DestinationMap',str(target)],check=True)
   with zipfile.ZipFile(target) as z:text=z.read('map.yaml').decode('utf-8-sig')
   self.assertRegex(text,r'ActorSentryVoxel: sentry.soviet.voxel\r?\n\t\tOwner: Multi0\r?\n\t\tLocation: 142,-66')
   self.assertIn('Actor27: sentry.soviet',text);self.assertNotRegex(text,r'(?m)^\tFTUR:')
  self.assertEqual(source.read_bytes(),before)

if __name__=='__main__':unittest.main()
