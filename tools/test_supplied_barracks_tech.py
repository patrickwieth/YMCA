import hashlib,json,unittest
from pathlib import Path
from prepare_faction_building_icons import parse
ROOT=Path(__file__).resolve().parents[1]
B=ROOT/'art-source/soviet-barracks'; A=ROOT/'art-source/allied-tech-center'
class SuppliedBuildings(unittest.TestCase):
    def test_originals_and_only_requested_icons(self):
        for art,source,folder,changed in [(B,'soviet/buildings/barracks.png','faction-icons',set()),(A,'allies/buildings/tech center.png','allies/icons',{'tech-center.png'})]:
            self.assertEqual((art/'supplied.png').read_bytes(),(ROOT/'mods/ca/bits'/source).read_bytes())
            old=json.loads((art/'icons-before.json').read_text())
            # Later explicitly requested cameo shifts have their own current-pixel checks.
            later=json.loads((ROOT/'art-source/building-finish-review/icon-adjustments.json').read_text())
            later.update(json.loads((ROOT/'art-source/building-ui-followup/approved-allied-icon-adjustments.json').read_text()))
            actual={n for n,h in old.items() if later.get('mods/ca/bits/'+folder+'/'+n,{}).get('before',hashlib.sha256((ROOT/'mods/ca/bits'/folder/n).read_bytes()).hexdigest())!=h}
            self.assertEqual(actual,changed)
        self.assertTrue((ROOT/'mods/ca/bits/faction-icons/barr-soviet.png').exists())
    def test_resolved_barracks_site_and_exits(self):
        r=parse((B/'resolved-rules.yaml').read_text());old=parse((B/'resolved-rules-before.yaml').read_text())
        self.assertEqual(r['Building']['Dimensions'],'2,2')
        self.assertEqual(r['Building']['Footprint'],'xx ==')
        self.assertEqual(r['Building']['FootprintOffset'],'0,0')
        self.assertEqual(r['Building']['LocalCenterOffset'],'0,0,0')
        self.assertEqual(r['WithIdleOverlay@SOVIETBIB']['Image'],'sovietbib2x2')
        self.assertEqual(r['Exit@1']['ExitCell'],'1,1')
        self.assertEqual(r['Exit@2']['ExitCell'],'0,1')
        for trait in ['Valued','Health','Power','Buildable']:
            self.assertEqual(r[trait],old[trait],trait)
    def test_resolved_sources_all_states_and_unrelated_rules(self):
        seq=json.loads((B/'rules-after.json.sequences.json').read_text())
        for image,file,offset,states in [('barr.soviet','soviet/buildings/barracks.png','26,12',['idle','damaged-idle']),('atek.allies','allies/buildings/tech center.png','0,20',['idle','damaged-idle','active','damaged-active','false-active','damaged-false-active'])]:
            for state in states:
                row=parse(seq[image])[state]
                self.assertEqual(row['Filename'],'bits/'+file)
                self.assertEqual(row.get('Scale','1'),'1');self.assertEqual(row['Offset'],offset)
        before={r['Actor']:r for r in json.loads((B/'rules-before.json').read_text())}
        for row in json.loads((B/'rules-after.json').read_text()):
            old=before[row['Actor']]
            if row['Actor']!='barr':self.assertEqual(old['GameplayHash'],row['GameplayHash'],row['Actor'])
            for a,b in zip(old['Views'],row['Views']):
                if b['Image'] not in {'barr.soviet','atek.allies'}:self.assertEqual(a,b,row['Actor'])
    def test_native_menus(self):
        for art in [A,B]:
            data=json.loads((art/'native-menu-result.json').read_text())
            self.assertTrue(data['Passed']);self.assertEqual(data['ActualUIScale'],1.25)
if __name__=='__main__':unittest.main()
