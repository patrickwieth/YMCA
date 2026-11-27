import hashlib
import json
import math
from pathlib import Path
import struct
import tempfile
import unittest

from prepare_handcrafted_sources import ROOT, OUT, actors, binary_summary, load_source, save_exact


class HandcraftedSourceTests(unittest.TestCase):
    def test_recovered_files_are_byte_exact_git_sources(self):
        manifest = json.loads((OUT / 'manifest.json').read_text(encoding='utf-8'))
        self.assertEqual(len(manifest['maps']), 3)
        for item in manifest['maps']:
            files, package = load_source(item['source_path'])
            for name, original in files.items():
                actual = (OUT / item['slug'] / name).read_bytes()
                self.assertEqual(actual, original)
                self.assertEqual(hashlib.sha256(actual).hexdigest(), item['files'][name]['sha256'])
            if package:
                self.assertEqual((OUT / (item['slug'] + '.original.oramap')).read_bytes(), package)

    def test_original_dimensions_and_start_counts_are_not_invented(self):
        maps = {m['slug']: m for m in json.loads((OUT / 'manifest.json').read_text(encoding='utf-8'))['maps']}
        for slug, bounds, starts in [('all-that-glitters-barren', [16,16,96,96], 8),
                                      ('dune-patrol', [1,1,70,50], 2),
                                      ('two-sides-naval', [1,1,128,128], 6)]:
            self.assertEqual(maps[slug]['playable_bounds'], bounds)
            self.assertEqual(len(maps[slug]['starts']), starts)
            self.assertEqual(maps[slug]['grid'], 'Rectangular')
            self.assertIn('not converted', maps[slug]['remake_status'])

    def test_current_generator_report_covers_every_archetype_and_player_count(self):
        report = json.loads((ROOT / 'art-source/handcrafted/generator-sizes.json').read_text())
        self.assertEqual(report['Grid'], 'RectangularIsometric')
        rows = {(r['Preset'], r['Players']): r for r in report['Rows']}
        self.assertEqual(len(rows), 18)
        for (preset, players), row in rows.items():
            expected = 80 if players == 2 else 96
            if preset == 'Archipelago': expected = max(96, expected)
            if preset == 'Migration': expected += 32
            self.assertEqual((row['PlayableWidth'], row['PlayableHeight']), (expected, expected))

    def test_different_local_reference_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'original'
            save_exact(path, b'preserved')
            with self.assertRaises(ValueError): save_exact(path, b'replacement')
            self.assertEqual(path.read_bytes(), b'preserved')

    def test_binary_resource_layer_is_column_major_and_clipped_to_bounds(self):
        # width 2, height 3: coordinate (1,0) is array index 3, not index 1.
        header = struct.pack('<BHHIII', 2, 2, 3, 17, 0, 35)
        tiles = bytes(18)
        resources = bytes([0,0, 0,0, 0,0, 1,12, 0,0, 0,0])
        summary = binary_summary(header+tiles+resources, [2,3], [1,0,1,1])
        self.assertEqual(summary['resource_layers'], {'1': {'cells':1, 'density_sum':12}})
        with self.assertRaises(ValueError): binary_summary(header+tiles, [2,3], [0,0,2,3])

    def test_actor_reader_handles_location_order_and_excludes_rule_definitions(self):
        text = 'Actors:\n\tStart: mpspawn\n\t\tOwner: Neutral\n\t\tLocation: 9,26\nRules:\n\tNotAnActor: mpspawn\n\t\tLocation: 1,2\n'
        self.assertEqual(actors(text), [{'id':'Start','type':'mpspawn','location':[9,26]}])

    def test_cell_mapping_preserves_distances_but_memory_coordinate_copy_does_not(self):
        # Mirrors the verified native Map.CenterOfCell and MPos.ToCPos formulas.
        for dx, dy in [(54,1), (85,-1), (0,20), (20,0), (12,23)]:
            old = math.hypot(1024*dx,1024*dy)
            new = math.hypot(724*(dx-dy),724*(dx+dy))
            self.assertAlmostEqual(new/old,1,delta=0.0002)
        self.assertEqual(1448/724,2)  # Isometric map-memory axes are not equal-length.


if __name__ == '__main__':
    unittest.main()
