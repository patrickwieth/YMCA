import hashlib,json,unittest
from pathlib import Path
from PIL import Image
from prepare_faction_building_icons import parse
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/map-biome-followup';BASE=ROOT/'mods/ca/bits/terrain/rubberduck'
class MapBiomeFollowup(unittest.TestCase):
    def test_native_generator_and_control_maps(self):
        text=(ART/'generator-checks.txt').read_text()
        for name in ['Arabia','MountainValleys','TwoSides','Continents','Migration','Archipelago']:
            for players in [2,4]:self.assertIn(f'PASS {name} {players}P:',text)
        self.assertIn('CHANGE MAP COMPLETE',(ART/'native-ui-results.txt').read_text())
        self.assertIn('Active maps passed',(ART/'chooser-check.txt').read_text())
    def test_source_cliffs_preserved_and_only_green_pixels_replaced(self):
        for row in json.loads((ART/'biome-art.json').read_text()):
            p=ROOT/row['source'];self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),row['sha256'])
            a=Image.open(p).convert('RGBA');b=Image.open(BASE/'derived/biomes'/row['biome']/p.name).convert('RGBA')
            self.assertEqual(a.size,b.size);self.assertEqual(a.getchannel('A').tobytes(),b.getchannel('A').tobytes())
            for old,new in zip(a.getdata(),b.getdata()):
                r,g,blue,alpha=old
                if not(alpha and g>r+8 and g>blue+8):self.assertEqual(old,new)
    def test_roof_ramp_geometry_and_registration_unchanged(self):
        for biome in ['barren','desert']:
            for p in (BASE/'derived/biomes'/biome).glob('plateau-*.png'):
                a=Image.open(BASE/'derived'/p.name);b=Image.open(p);a.load();b.load()
                self.assertEqual(a.size,b.size);self.assertEqual(a.convert('RGBA').getchannel('A').tobytes(),b.convert('RGBA').getchannel('A').tobytes())
                for key in ['FrameSize','FrameAmount','Offset']:self.assertEqual(a.info[key],b.info[key])
    def test_nuclear_site_is_centered_without_rescaling(self):
        r=parse((ART/'nuclear-resolved.yaml').read_text());s=json.loads((ART/'nuclear-resolved-sequences.json').read_text())
        self.assertEqual(r['Building']['Dimensions'],'4,3');self.assertEqual(r['IsometricSelectable']['DecorationFootprint'],'4,3')
        self.assertNotIn('DecorationBounds',r['IsometricSelectable']);self.assertEqual(r['WithIdleOverlay@SOVIETBIB']['Offset'],'-567,-362,0')
        for state in ['idle','damaged-idle']:
            self.assertEqual(parse(s['apwr.soviet'])[state]['Offset'],'3,-53');self.assertEqual(parse(s['apwr.soviet'])[state]['Scale'],'1')
        # Shared service-depot registration is intentionally NOT changed.
        self.assertEqual(parse(s['sovietbib4x3'])['grid']['Offset'],'32,-16')
        self.assertEqual((ART/'nuclear-site-check.txt').read_text().count('actual ground offset 0,0'),2)
        row=next(r for r in json.loads((ROOT/'art-source/construction-alignment/registration.json').read_text()) if r['image']=='apwr.soviet')
        self.assertEqual(row['target'],[0,0])
if __name__=='__main__':unittest.main()
