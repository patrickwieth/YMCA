import hashlib,json,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/faction-building-icons'
def load(name):return json.loads((ART/name).read_text())
def body(text):
    # Preserve the complete nested MiniYaml text, excluding only portrait states.
    blocks={};key=None
    for line in text.splitlines():
        if not line.startswith('\t'):key=line.split(':',1)[0];blocks[key]=[]
        blocks[key].append(line)
    return {k:v for k,v in blocks.items() if k not in ('icon','iconadv') and not k.endswith('-icon')}
class FactionBuildingIcons(unittest.TestCase):
    def test_sources_and_dimensions(self):
        rows=load('provenance.json');self.assertEqual(len(rows),99)
        for r in rows:
            self.assertEqual(hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest(),r['sha256'],r['id'])
            with Image.open(ROOT/'mods/ca/bits/faction-icons'/f"{r['id']}.png") as im:self.assertEqual(im.size,(192,144))
        self.assertEqual(sum(r['method']=='direct PNG' for r in rows),81)
    def test_approved_allied_pixels(self):
        replacements=json.loads((ROOT/'art-source/allied-radar-helipad/icon-replacements.json').read_text())
        replacements.update(json.loads((ROOT/'art-source/allied-tech-center/icon-replacement.json').read_text()))
        replacements.update(json.loads((ROOT/'art-source/building-ui-followup/approved-allied-icon-adjustments.json').read_text()))
        for path,digest in load('approved-allied-hashes.json').items():
            if path in replacements:
                self.assertEqual(replacements[path]['before'],digest,path)
                digest=replacements[path]['after']
            self.assertEqual(hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),digest,path)
    def test_gameplay_and_all_non_icon_sequences(self):
        before={r['Actor']:r for r in load('rules-before.json')};after={r['Actor']:r for r in load('rules-after.json')}
        self.assertEqual(before.keys(),after.keys())
        for key,row in before.items():self.assertEqual(row['GameplayHash'],after[key]['GameplayHash'],key)
        old=load('sequences-before.json');new=load('sequences-after.json')
        for key in old:self.assertEqual(body(old[key]),body(new[key]),key)
    def test_bindings_and_exceptions(self):
        before={r['Actor']:r for r in load('rules-before.json')};after={r['Actor']:r for r in load('rules-after.json')}
        for key in ('barr','apwr.soviet'):
            self.assertEqual(before[key]['Views'],after[key]['Views'])
        bindings={r['Actor']:r for r in json.loads((ROOT/'art-source/soviet-building-followup/rules-after.json').read_text())}
        # Later supplied Barracks replacement intentionally retires its legacy icon.
        bindings['barr']=next(r for r in json.loads((ROOT/'art-source/soviet-barracks/rules-after.json').read_text()) if r['Actor']=='barr')
        bindings['apwr.soviet']=next(r for r in json.loads((ROOT/'art-source/building-finish-review/rules-after.json').read_text()) if r['Actor']=='apwr.soviet')
        for c in load('catalog.json'):
            for actor in c['actors']:
                for view in bindings[actor]['Views']:
                    self.assertEqual(view['File'],f"bits/faction-icons/{c['key']}.png",actor)
                    self.assertEqual(view['Font'],'OsShp',actor)
        # The already-approved Allied bindings, including their shared income aliases.
        for key in ('fact','powr','apwr','tent','weap','dome','hpad','agun','oilp','oil_platform','swiss_bank'):
            for old,new in zip(before[key]['Views'][:3],after[key]['Views'][:3]):self.assertEqual(old,new,key)
    def test_native_menus(self):
        for family,data in load('validation/native-results.json').items():
            self.assertTrue(data['Passed'],family)
            self.assertEqual(data['ActualUIScale'],1.25)
if __name__=='__main__':unittest.main()
