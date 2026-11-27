"""Resolved runtime-rule checks for bomb range and the protected Nod exit."""
import os
import subprocess
import unittest
from pathlib import Path
from convert_handcrafted_layouts import miniyaml
ROOT=Path(__file__).resolve().parents[1]

def resolved(command,name):
    env=dict(os.environ,ENGINE_DIR=str(ROOT/'engine'),MOD_SEARCH_PATHS=f'{ROOT/"mods"},{ROOT/"engine/mods"}')
    return miniyaml(subprocess.check_output(['dotnet',str(ROOT/'engine/bin/OpenRA.Utility.dll'),'ca',command,name],cwd=ROOT,env=env,text=True))

class CombatRegressionTests(unittest.TestCase):
    def test_all_four_bombs_can_attack_within_their_range(self):
        for weapon in ('KirovBomb','KirovTeslaBomb','KirovClusterBomb','KirovNukeBomb'):
            with self.subTest(weapon=weapon):
                data=resolved('--resolved-weapons',weapon)
                self.assertEqual(data.child('MinRange').value,'0c0')
                self.assertEqual(data.child('Range').value,'1c0')
        artillery=resolved('--resolved-weapons','^ShellArtillery')
        self.assertEqual(artillery.child('MinRange').value,'4c0')
    def test_nod_exit_is_on_existing_passable_footprint(self):
        nod=resolved('--resolved-rules','WEAP.NOD');gdi=resolved('--resolved-rules','WEAP.TD')
        b=nod.child('Building')
        self.assertEqual(b.child('Dimensions').value,'4,3')
        self.assertEqual(b.child('FootprintOffset').value,'-1,-1')
        self.assertEqual(b.child('Footprint').value,'xxxx x+xx =+==')
        self.assertEqual(nod.child('Exit@1').child('ExitCell').value,'0,1')
        self.assertEqual(nod.child('Exit@1').child('SpawnOffset').value,'-341,-341,0')
        self.assertEqual(gdi.child('Exit@1').child('ExitCell').value,'0,2')
        # Convert exit to footprint-local coordinates: (0,1)-(-1,-1)=(1,2).
        self.assertEqual(b.child('Footprint').value.split()[2][1],'+')
    def test_shellmap_overrides_are_scoped(self):
        data=miniyaml((ROOT/'mods/ca/maps/liberty-stream-shellmap/liberty-rules.yaml').read_text())
        self.assertIn('LIBERTY.KIROV',[n.key for n in data.children])
        self.assertNotIn('KIROV',[n.key.upper() for n in data.children])
        self.assertEqual(data.child('LIBERTY.KIROV').child('Inherits').value,'Kirov')

if __name__=='__main__':unittest.main()
