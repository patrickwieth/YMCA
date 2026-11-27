"""Check a staged Two Sides package without replacing a running user's map."""
import json
import struct
import sys
import unittest
import zipfile
from pathlib import Path
from convert_handcrafted_layouts import ROOT, miniyaml

PACKAGE=Path(sys.argv.pop(1)) if len(sys.argv)>1 and not sys.argv[1].startswith('-') else ROOT/'mods/ca/maps/handcrafted-two-sides-naval.oramap'

class ShoreRefinementTests(unittest.TestCase):
    def test_original_gameplay_and_continuous_beach(self):
        layout=json.loads((ROOT/'art-source/handcrafted/layouts/two-sides-naval.layout.json').read_text())
        with zipfile.ZipFile(PACKAGE) as z:
            yaml=z.read('map.yaml').decode()
            if PACKAGE==ROOT/'mods/ca/maps/handcrafted-two-sides-naval.oramap' and 'AuthoredDryJoins: false' not in yaml:
                self.skipTest('Refinement is staged, not installed; pass the staged package path to test it.')
            data=miniyaml(yaml);binary=z.read('map.bin')
        _,width,height=struct.unpack_from('<BHH',binary)
        tiles,heights,resources=struct.unpack_from('<III',binary,5)
        terrain={};tile={};resource={}
        overrides={n.key:n.value for n in data.child('BaseTerrainOverrides').children}
        for row in layout['cells']:
            c=tuple(row[2:4]);index=row[4]*height+row[5]
            terrain[c]=row[6];tile[c]=struct.unpack_from('<H',binary,tiles+index*3)[0]
            resource[c]=binary[resources+index*2]
            self.assertEqual(overrides[f'{c[0]},{c[1]}'],row[6])
            self.assertEqual(binary[heights+index],0)
            self.assertEqual(tuple(binary[resources+index*2:resources+index*2+2]),tuple(row[9:11]))
        def eligible(c):
            return c in terrain and resource[c]==0 and (terrain[c] in ('Clear','Beach','Rough') or terrain[c]=='Rock' and tile[c] in (1000,1020))
        frontier={c for c,t in tile.items() if t==1050};beach=set()
        for _ in range(3):
            following=set()
            for x,y in frontier:
                for c in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
                    if eligible(c) and c not in beach:beach.add(c);following.add(c)
            frontier=following
        self.assertGreater(len(beach),400)
        for c in terrain:
            if eligible(c):self.assertEqual(tile[c]==1020,c in beach,f'orphan or missing beach at {c}')
        actors=data.child('Actors')
        trees=[a for a in actors.children if a.key.startswith('HandcraftedGrassTree')]
        self.assertGreaterEqual(len(trees),20)
        if trees:
            self.assertLessEqual(len(trees),42)
            starts=[tuple(a['target_cell']) for a in layout['actors'] if a['type']=='mpspawn']
            original=[tuple(a['target_cell']) for a in layout['actors']]
            for tree in trees:
                c=tuple(map(int,tree.child('Location').value.split(',')))
                distance=lambda p:max(abs(p[0]-c[0]),abs(p[1]-c[1]))
                self.assertTrue(all(distance(s)>=12 for s in starts))
                self.assertTrue(all(distance(a)>=5 for a in original))
                for dx in range(-5,6):
                    for dy in range(-5,6):
                        n=(c[0]+dx,c[1]+dy)
                        self.assertIn(n,terrain);self.assertEqual(resource[n],0)
                        if abs(dx)<=4 and abs(dy)<=4:self.assertEqual(terrain[n],'Clear')
                rule=data.child('Rules').child(tree.value)
                self.assertEqual(rule.child('Inherits').value,'^RubberduckTerrainDecoration')
                self.assertEqual({n.key for n in rule.children},{'Inherits','RenderSprites'})
        for actor in layout['actors']:
            stored=actors.child(actor['id']);self.assertEqual(stored.value,actor['type'])
            values={n.key:n.value for n in stored.children}
            for init in actor['source_initializers']:
                expected=','.join(map(str,actor['target_cell'])) if init['key']=='Location' else init['value']
                self.assertEqual(values[init['key']],expected)
        with zipfile.ZipFile(PACKAGE) as z:
            self.assertIn('AuthoredDryJoins: false',z.read('map.yaml').decode())

if __name__=='__main__':unittest.main()
