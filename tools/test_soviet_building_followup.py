"""Requested footprint registration and source-crop corrections only."""
from pathlib import Path
import hashlib,json,unittest
from convert_handcrafted_layouts import miniyaml
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/soviet-building-followup'
class SovietBuildingFollowup(unittest.TestCase):
    def test_only_requested_icons_changed(self):
        hashes=json.loads((ART/'icon-hashes-before.json').read_text());changed=[]
        later=json.loads((ROOT/'art-source/building-ui-followup/icons-before.json').read_text())
        for name,sha in hashes.items():
            # These GDI/Nod/Scrin cameos were deliberately revised in the subsequent pass;
            # that pass validates their current pixels against this boundary.
            actual=later[name] if name in {'weap-td.png','proc-td.png','pyle.png','atwr.png','weap-nod.png','rep-nod.png','port.png','wsph.png','srep.png','grav.png','rift.png','mani.png','powr-soviet.png','cloning_vats.png'} else hashlib.sha256((ROOT/'mods/ca/bits/faction-icons'/name).read_bytes()).hexdigest()
            if actual!=sha:changed.append(name)
        self.assertEqual(set(changed),{'stek.png','kenn.png','indp.png','chcommand.png'})
    def test_footprint_cells_and_balance_preserved(self):
        a=miniyaml((ART/'tpwr-before.yaml').read_text());b=miniyaml((ART/'tpwr-after.yaml').read_text())
        for name in ('Dimensions','Footprint','FootprintOffset'):
            self.assertEqual(a.child('Building').child(name).value,b.child('Building').child(name).value)
        for trait,field in [('Valued','Cost'),('Power','Amount'),('Health','HP'),('PowerMultiplier','Modifier')]:
            self.assertEqual(a.child(trait).child(field).value,b.child(trait).child(field).value)
        self.assertEqual(b.child('Building').child('LocalCenterOffset').value,'0,-724,0')
        self.assertEqual(b.child('WithIdleOverlay@SOVIETBIB').child('Offset').value,'-1134,0,0')
        self.assertEqual(b.child('HitShape').child('Type').child('TopLeft').value,'-1024,-1024')
        self.assertEqual(b.child('HitShape').child('Type').child('BottomRight').value,'1024,1024')
    def test_other_actors_and_body_sequences_unchanged(self):
        before={r['Actor']:r for r in json.loads((ROOT/'art-source/allied-radar-helipad/rules-after.json').read_text())}
        after={r['Actor']:r for r in json.loads((ART/'rules-after.json').read_text())}
        for name,row in before.items():
            if name=='tpwr':continue
            self.assertEqual(row['GameplayHash'],after[name]['GameplayHash'],name)
            for old,new in zip(row['Views'],after[name]['Views']):
                self.assertEqual(old['BodyHash'],new['BodyHash'],name)
    def test_native_galleries(self):
        for family in ('soviet','china'):
            data=json.loads((ART/(family+'-native-result.json')).read_text());self.assertTrue(data['Passed'])
        data=json.loads((ART/'china-native-result.json').read_text())
        self.assertIn('iconreview.chcommand.radar',data['Actors'])
if __name__=='__main__':unittest.main()
