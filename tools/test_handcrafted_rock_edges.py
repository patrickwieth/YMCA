"""Validate staged Glitters contour cleanup against the installed terrain layout."""
import json
import re
import struct
import sys
import unittest
import zipfile
from pathlib import Path
from convert_handcrafted_layouts import ROOT, miniyaml

PACKAGE=Path(sys.argv.pop(1)) if len(sys.argv)>1 and not sys.argv[1].startswith('-') else ROOT/'mods/ca/maps/handcrafted-all-that-glitters-barren.oramap'

class RockEdgeTests(unittest.TestCase):
    @unittest.skipUnless(PACKAGE,'Pass the staged Glitters package path.')
    def test_visual_only_and_reclaimed_building_space(self):
        current=ROOT/'art-source/handcrafted/history/before-edge-followup/handcrafted-all-that-glitters-barren.oramap'
        if not current.exists():current=ROOT/'mods/ca/maps/handcrafted-all-that-glitters-barren.oramap'
        with zipfile.ZipFile(current) as old,zipfile.ZipFile(PACKAGE) as new:
            a=miniyaml(old.read('map.yaml').decode());b=miniyaml(new.read('map.yaml').decode())
            before=old.read('map.bin');after=new.read('map.bin')
        def tree(node):return (node.key,node.value,tuple(tree(c) for c in node.children))
        for name in ('CellBounds','Bounds','Players','BaseTerrainOverrides'):
            self.assertEqual(tree(a.child(name)),tree(b.child(name)))
        for node in a.child('Actors').children:
            if not node.key.startswith('Handcrafted'):
                self.assertEqual(tree(node),tree(b.child('Actors').child(node.key)))
        ta,ha,ra=struct.unpack_from('<III',before,5);tb,hb,rb=struct.unpack_from('<III',after,5)
        self.assertEqual(before[ha:],after[hb:],'Heights/resources changed')
        _,width,height=struct.unpack_from('<BHH',after)
        layout=json.loads((ROOT/'art-source/handcrafted/layouts/all-that-glitters-barren.layout.json').read_text())
        tiles={tuple(r[2:4]):struct.unpack_from('<H',after,tb+3*(r[4]*height+r[5]))[0] for r in layout['cells']}
        manifest=json.loads((ROOT/'art-source/handcrafted/packages/validation.json').read_text())
        reclaimed=next(e['ReclaimedRockCells'] for e in manifest if e['slug']=='all-that-glitters-barren')
        self.assertEqual(len(reclaimed),531)
        for c in reclaimed:self.assertNotEqual(tiles[tuple(c)],3992,'Reclaimed construction space covered by a roof')
        directions=((1,0),(0,1),(-1,0),(0,-1))
        for actor in b.child('Actors').children:
            match=re.match(r'terrain\.rubberduck\.coast-native-([0-3])-\d-0\.bank$',actor.value)
            if not match:continue
            c=tuple(map(int,actor.child('Location').value.split(',')));dx,dy=directions[int(match[1])]
            self.assertEqual(tiles[c],3992)
            self.assertNotEqual(tiles.get((c[0]+dx,c[1]+dy)),3992,'Internal face between continuous roof tiles')

if __name__=='__main__':unittest.main()
