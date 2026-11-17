"""Texture-only guard: exact original geometry/registration and native pixel provenance."""
from pathlib import Path
import json,unittest
import numpy as np
from PIL import Image
from test_soviet_building_art import resolved,traits
from test_current_building_followups import field
R=Path(__file__).resolve().parents[1];F=R/'art-source/gdi missile tower';OLD=F/'pass-14';NEW=F/'pass-16'
class GdiMissileTextureOnly(unittest.TestCase):
 def test_geometry_palette_and_mount_unchanged(self):
  a=np.load(OLD/'colored-voxels.npz');b=np.load(NEW/'colored-voxels.npz')
  for key in ('coords','normals','grid_min','grid_size','density','palette'):self.assertTrue(np.array_equal(a[key],b[key]),key)
  for name in ('base.png','missile-turret.hva','turret-palette.png','engine-projection.json'):self.assertEqual((OLD/name).read_bytes(),(NEW/name).read_bytes())
  self.assertTrue(json.loads((NEW/'geometry-preservation.json').read_text())['identical_vertices_faces_and_world_matrices'])
  self.assertTrue(np.any(a['colors']!=b['colors']))
  old=np.load(OLD/'surface-uv.npz');roles=json.loads((OLD/'voxel-sampling.json').read_text())['textures']
  for i,role in enumerate(roles):
   if role not in ('gold','goldlight','goldshade'):self.assertTrue(np.array_equal(a['colors'][old['texture']==i],b['colors'][old['texture']==i]),role)
  r=traits(resolved('--resolved-rules','ATWR'))
  self.assertEqual(field(r['RenderVoxels'],'Scale'),'0.85');self.assertEqual(field(r['Turreted'],'Offset'),'0,11,1527')
  for arm in ('primary','secondary','tertiary','quartary'):self.assertEqual(field(r['Armament@'+arm],'LocalOffset'),'365,163,853, 365,-163,853')
 def test_each_gold_atlas_texel_is_an_original_source_pixel(self):
  meta=json.loads((NEW/'texture-provenance.json').read_text());src=np.array(Image.open(R/meta['source']).convert('RGBA'))
  for panel in meta['panels']:
   name=panel['name'];xy=np.load(NEW/'source-cuts'/f'{name}-source-xy.npy');im=np.array(Image.open(NEW/'source-cuts'/f'{name}.png').convert('RGBA'))
   self.assertTrue(np.array_equal(im,src[xy[:,:,1],xy[:,:,0]]),name)
  self.assertEqual((NEW/'missile-turret.vxl').read_bytes(),(R/'mods/ca/bits/gdi/buildings/missile-tower-turret.vxl').read_bytes())
  self.assertEqual((OLD/'base.png').read_bytes(),(R/'mods/ca/bits/gdi/buildings/missile-tower-base.png').read_bytes())
if __name__=='__main__':unittest.main()
