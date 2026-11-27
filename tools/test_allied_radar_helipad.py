import hashlib,json,unittest
from pathlib import Path
from PIL import Image
from prepare_faction_building_icons import parse
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/allied-radar-helipad'
class AlliedRadarHelipad(unittest.TestCase):
    def test_bounded_alpha_only_mast_cutout(self):
        old=Image.open(ART/'supplied-radar dome.png').convert('RGBA');new=Image.open(ROOT/'mods/ca/bits/allies/buildings/radar dome.png').convert('RGBA')
        mask=Image.open(ART/'mast-void-mask.png');self.assertEqual(old.size,new.size)
        changed=0
        for a,b,m in zip(old.getdata(),new.getdata(),mask.getdata()):
            self.assertEqual(a[:3],b[:3])
            if m:self.assertEqual(a,(0,1,7,255));self.assertEqual(b[3],0);changed+=1
            else:self.assertEqual(a,b)
        self.assertEqual(changed,545)
    def test_helipad_original_and_unrelated_icons_preserved(self):
        self.assertEqual((ART/'supplied-Helipad.png').read_bytes(),(ROOT/'mods/ca/bits/allies/buildings/helipad.png').read_bytes())
        for name,digest in json.loads((ART/'icons-before.json').read_text()).items():
            if name not in ('helipad.png','radar.png'):
                # Tech Center was explicitly replaced in the later supplied-art pass.
                actual=json.loads((ROOT/'art-source/allied-tech-center/icons-before.json').read_text())[name] if name=='tech-center.png' else hashlib.sha256((ROOT/'mods/ca/bits/allies/icons'/name).read_bytes()).hexdigest()
                newer=json.loads((ROOT/'art-source/building-ui-followup/approved-allied-icon-adjustments.json').read_text()).get('mods/ca/bits/allies/icons/'+name)
                if newer:actual=newer['before']
                self.assertEqual(actual,digest,name)
    def test_resolved_files_and_placement(self):
        seq=json.loads((ART/'resolved-sequences.json').read_text())
        for image,file,offset,states in [('hpad.allies','helipad.png','-10,59',['idle','damaged-idle','active','damaged-active']),('dome.allies','radar dome.png','0,25',['idle','damaged-idle'])]:
            d=parse(seq[image])
            for state in states:
                self.assertEqual(d[state]['Filename'],'bits/allies/buildings/'+file)
                self.assertEqual(d[state]['Offset'],offset)
                self.assertEqual(d[state].get('Scale','1'),'1')
    def test_gameplay_and_unrelated_factions(self):
        before={r['Actor']:r for r in json.loads((ROOT/'art-source/faction-building-icons/rules-after.json').read_text())}
        after={r['Actor']:r for r in json.loads((ART/'rules-after.json').read_text())}
        self.assertEqual(before.keys(),after.keys())
        for actor,old in before.items():
            new=after[actor];self.assertEqual(old['GameplayHash'],new['GameplayHash'],actor)
            for a,b in zip(old['Views'],new['Views']):
                if b['Image'] not in ('hpad.allies','dome.allies'):self.assertEqual(a,b,(actor,b['Faction']))
if __name__=='__main__':unittest.main()
