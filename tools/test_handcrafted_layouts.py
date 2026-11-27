import json
import math
from pathlib import Path
import unittest

from convert_handcrafted_layouts import ROOT, miniyaml, to_cpos, to_mpos, transformed, translation, world, shortest_paths


class HandcraftedLayoutTests(unittest.TestCase):
    def test_native_audit_passes_for_all_three_references(self):
        reports=json.loads((ROOT/'art-source/handcrafted/native-layout-audit.json').read_text())
        self.assertEqual(len(reports),3)
        for r in reports:
            self.assertTrue(r['NativeGridRoundtrip'])
            self.assertTrue(r['NativeFlatWorldCoordinates'])
            self.assertEqual(r['MissingCurrentActorTypes'],[])

    def test_rectangular_and_square_layouts_roundtrip_without_stagger_loss(self):
        for bounds,size in [([16,16,96,96],[128,128]),([1,1,70,50],[72,52]),([1,1,128,128],[130,130]),([1,2,7,9],[11,13])]:
            offset=translation(bounds,size)
            points=[transformed((x,y),offset) for x in range(size[0]) for y in range(size[1])]
            memory=[to_mpos(p) for p in points]
            self.assertEqual(len(set(memory)),len(points))
            self.assertEqual([to_cpos(p) for p in memory],points)

    def test_all_source_fields_and_actor_initializers_survive(self):
        directory=ROOT/'art-source/handcrafted'
        originals=json.loads((directory/'originals/manifest.json').read_text(encoding='utf-8'))['maps']
        for m in originals:
            layout=json.loads((directory/'layouts'/(m['slug']+'.layout.json')).read_text(encoding='utf-8'))
            self.assertEqual(layout['cell_count'],m['playable_cells'])
            self.assertEqual(len(layout['cells']),m['playable_cells'])
            resources={}
            for row in layout['cells']:
                if row[9]:
                    r=resources.setdefault(str(row[9]),{'cells':0,'density_sum':0})
                    r['cells']+=1;r['density_sum']+=row[10]
                self.assertEqual(row[11],0)
            self.assertEqual(resources,m['resource_layers'])
            source=miniyaml((directory/'originals'/m['slug']/'map.yaml').read_text()).child('Actors').children
            self.assertEqual(len(source),len(layout['actors']))
            for old,new in zip(source,layout['actors']):
                self.assertEqual([n.json() for n in old.children],new['source_initializers'])
                self.assertEqual(old.key,new['id']);self.assertEqual(old.value,new['type'])
            self.assertIn('MUST remain unplayable',layout['exterior_policy'])

    def test_computed_route_costs_differ_only_by_native_integer_scale(self):
        audit=json.loads((ROOT/'art-source/handcrafted/layouts/audit.json').read_text(encoding='utf-8'))
        compared=0
        for m in audit:
            for p in m['paths']:
                before=p['source_weighted_cell_lengths'];after=p['target_weighted_cell_lengths']
                if before is None:
                    self.assertIsNone(after)
                else:
                    self.assertAlmostEqual(after/before,724*math.sqrt(2)/1024,places=9)
                    compared+=1
        self.assertEqual(compared,176)

    def test_blocked_and_narrow_routes_remain_blocked_or_connected(self):
        cells={(x,y):{'terrain':'Clear' if x!=2 or y==3 else 'Rock'} for x in range(5) for y in range(5)}
        offset=translation([0,0,5,5])
        target={transformed(p,offset):v for p,v in cells.items()}
        speeds={'Clear':100}
        a=shortest_paths(cells,(0,0),[(4,0)],speeds,False)[(4,0)]
        b=shortest_paths(target,transformed((0,0),offset),[transformed((4,0),offset)],speeds,True)[transformed((4,0),offset)]
        self.assertGreater(a,4)
        self.assertAlmostEqual(b/a,724*math.sqrt(2)/1024,places=9)
        cells[(2,3)]={'terrain':'Rock'}
        self.assertIsNone(shortest_paths(cells,(0,0),[(4,0)],speeds,False)[(4,0)])

    def test_source_node_values_and_children_are_both_preserved(self):
        node=miniyaml('Actors:\n\tA: mpspawn\n\t\tLocation: 3,4\n').child('Actors').child('A')
        self.assertEqual(node.value,'mpspawn')
        self.assertEqual(node.child('Location').value,'3,4')
        with self.assertRaises(ValueError):miniyaml('Actors:\n\t\tLocation: 3,4\n')


if __name__=='__main__':
    unittest.main()
