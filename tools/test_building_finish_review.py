import hashlib,json,unittest
from pathlib import Path
from prepare_faction_building_icons import parse
from prepare_construction_alignment import ground
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'art-source/building-finish-review'
def blocks(text):
    result={};key=None
    for line in text.splitlines():
        if not line.startswith('\t'):key=line.split(':',1)[0];result[key]=[]
        result[key].append(line)
    return result
class BuildingFinish(unittest.TestCase):
    def test_construction_only_and_ground_registration(self):
        old=json.loads((ART/'sequences-before.json').read_text());new=json.loads((ART/'rules-after.json.sequences.json').read_text())
        records=json.loads((ROOT/'art-source/construction-alignment/registration.json').read_text())
        self.assertGreater(len(records),100)
        for key,text in old.items():
            a=blocks(text);b=blocks(new[key]);a.pop('construction',None);b.pop('construction',None)
            self.assertEqual(a,b,key)
        later={r['image']:r for r in json.loads((ROOT/'art-source/map-biome-followup/construction-registration-before.json').read_text())}
        for row in records:
            # Subsequent nuclear site alignment is validated separately; retain this phase boundary.
            if row['image']=='apwr.soviet':row=later[row['image']]
            seq=parse(new[row['image']])['construction'];before=parse(old[row['image']])['construction']
            self.assertEqual(seq['Scale'],before['Scale']);self.assertEqual(seq['Tick'],before['Tick']);self.assertEqual(seq['Length'],before['Length'])
            offset=[float(v) for v in seq['Offset'].split(',')]
            base=row['basis'] if row.get('method')=='native legacy sprite' else ground(ROOT/'mods/ca/bits/misc'/row['construction'])[0]
            for i in range(2):self.assertAlmostEqual(base[i]+offset[i],row['target'][i],places=2)
    def test_rules_only_bank_bib_changed_since_boundary(self):
        before={r['Actor']:r for r in json.loads((ART/'rules-before.json').read_text())}
        for row in json.loads((ART/'rules-after.json').read_text()):
            if row['Actor']!='swiss_bank':self.assertEqual(before[row['Actor']]['GameplayHash'],row['GameplayHash'],row['Actor'])
            if row['Actor'] in {'syrd','syrf','spen','spen.nod','syrd.gdi','chsyrd'}:self.assertFalse(row['MakeAnimation'])
    def test_footprints_economy_and_four_docks(self):
        r=parse((ROOT/'art-source/soviet-nuclear-4x3/resolved-rules.yaml').read_text())
        self.assertEqual(r['Building']['Dimensions'],'4,3');self.assertEqual(r['Building']['Footprint'],'xxxx xxxx xxxx')
        self.assertEqual(r['WithIdleOverlay@SOVIETBIB']['Image'],'sovietbib4x3')
        self.assertEqual(r['Valued']['Cost'],'1500');self.assertEqual(r['Health']['HP'],'70000');self.assertEqual(r['Power']['Amount'],'800')
        h=parse((ART/'helipad-resolved.yaml').read_text());self.assertEqual(h['Building']['Footprint'],'xxx xxx xxx')
        c=parse((ART/'supply-resolved.yaml').read_text());self.assertEqual(c['Building']['Dimensions'],'3,3');self.assertEqual(c['Building']['LocalCenterOffset'],'0,0,0')
        self.assertEqual(c['DockHost']['DockOffset'],'-724,2172,0')
        self.assertEqual(c['FreeActor@1']['SpawnOffset'],'2,3')
        self.assertEqual([c['SpawnNeighboringActors@dockhelper'+str(i)]['Locations'] for i in range(1,4)],['1,3','0,-1','1,-1'])
        self.assertIn('PASS: 42 deliveries and 42 departures',(ART/'supply-traffic.txt').read_text())
    def test_bank_economy_and_faction_coverage(self):
        r=parse((ART/'bank-resolved.yaml').read_text())
        self.assertEqual(r['Building']['Footprint'],'xx= xx= ===')
        self.assertEqual(r['Valued']['Cost'],'500');self.assertEqual(r['Health']['HP'],'25000');self.assertEqual(r['Power']['Amount'],'-25')
        overlays=[v for k,v in r.items() if k.startswith('WithFactionBibOverlay@')]
        for family,image in [('allies england france germany','alliedbib3x3'),('soviet russia nkorea iraq china chinainf chinatnk chinaatom','sovietbib3x3'),('gdi talon zocom eagle','gdibib3x3'),('nod blackh marked legion','nodbib3x3'),('scrin reaper traveler harvester','scrinbib3x3')]:
            for faction in family.split():
                enabled=[v for v in overlays if (not v.get('Factions') or faction in v['Factions'].split(', ')) and faction not in v.get('ExcludeFactions','').split(', ')]
                self.assertEqual([v['Image'] for v in enabled],[image],faction)
                self.assertEqual(enabled[0]['Offset'],'-1134,0,0')
    def test_authorized_cameo_pixels(self):
        for file in [ART/'icon-adjustments.json',ROOT/'art-source/building-ui-followup/approved-allied-icon-adjustments.json']:
            for path,row in json.loads(file.read_text()).items():self.assertEqual(hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),row['after'],path)
    def test_native_bank_helipad_and_construction(self):
        r=json.loads((ART/'native-result.json').read_text());self.assertTrue(r['Passed'])
        self.assertEqual(len(r['BankFactions']),6);self.assertEqual(r['HelipadBlockedCells'],9);self.assertTrue(r['HelipadDetour'])
        self.assertEqual(len(r['ConstructionActors']),10);self.assertEqual(r['Screenshots'],36)
if __name__=='__main__':unittest.main()
