from pathlib import Path
import hashlib,json,unittest
from PIL import Image
from prepare_faction_building_icons import parse
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/building-ui-followup'
NAVAL={'syrd','syrf','spen','spen.nod','syrd.gdi','chsyrd'}
class BuildingUiFollowup(unittest.TestCase):
    def test_four_pixel_offsets_all_body_states(self):
        seq=json.loads((ART/'resolved-sequences.json').read_text())
        for name,old,scale in [('chpower2.supplied',-8,1),('chsupply.supplied',19,1.25)]:
            for state in ['idle','damaged-idle','place','dead']:
                node=parse(seq[name])[state]
                self.assertEqual(float(node['Scale']),scale)
                self.assertAlmostEqual((float(node['Offset'].split(',')[1])-old)*scale,4)
    def test_two_dishes_and_actual_placement_source(self):
        with Image.open(ROOT/'mods/ca/bits/china/buildings/radar.png') as im:expected=im.convert('RGBA').crop((0,0,60,50))
        with Image.open(ROOT/'mods/ca/bits/misc/overlays/china-radar-preview.png') as actual:
            self.assertEqual(actual.size,expected.size);self.assertEqual(actual.convert('RGBA').tobytes(),expected.tobytes())
        node=parse(json.loads((ART/'resolved-sequences.json').read_text())['chcommand.radar'])['idle']
        self.assertEqual(node['Filename'],'bits/misc/overlays/china-radar-preview.png')
        data=json.loads((ART/'native-result.json').read_text());self.assertEqual(data['RadarPreview'],'PlaceBuildingOrderGenerator')
    def test_naval_only_make_removal_and_unrelated_rules(self):
        before={r['Actor']:r for r in json.loads((ROOT/'art-source/soviet-building-followup/rules-after.json').read_text())}
        after={r['Actor']:r for r in json.loads((ART/'rules-after.json').read_text())}
        for name in NAVAL:self.assertFalse(after[name]['MakeAnimation'],name)
        self.assertTrue(after['powr']['MakeAnimation'])
        for name,row in before.items():
            if name not in NAVAL:self.assertEqual(row['GameplayHash'],after[name]['GameplayHash'],name)
            self.assertEqual(row['Prerequisites'],after[name]['Prerequisites'],name)
            for a,b in zip(row['Views'],after[name]['Views']):
                if b['Image'] not in {'chpower2.supplied','chsupply.supplied','chcommand.radar'}:self.assertEqual(a['BodyHash'],b['BodyHash'],name)
    def test_only_requested_cameos_changed(self):
        old=json.loads((ART/'icons-before.json').read_text());changed=[]
        for name,sha in old.items():
            if hashlib.sha256((ROOT/'mods/ca/bits/faction-icons'/name).read_bytes()).hexdigest()!=sha:changed.append(name)
        self.assertEqual(set(changed),{'weap-td.png','proc-td.png','pyle.png','atwr.png','chcommand-radar.png','weap-nod.png','rep-nod.png','port.png','wsph.png','srep.png','grav.png','rift.png','mani.png','powr-soviet.png','cloning_vats.png'})
        catalog=json.loads((ROOT/'art-source/faction-building-icons/catalog.json').read_text())
        self.assertEqual(next(c for c in catalog if c['key']=='atwr')['facing'],597)
    def test_real_creation_and_menu_runs(self):
        native=json.loads((ART/'native-result.json').read_text());self.assertTrue(native['Passed'])
        self.assertEqual(set(native['NavalActors']),NAVAL-{'syrf'})
        self.assertTrue(json.loads((ART/'gdi-native-result.json').read_text())['Passed'])
if __name__=='__main__':unittest.main()
