"""Regression checks for installed handcrafted packages and immutable references."""
import hashlib
import json
import struct
import unittest
import zipfile
from pathlib import Path
from PIL import Image

from convert_handcrafted_layouts import ROOT, miniyaml, integer_list


class HandcraftedPackageTests(unittest.TestCase):
    def test_installed_maps_preserve_every_source_cell_and_original_actor(self):
        layouts=ROOT/'art-source/handcrafted/layouts'
        uids=set()
        manifest=json.loads((ROOT/'art-source/handcrafted/packages/validation.json').read_text())
        self.assertEqual(len(manifest),3)
        for entry in manifest:
            slug=entry['slug'];layout=json.loads((layouts/(slug+'.layout.json')).read_text(encoding='utf-8'))
            path=ROOT/'mods/ca/maps'/('handcrafted-'+slug+'.oramap')
            self.assertEqual(path.read_bytes(),(ROOT/'art-source/handcrafted/packages'/path.name).read_bytes())
            with zipfile.ZipFile(path) as archive:
                self.assertIsNone(archive.testzip())
                text=archive.read('map.yaml').decode();data=miniyaml(text);binary=archive.read('map.bin')
                self.assertIn('Handcrafted Maps',data.child('Categories').value)
                self.assertEqual(data.child('Tileset').value,'RUBBERDUCK-TEMPERATE')
                self.assertNotIn('TerrainCalibrationView',text)
                self.assertNotIn('HandcraftedRuntimeProbe',text)
                self.assertNotIn('MapRevealPerformanceProbe',text)
                self.assertNotIn('FactoryAirshipProbe',text)
                self.assertIn('HandcraftedMapSupport',text)
                self.assertNotIn('REVIEW ONLY',data.child('Title').value)
                original=layout['source_bounds'];offset=layout['cell_translation']
                self.assertEqual(integer_list(data.child('CellBounds').value),[original[0]+offset[0],original[1]+offset[1],original[2],original[3]])
                overrides={tuple(integer_list(n.key)):n.value for n in data.child('BaseTerrainOverrides').children}
                self.assertEqual(len(overrides),layout['cell_count'])
                version,width,height=struct.unpack_from('<BHH',binary)
                self.assertEqual(version,2)
                _,height_offset,resource_offset=struct.unpack_from('<III',binary,5)
                reclaimed={tuple(c) for c in entry.get('ReclaimedRockCells',[])}
                self.assertTrue(not reclaimed or slug=='all-that-glitters-barren')
                actual_reclaimed=set()
                for row in layout['cells']:
                    expected=row[6]
                    if (row[2],row[3]) in reclaimed:
                        self.assertEqual(row[6],'Rock')
                        self.assertEqual(row[9],0)
                        expected='Clear';actual_reclaimed.add((row[2],row[3]))
                    self.assertEqual(overrides[row[2],row[3]],expected)
                    index=row[4]*height+row[5]
                    self.assertEqual(tuple(binary[resource_offset+index*2:resource_offset+index*2+2]),tuple(row[9:11]))
                    self.assertEqual(binary[height_offset+index],0)
                self.assertEqual(actual_reclaimed,reclaimed)
                if slug=='two-sides-naval':
                    self.assertNotIn('terrain.rubberduck.coast-',text)
                    self.assertIn('terrain.handcrafted.bank',text)
                actors=data.child('Actors')
                for actor in layout['actors']:
                    stored=actors.child(actor['id']);self.assertEqual(stored.value,actor['type'])
                    values={n.key:n.value for n in stored.children}
                    for init in actor['source_initializers']:
                        expected=','.join(map(str,actor['target_cell'])) if init['key']=='Location' else init['value']
                        self.assertEqual(values[init['key']],expected)
                # Same SHA-1 rule as native Map.ComputeUID, including entry order.
                digest=hashlib.sha1()
                for name in archive.namelist():
                    if name.endswith(('.yaml','.bin','.lua')) or name=='map.png':digest.update(archive.read(name))
                self.assertEqual(digest.hexdigest(),entry['Uid'])
                uids.add(entry['Uid'])
        self.assertEqual(len(uids),3)

    def test_dune_boundary_roof_preserves_gameplay(self):
        old=ROOT/'art-source/handcrafted/history/before-coast-and-space-refinement/handcrafted-dune-patrol.oramap'
        current=ROOT/'mods/ca/maps/handcrafted-dune-patrol.oramap'
        with zipfile.ZipFile(old) as z,zipfile.ZipFile(current) as n:
            before=z.read('map.bin');after=n.read('map.bin')
            old_yaml=miniyaml(z.read('map.yaml').decode());new_yaml=miniyaml(n.read('map.yaml').decode())
        def tree(node):return (node.key,node.value,tuple(tree(c) for c in node.children))
        self.assertEqual(tree(old_yaml.child('BaseTerrainOverrides')),tree(new_yaml.child('BaseTerrainOverrides')))
        old_height=struct.unpack_from('<I',before,9)[0];new_height=struct.unpack_from('<I',after,9)[0]
        self.assertEqual(before[old_height:],after[new_height:])
        layout=json.loads((ROOT/'art-source/handcrafted/layouts/dune-patrol.layout.json').read_text())
        _,width,height=struct.unpack_from('<BHH',after);tiles=struct.unpack_from('<I',after,5)[0]
        # The continuous raised background must reach every cell of the source's top edge.
        top=layout['source_bounds'][1]
        for r in layout['cells']:
            if r[1]==top:self.assertEqual(struct.unpack_from('<H',after,tiles+3*(r[4]*height+r[5]))[0],3992)
        body=new_yaml.child('Rules').child('^RubberduckBankAlignedRearCoast').child('RockCoastBody')
        self.assertEqual(body.child('ClipToMap').value,'true')

    def test_low_bank_rgb_comes_only_from_bounded_native_rock_faces(self):
        folder=ROOT/'mods/ca/bits/terrain/rubberduck/derived/handcrafted/banks'
        data=json.loads((folder/'provenance.json').read_text())
        source=ROOT/'mods/ca'/data['source']
        self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),data['sha256'])
        with Image.open(source) as raw:
            atlas=raw.convert('RGBA');colors=set()
            for slot in data['slots']:
                colors.update(p[:3] for p in atlas.crop((slot*128+16,75,slot*128+96,161)).getdata())
        images=list(folder.glob('bank-*.png'));self.assertEqual(len(images),45)
        for path in images:
            with Image.open(path) as image:
                self.assertEqual(image.size,(192,128))
                self.assertTrue(all(p[:3] in colors for p in image.getdata() if p[3]))

    def test_previous_handcrafted_uids_are_archived_not_replaced(self):
        folder=ROOT/'art-source/handcrafted/history/before-coast-and-space-refinement'
        for entry in json.loads((folder/'validation.json').read_text()):
            archived=ROOT/'mods/ca/development-maps'/('handcrafted-history-'+entry['Uid']+'.oramap')
            self.assertEqual(archived.read_bytes(),(folder/('handcrafted-'+entry['slug']+'.oramap')).read_bytes())

    def test_closed_corner_faces_use_native_translated_pixels(self):
        folder=ROOT/'mods/ca/bits/terrain/rubberduck/derived/handcrafted/closed-corners'
        for record in json.loads((folder/'provenance.json').read_text()):
            base=ROOT/'mods/ca'/record['corner'];face=ROOT/'mods/ca'/record['face']
            self.assertEqual(hashlib.sha256(base.read_bytes()).hexdigest(),record['corner_sha256'])
            self.assertEqual(hashlib.sha256(face.read_bytes()).hexdigest(),record['face_sha256'])
            with Image.open(base) as b,Image.open(face) as f,Image.open(folder/record['file']) as result:
                expected=b.convert('RGBA');donor=f.convert('RGBA');dx,dy=record['translation']
                for y in range(256):
                    for x in range(128):
                        top=96+x/2 if dx<0 else 160-x/2
                        if (x<64 if dx<0 else x>=64) and top<=y<top+64:
                            pixel=donor.getpixel((x-dx,y-dy))
                            if pixel[3]:expected.putpixel((x,y),pixel)
                self.assertEqual(expected.tobytes(),result.convert('RGBA').tobytes())

    def test_boulders_are_unmodified_bounded_source_cuts(self):
        folder=ROOT/'mods/ca/bits/terrain/rubberduck/derived/handcrafted'
        provenance=json.loads((folder/'provenance.json').read_text())
        source=ROOT/'mods/ca'/provenance['source']
        self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),provenance['source_sha256'])
        image=Image.open(source).convert('RGBA')
        for cut in provenance['cuts']:
            expected=image.crop(cut['frame']).crop(cut['alpha_bounds'])
            with Image.open(folder/cut['file']) as actual:self.assertEqual(actual.convert('RGBA').tobytes(),expected.tobytes())

    def test_original_glitters_oil_islands_are_not_opened_up(self):
        layout=json.loads((ROOT/'art-source/handcrafted/layouts/all-that-glitters-barren.layout.json').read_text())
        self.assertEqual(len(layout['oil_ground_access']),5)
        for oil in layout['oil_ground_access']:
            self.assertEqual(oil['from_starts'],[False]*8)

    def test_source_actor_gameplay_traits_are_not_overridden(self):
        # Scenery changes can only select the proper sprite palette; never edit footprints.
        for path in (ROOT/'mods/ca/maps').glob('handcrafted-*.oramap'):
            with zipfile.ZipFile(path) as archive:
                rules=miniyaml(archive.read('map.yaml').decode()).child('Rules')
                for actor in rules.children:
                    if actor.key.startswith(('T0','T1','TC')):
                        self.assertEqual([n.key for n in actor.children],['RenderSprites'])
                        self.assertEqual([n.key for n in actor.child('RenderSprites').children],['Palette'])


if __name__=='__main__':
    unittest.main()
