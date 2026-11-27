import hashlib,json,unittest
from pathlib import Path
from PIL import Image
from convert_handcrafted_layouts import miniyaml
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/propaganda-overlay'
class PropagandaOverlay(unittest.TestCase):
    def test_central_binding_and_unchanged_source(self):
        seq=miniyaml((ROOT/'mods/ca/sequences/misc.yaml').read_text()).child('iconoverlays').child('speaker')
        self.assertEqual(seq.child('Filename').value,'bits/misc/icons/propagandaicon.png')
        self.assertEqual(seq.child('Offset').value,'1, 1')
        source=json.loads((ART/'source.json').read_text());path=ROOT/source['source']
        self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),source['sha256'])
        with Image.open(path) as im:self.assertEqual(im.size,(20,21));self.assertEqual(im.mode,'RGBA')
    def test_native_engine_binding(self):
        seq=miniyaml((ART/'resolved-sequences.yaml').read_text()).child('speaker')
        self.assertEqual(seq.child('Filename').value,'bits/misc/icons/propagandaicon.png')
        self.assertTrue(json.loads((ART/'native-result.json').read_text())['Passed'])
if __name__=='__main__':unittest.main()
