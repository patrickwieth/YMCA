"""Source-integrity checks, not runtime or artistic acceptance of the urban kit."""
import hashlib
import json
import unittest
from unittest.mock import patch
from pathlib import Path

from PIL import Image

from prepare_urban_source_cuts import CUTS, SOURCE_HASHES, extract, source_image, source_reference
from prepare_urban_road_core import DIRECTIONS, mark_road, road_mask

ROOT = Path(__file__).resolve().parents[1]


class UrbanSourceCutsTests(unittest.TestCase):
    def test_native_source_pixels_and_alpha(self):
        records = json.loads((ROOT / 'art-source/urban-warfare/cutout-catalog.json').read_text())
        self.assertTrue(records)
        for record in records:
            with self.subTest(asset=record['id']):
                source_path = ROOT / 'mods/ca/bits/misc/buildings' / record['source']
                self.assertEqual(hashlib.sha256(source_path.read_bytes()).hexdigest(),
                                 record['source_sha256'])
                reference, sources = source_reference(record)
                self.assertEqual(sources, record['sources'])
                source = reference.crop(record['crop'])
                cut = Image.open(ROOT / record['output']).convert('RGBA')
                self.assertEqual(cut.size, source.size)
                self.assertEqual(list(cut.size), record['size'])
                self.assertIsNotNone(cut.getbbox())
                for pixel, original in zip(cut.getdata(), source.getdata()):
                    if pixel[3]:
                        self.assertEqual(pixel, original)
                x, y = record['ground_in_cutout']
                self.assertTrue(0 <= x < cut.width and 0 <= y < cut.height)

    def test_reproducible_cutouts(self):
        for spec in CUTS:
            with self.subTest(asset=spec['id']):
                cut, _, _ = extract(spec)
                installed = Image.open(ROOT / 'art-source/urban-warfare/cutouts' / (spec['id']+'.png'))
                self.assertEqual(cut.size, installed.size)
                self.assertEqual(cut.tobytes(), installed.tobytes())

    def test_replaced_source_requires_review(self):
        name = CUTS[0]['source']
        with patch.dict(SOURCE_HASHES, {name:'not-the-inventoried-source'}):
            with self.assertRaisesRegex(ValueError, 'inventory/review'):
                source_image(name)

    def test_repair_needs_provenance(self):
        spec = dict(CUTS[0], repairs=[{'source':CUTS[0]['source'], 'crop':[0,0,1,1], 'at':[0,0]}])
        with self.assertRaisesRegex(ValueError, 'explanation'):
            source_reference(spec)

    def test_road_source_pixels_and_snow_alpha(self):
        directory = ROOT / 'art-source/urban-warfare/road-core'
        info = json.loads((directory/'provenance.json').read_text())
        for name, digest in info['sources'].items():
            self.assertEqual(hashlib.sha256((ROOT/name).read_bytes()).hexdigest(), digest)
        donor = Image.open(ROOT/'mods/ca/bits/misc/buildings/paved_roads-991488041-0.png').convert('RGBA')
        patch = donor.crop(info['asphalt_crop'])
        plane = Image.open(directory/'asphalt-source-plane.png').convert('RGBA')
        for y in range(64):
            for x in range(128):
                self.assertEqual(plane.getpixel((x,y)), patch.getpixel((x%16,y%16)))
        for connections in range(16):
            road = Image.open(directory/f'road-{connections:02x}.png').convert('RGBA')
            self.assertEqual(road.getchannel('A').tobytes(), road_mask(connections).tobytes())
            for pixel, expected in zip(road.getdata(), plane.getdata()):
                if pixel[3]:
                    self.assertEqual(pixel[:3], expected[:3])
        sand = Image.open(ROOT/'mods/ca/bits/terrain/rubberduck/sand_base.png').convert('RGBA')
        snow = Image.open(directory/'snow-from-sand.png').convert('RGBA')
        self.assertEqual(sand.size, snow.size)
        self.assertEqual(sand.getchannel('A').tobytes(), snow.getchannel('A').tobytes())

    def test_source_markings_reproduce_without_changing_road_coverage(self):
        directory = ROOT/'art-source/urban-warfare/road-core'
        catalog = {r['id']:r for r in json.loads((ROOT/'art-source/urban-warfare/cutout-catalog.json').read_text())}
        colors = set()
        for name in ['street-dash-fall-east','street-dash-rise-east','crosswalk-stripe-rise-east']:
            sprite = Image.open(ROOT/catalog[name]['output']).convert('RGBA')
            colors.update(p[:3] for p in sprite.getdata() if p[3])
        for connections,crossing,suffix in [(5,False,'05'),(10,False,'0a'),(5,True,'05-crosswalk')]:
            core = Image.open(directory/f'road-{connections:02x}.png').convert('RGBA')
            marked, _ = mark_road(core,connections,catalog,crossing)
            saved = Image.open(directory/f'marked-road-{suffix}.png').convert('RGBA')
            self.assertEqual(marked.tobytes(),saved.tobytes())
            self.assertEqual(marked.getchannel('A').tobytes(),core.getchannel('A').tobytes())
            for pixel,base in zip(marked.getdata(),core.getdata()):
                if pixel[3] and pixel[:3] != base[:3]:
                    self.assertIn(pixel[:3],colors)

    def test_road_connections_have_no_pixel_gap(self):
        for bit, (du,dv) in enumerate(DIRECTIONS):
            canvas = Image.new('L', (384,192))
            a = road_mask(1 << bit)
            b = road_mask(1 << ((bit+2)%4))
            canvas.paste(a,(128,64))
            dx,dy = (du-dv)*64,(du+dv)*32
            canvas.paste(b,(128+dx,64+dy),b)
            # A narrow center corridor crosses the shared tile edge uninterrupted.
            for step in range(65):
                for side in (-2,0,2):
                    x = round(192 + dx*step/64 + side)
                    y = round(96 + dy*step/64)
                    self.assertEqual(canvas.getpixel((x,y)),255,(bit,step,side))

    def test_preview_source_copy(self):
        preview = ROOT / 'art-source/urban-warfare/sources/map-preview.png'
        self.assertEqual(hashlib.sha256(preview.read_bytes()).hexdigest(),
                         'ed8e29d4e22588d6f918623fc388078ae6b465ea3ba072e39be950d68c522c8f')


if __name__ == '__main__':
    unittest.main()
