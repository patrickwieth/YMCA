"""Allied cameo coverage and preservation of approved artwork/gameplay/other factions."""
from pathlib import Path
import hashlib, json, unittest
from PIL import Image
from convert_handcrafted_layouts import miniyaml
from prepare_allied_building_icons import allied_palette

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'art-source/allied-building-icons'
SHARED={'oilp','oil_platform','swiss_bank'}
ALLIED={'afld.allies','agun','apwr','atek','dome','domf','facf','fact','fact.nukular','fix','gap','grand_cannon','gun','hbox','hpad','orep','pbox','pdox','powr','pris','proc','silo','syrd','syrf','tent','weaf','weap','weat'}

class AlliedBuildingIconTests(unittest.TestCase):
    def test_approved_power_pixels_preserved(self):
        for filename,sha in json.loads((ART/'approved-power-hashes.json').read_text()).items():
            self.assertEqual(hashlib.sha256((ROOT/'mods/ca/bits/allies/icons'/filename).read_bytes()).hexdigest(),sha)

    def test_sources_and_generated_dimensions(self):
        records=json.loads((ART/'provenance.json').read_text())
        self.assertEqual(len(records),23)
        for r in records:
            for s in r['sources']:
                self.assertEqual(hashlib.sha256((ROOT/s['path']).read_bytes()).hexdigest(),s['sha256'])
            with Image.open(ROOT/'mods/ca/bits/allies/icons'/f"{r['id']}.png") as im:
                self.assertEqual(im.size,(192,144))
                self.assertEqual(im.getchannel('A').getextrema(),(255,255))

    def test_player_palette_remap_keeps_alpha_and_non_player_colors(self):
        for name,td in [('grand-cannon',False),('swiss-bank',True),('turret',False),('silo',False)]:
            source=ART/'sources'/f'{name}.png'
            im=Image.open(source);original=im.convert('RGBA');actual=allied_palette(source,td)
            allowed=set(range(176,192) if td else range(80,96))
            self.assertEqual(original.getchannel('A').tobytes(),actual.getchannel('A').tobytes())
            for index,before,after in zip(im.getdata(),original.getdata(),actual.getdata()):
                if index not in allowed:self.assertEqual(before,after)

    def test_resolved_rules_and_body_art_unchanged(self):
        before={a['Actor']:a for a in json.loads((ART/'rules-before.json').read_text())}
        after={a['Actor']:a for a in json.loads((ART/'validation/rules-after.json').read_text())}
        self.assertEqual(set(before),set(after))
        self.assertTrue((ALLIED|SHARED)<=set(after))
        for name,a in after.items():
            b=before[name]
            self.assertEqual(a['GameplayHash'],b['GameplayHash'],name)
            self.assertEqual(a['Prerequisites'],b['Prerequisites'],name)
            for view,old in zip(a['Views'],b['Views']):
                self.assertEqual(view['BodyHash'],old['BodyHash'],(name,view['Faction']))
                targeted=name in ALLIED or (name in SHARED and view['Faction'] in ('england','france','germany'))
                if targeted:
                    self.assertTrue(view['File'].startswith('bits/allies/icons/'),(name,view))
                    self.assertEqual(view['Font'],'OsShp')
                else:
                    self.assertEqual((view['File'],view['Font'],view['Label']),(old['File'],old['Font'],old['Label']),(name,view['Faction']))

    def test_distinct_variant_captions(self):
        nodes=miniyaml((ROOT/'mods/ca/sequences/allied-building-icons.yaml').read_text())
        self.assertEqual(nodes.child('pbox.allies').child('camo-icon').child('ButtonLabel').value,'Camo Pillbox')
        for image in ('fact.allies.icons','weap.allies','dome.allies','syrd.allies'):
            self.assertTrue(nodes.child(image).child('fake-icon').child('ButtonLabel').value.startswith('Fake '))

if __name__=='__main__':unittest.main()
