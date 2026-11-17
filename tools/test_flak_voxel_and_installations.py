"""Runtime asset/binding regressions; not a substitute for live visual checks."""
import hashlib,json,math,os,struct,subprocess,unittest,re
from pathlib import Path
from PIL import Image
from test_soviet_building_art import resolved,traits,gameplay_hash
ROOT=Path(__file__).resolve().parents[1]
def utility(command):
 env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
 return subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),str(ROOT/'mods/ca'),command],cwd=ROOT,env=env,text=True)
class FlakInstallationTests(unittest.TestCase):
 def test_three_real_voxel_parts_and_recoil(self):
  self.assertIn('axial recoil validated',utility('--check-soviet-flak-voxel'))
  root=ROOT/'mods/ca/bits/soviet/buildings/experimental'
  for part in ('base','turret','barrel'):
   data=(root/f'flak-{part}.vxl').read_bytes();self.assertTrue(data.startswith(b'Voxel Animation\0'))
   _,headers,tails,length=struct.unpack_from('<4I',data,16);self.assertEqual((headers,tails),(1,1))
   footer=830+length;lowhigh=struct.unpack_from('<6f',data,footer+64)
   if part=='base':self.assertLess(max(abs(lowhigh[i]) for i in (0,1,3,4))*math.sqrt(2),64)
   hva=(root/f'flak-{part}.hva').read_bytes();self.assertEqual(struct.unpack_from('<2I',hva,16),(1,1))
  self.assertEqual((ROOT/'art-source/soviet flak/voxel-01/SAM-before.yaml').read_bytes(),(ROOT/'art-source/soviet flak/voxel-01/SAM-after.yaml').read_bytes())
 def test_allied_approved_assets_and_registration(self):
  self.assertIn('two muzzle slots valid',utility('--check-allied-flak-art'))
  for src,dest in [('base.png','flak-base.png'),('turret32.png','flak-turret32.png')]:
   self.assertEqual((ROOT/'art-source/allies flak/pass-15/review'/src).read_bytes(),(ROOT/'mods/ca/bits/allies/buildings'/dest).read_bytes())
 def test_helipad_binding_and_unchanged_gameplay(self):
  self.assertIn('native194x135 PNG',utility('--check-soviet-helipad-art'))
  before=(ROOT/'art-source/soviet-helipad-refresh/before/HPAD.Soviet.yaml').read_text(encoding='utf-8-sig');after=resolved('--resolved-rules','HPAD.Soviet');self.assertEqual(gameplay_hash(before),gameplay_hash(after))
  allied_now=resolved('--resolved-rules','HPAD')
  self.assertIn('Sequence: construction',traits(allied_now)['WithMakeAnimation'])
  # Only the newly approved global make-sequence selection may differ here.
  allied_now=re.sub(r'(WithMakeAnimation:\n)\tSequence: construction\n',r'\1',allied_now)
  self.assertEqual(traits((ROOT/'art-source/soviet-helipad-refresh/before/HPAD.yaml').read_text(encoding='utf-8-sig')),traits(allied_now))
  seq=traits(resolved('--resolved-sequences','hpad.soviet'))
  for state in ('idle','damaged-idle','active','damaged-active'):
   self.assertIn('Filename: bits/soviet/buildings/helipad.png',seq[state]);self.assertIn('Scale: 1',seq[state]);self.assertIn('Offset: -1,2',seq[state]);self.assertIn('Length: 1',seq[state])
  with Image.open(ROOT/'mods/ca/bits/soviet/buildings/helipad.png') as im:self.assertEqual(im.size,(194,135))
if __name__=='__main__':unittest.main()
