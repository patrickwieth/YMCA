"""Numerical PNG/bib/selection alignment checks; no artwork is generated or changed.

Run: python -m unittest discover -s tools -p test_allied_building_alignment.py
Requires Pillow. Mirrors the engine's SpriteAnchor.Bottom transform.
"""
from pathlib import Path
import unittest

from PIL import Image
from audit_allied_selection_heights import measure

ROOT = Path(__file__).resolve().parents[1]


def load(relative):
    # Extract only root/trait/field levels, not inheritance. MiniYaml is NOT YAML.
    # The engine utility separately resolves and validates the real definitions.
    result = {}
    root = section = None
    for line in (ROOT / relative).read_text(encoding="utf-8-sig").splitlines():
        if not line.strip() or line.lstrip().startswith("#") or ":" not in line:
            continue
        depth = len(line) - len(line.lstrip("\t"))
        key, _, value = line.strip().partition(":")
        value = value.strip()
        if depth == 0:
            root = result.setdefault(key, {})
            section = None
        elif depth == 1 and root is not None:
            root[key] = value if value else {}
            section = root[key] if isinstance(root[key], dict) else None
        elif depth == 2 and section is not None:
            section[key] = value
    return result


def load_sequences():
    result = load("mods/ca/sequences/allies.yaml")
    for image, states in load("mods/ca/sequences/allied-building-placement.yaml").items():
        for state, fields in states.items():
            result.setdefault(image, {}).setdefault(state, {}).update(fields)
    return result


class AlliedBuildingAlignmentTests(unittest.TestCase):
    def test_overridden_sequence_roots_have_unique_merge_keys(self):
        manifest = (ROOT / "mods/ca/mod.yaml").read_text(encoding="utf-8-sig").splitlines()
        files = []
        for line in manifest[manifest.index("Sequences:") + 1:]:
            if line.startswith("\t"):
                files.append(line.strip())
            elif line.strip():
                break
        self.assertEqual(files[-1], "ca|sequences/allied-building-placement.yaml")
        overridden = set(load("mods/ca/sequences/allied-building-placement.yaml"))
        for filename in ("structures.yaml", "allies.yaml", "soviet.yaml"):
            root, seen = None, set()
            for line in (ROOT / "mods/ca/sequences" / filename).read_text(encoding="utf-8-sig").splitlines():
                if not line.strip() or line.lstrip().startswith("#") or ":" not in line:
                    continue
                depth = len(line) - len(line.lstrip("\t"))
                key = line.strip().split(":", 1)[0]
                if depth == 0:
                    root, seen = key, set()
                elif depth == 1 and root in overridden:
                    with self.subTest(file=filename, image=root, key=key):
                        self.assertNotIn(key, seen, "Cross-file overrides require unique keys; name multiple Inherits entries.")
                    seen.add(key)

    def test_source_art_offsets_against_baseline_registration(self):
        sequences = load_sequences()
        # Original registration references, independent of later selection-height corrections.
        baseline_heights = {"PBOX": 76, "PRIS": 143, "GAP": 122, "POWR": 42,
                            "APWR": 74, "TENT": 48, "PROC": 44, "OREP": 112,
                            "WEAT": 75, "ATEK": 78, "HPAD": 118, "DOME": 93}
        entries = (
            ("PBOX", "pbox.allies", 1), ("PRIS", "pris.allies", 1),
            ("GAP", "gap.allies", 2), ("POWR", "powr.allies", 2),
            ("APWR", "apwr.allies", 2),
            ("PROC", "proc.allies", 3),
            ("OREP", "orep.allies", 2),
            ("WEAT", "weat.allies", 2),
            ("ATEK", "atek.allies", 2), ("HPAD", "hpad.allies", 2),
            ("DOME", "dome.allies", 2),
        )
        for actor, image, size in entries:
            with self.subTest(actor=actor):
                bib_size = 3 if size == 2 and actor != "GAP" else size
                bib = sequences[f"alliedbib{bib_size}x{bib_size}"]["idle"]
                with Image.open(ROOT / "mods/ca" / bib["Filename"]) as foundation:
                    self.assertEqual(foundation.size, (128 * bib_size, 64 * bib_size))
                # Art is measured relative to its body center, not the shifted full-bib center.
                floor_front = 32 * size
                floor_back = -32 * size
                art_shift = {"PBOX": -12, "PRIS": -12, "OREP": 22, "PROC": -16,
                             "APWR": 12, "DOME": 0, "POWR": -12, "ATEK": -8,
                             "WEAT": -6, "HPAD": 7}.get(actor, -24 if size == 2 else 0)
                decoration_height = baseline_heights[actor]
                for state, seq in sequences[image].items():
                    if not isinstance(seq, dict) or "Filename" not in seq:
                        continue
                    if "/buildings/" not in seq["Filename"]:
                        continue
                    with self.subTest(state=state):
                        self.assertEqual(float(seq.get("Scale", 1)), 1)
                        self.assertEqual(seq["Anchor"], "Bottom")
                        y = float(str(seq.get("Offset", "0,0")).split(",")[1])
                        with Image.open(ROOT / "mods/ca" / seq["Filename"]) as art:
                            bounds = art.getchannel("A").getbbox()
                            # Frame center + Bottom anchor + configured pixel offset.
                            translation = -art.height + art.width / 4 + y
                            # Current supplied replacements have shorter alpha bases than the original registration.
                            front_inset = {"PROC": 4, "POWR": 5, "WEAT": 19, "DOME": 31, "OREP": 44}.get(actor, 0)
                            self.assertAlmostEqual(bounds[3] + translation, floor_front + art_shift - front_inset, delta=0.6)
                            self.assertAlmostEqual(bounds[1] + translation,
                                                   floor_back - decoration_height + art_shift, delta=0.6)

    def test_driveable_bib_registration(self):
        template = load("mods/ca/rules/allies/defaults.yaml")["^Allied2x2DriveableBib"]
        self.assertEqual(template["Building"]["Dimensions"], "3,3")
        rows = template["Building"]["Footprint"].split()
        self.assertEqual(rows, ["xx=", "xx=", "==="])
        self.assertEqual("".join(rows).count("x"), 4)
        self.assertEqual("".join(rows).count("="), 5)
        # Body center is 32 projected pixels above the complete bib center.
        self.assertEqual(template["Building"]["LocalCenterOffset"], "0,-724,0")
        self.assertEqual(template["WithIdleOverlay@ALLIEDBIB"]["Offset"], "-1134,0,0")

    def test_bib_offsets_are_body_local_not_world_space(self):
        # Standing buildings use BodyOrientation's default pitch: engine integer Sin(40deg)=654.
        # The C# audit also runs the ACTUAL resolved BodyOrientationInfo.LocalToWorld method.
        def local_to_world(value):
            x, y, z = map(int, value.split(","))
            return y, int(-654 * x / 1024), z

        template = load("mods/ca/rules/allies/defaults.yaml")["^Allied2x2DriveableBib"]
        self.assertEqual(local_to_world(template["WithIdleOverlay@ALLIEDBIB"]["Offset"]), (0, 724, 0))
        # The old erroneous value shifts the bib RIGHT by 64 pixels, not down by 32.
        self.assertEqual(local_to_world("0,724,0"), (724, 0, 0))
        for path, actor in (("mods/ca/rules/structures.yaml", "AFLD.allies"),):
            bib = load(path)[actor]["WithIdleOverlay@ALLIEDBIB"]
            self.assertEqual(local_to_world(bib["Offset"]), (0, 724, 0))

    def test_refinery_lower_box_edge_trim_preserves_top(self):
        selection = load("mods/ca/rules/allies/structures.yaml")["PROC"]["IsometricSelectable"]
        width, depth, x, y = map(int, selection["DecorationBounds"].split(","))
        self.assertEqual(width, 4344)
        self.assertEqual(x, 0)
        # Match the engine's integer projection (truncates toward zero).
        top = -int(depth * 64 / (2 * 1448)) + int(y * 64 / 1448)
        bottom = top + int(depth * 64 / 1448)
        self.assertEqual(top, -96)
        self.assertEqual(bottom, 88)
        self.assertEqual(selection["DecorationHeight"], "55")

    def test_selection_height_diagnostic_detects_short_gap_box(self):
        seq = load_sequences()["gap.allies"]["idle"]
        entry = {"Actor": "gap", "Scale": seq.get("Scale", "1"), "File": seq["Filename"],
                 "Anchor": seq["Anchor"], "Offset": seq["Offset"], "SelectionTop": -186}
        top, increase = measure(entry)
        self.assertEqual(top, -210)
        self.assertEqual(increase, 24)
        entry["SelectionTop"] = -210
        self.assertEqual(measure(entry)[1], 0)
        entry["SelectionTop"] = -220
        self.assertEqual(measure(entry)[1], 0)  # Never shrink a taller box automatically.

    def test_chronosphere_2x2_body(self):
        rules = load("mods/ca/rules/allies/structures.yaml")["PDOX"]
        self.assertEqual(rules["Inherits@visuals"], "^Allied2x2DriveableBib")
        self.assertNotIn("Building", rules)
        self.assertNotIn("WithIdleOverlay@ALLIEDBIB", rules)
        self.assertNotIn("-DecorationBounds", rules["IsometricSelectable"])
        self.assertEqual(rules["IsometricSelectable"]["DecorationHeight"], "105")
        sequences = load_sequences()["pdox.allies"]
        for state in ("idle", "damaged-idle", "active", "damaged-active"):
            seq = sequences[state]
            self.assertEqual(seq["Offset"], "-4,35")
            self.assertEqual(float(seq.get("Scale", 1)), 1)
            self.assertEqual(seq["Anchor"], "Bottom")
            with Image.open(ROOT / "mods/ca" / seq["Filename"]) as art:
                bounds = art.getchannel("A").getbbox()
                translation = -art.height + art.width / 4 + 35
                # 2x2 decoration footprint has a back corner 64px above its center.
                height = int(rules["IsometricSelectable"]["DecorationHeight"])
                # Box raised 21px (84 -> 105); subsequent art-only move is 20px down.
                self.assertAlmostEqual(bounds[1] + translation, -64 - height + 41, delta=0.6)

    def test_barracks_3x2_body_and_cropped_art(self):
        rules = load("mods/ca/rules/structures.yaml")["TENT"]
        building = rules["Building"]
        self.assertEqual(building["Dimensions"], "3,3")
        self.assertEqual(building["Footprint"].split(), ["xxx", "xxx", "==="])
        self.assertEqual(building["LocalCenterOffset"], "362,-362,0")
        self.assertIn("-DecorationBounds", rules["IsometricSelectable"])
        self.assertEqual(rules["WithIdleOverlay@ALLIEDBIB"]["Offset"], "-567,-362,0")
        # Actor center moves (362,362) world = (32,16) screen; bib world center stays fixed.
        self.assertEqual((362 * 128 / 1448, 362 * 64 / 1448), (32, 16))
        for state in ("idle", "damaged-idle"):
            seq = load_sequences()["tent.allies"][state]
            self.assertEqual(seq["Offset"], "4,-7")
            self.assertEqual(float(seq.get("Scale", 1)), 1)
            with Image.open(ROOT / "mods/ca" / seq["Filename"]) as art:
                # User cropped away the large transparent canvas: no extra 72px correction.
                self.assertEqual(art.size, (217, 146))
                bounds = art.getchannel("A").getbbox()
                translation = -art.height + art.width / 4 - 7
                self.assertAlmostEqual(bounds[3] + translation, 46.25, delta=0.6)

    def test_airfield_decoration_enlargement_does_not_change_body(self):
        rules = load("mods/ca/rules/structures.yaml")["AFLD.allies"]
        selection = rules["IsometricSelectable"]
        self.assertEqual(selection["DecorationFootprint"], "4,2")
        self.assertEqual(selection["DecorationFootprintExtraDepth"], "181")
        self.assertEqual(selection["DecorationHeight"], "83")
        self.assertIn("-DecorationBounds", selection)
        self.assertNotIn("Bounds", selection)
        self.assertEqual(rules["Building"]["Footprint"].split(), ["xxx=", "xxx=", "===="])

    def test_4x4_bibs_preserve_body_pivots(self):
        rules = load("mods/ca/rules/allies/structures.yaml")
        for actor, previous in (("FACT", (-362, 1086)),):
            with self.subTest(actor=actor):
                building = rules[actor]["Building"]
                self.assertEqual(building["Dimensions"], "4,4")
                dx, dy, _ = map(int, building["LocalCenterOffset"].split(","))
                self.assertEqual((dx, 2172 + dy), previous)
                bib = rules[actor]["WithIdleOverlay@ALLIEDBIB"]
                self.assertEqual(bib["Image"], "alliedbib4x4")
                lx, ly, _ = map(int, bib["Offset"].split(","))
                # Full bib centers on the 4x4 site despite the preserved Conyard pivot.
                self.assertEqual((dx + ly, 2172 + dy + int(-654 * lx / 1024)), (0, 2172))
                self.assertIn("-WithBuildingBib", rules[actor])
        self.assertEqual(rules["FACT"]["Building"]["Footprint"].split(), ["xxX=", "xxx=", "XxX=", "===="])
        seq = load_sequences()["alliedbib4x4"]["idle"]
        self.assertEqual(seq["Anchor"], "Bottom")
        self.assertEqual(seq["Scale"], "1")
        self.assertEqual(seq["ZOffset"], "-4096")
        with Image.open(ROOT / "mods/ca" / seq["Filename"]) as art:
            self.assertEqual(art.size, (512, 256))

    def test_4x3_bibs_preserve_body_pivots(self):
        for path, actor, previous, footprint in (
            ("mods/ca/rules/allies/structures.yaml", "WEAP", (724, 1448), ["xxxx", "xxxx", "===="]),
            ("mods/ca/rules/structures.yaml", "AFLD.allies", (362, 1086), ["xxx=", "xxx=", "===="]),
        ):
            with self.subTest(actor=actor):
                rules = load(path)[actor]
                building = rules["Building"]
                self.assertEqual(building["Dimensions"], "4,3")
                self.assertEqual(building["Footprint"].split(), footprint)
                dx, dy, _ = map(int, building["LocalCenterOffset"].split(","))
                center = (362 + dx, 1810 + dy)
                self.assertEqual(center, previous)
                bib = rules["WithIdleOverlay@ALLIEDBIB"]
                self.assertEqual(bib["Image"], "alliedbib4x3")
                lx, ly, _ = map(int, bib["Offset"].split(","))
                self.assertEqual((center[0] + ly, center[1] + int(-654 * lx / 1024)), (362, 1810))
        seq = load_sequences()["alliedbib4x3"]["idle"]
        self.assertEqual(seq["Anchor"], "Bottom")
        self.assertEqual(seq["Scale"], "1")
        self.assertEqual(seq["ZOffset"], "-4096")
        with Image.open(ROOT / "mods/ca" / seq["Filename"]) as art:
            self.assertEqual(art.size, (447, 224))  # Supplied canvas is deliberately not resampled.

    def test_factory_rear_body_center_excludes_front_apron(self):
        rules = load("mods/ca/rules/allies/structures.yaml")
        building = rules["WEAP"]["Building"]
        self.assertEqual(building["Dimensions"], "4,3")
        self.assertEqual(building["Footprint"].split(), ["xxxx", "xxxx", "===="])
        offset = tuple(map(int, building["LocalCenterOffset"].split(",")))
        # Building.CenterOffset for a 4x3 site plus the configured correction.
        center = (362 + offset[0], 1810 + offset[1])
        # Mean center of the occupied 4x2 body, relative to its origin cell.
        self.assertEqual(center, (724, 1448))
        self.assertNotIn("DecorationBounds", rules["WEAP"]["IsometricSelectable"])


if __name__ == "__main__":
    unittest.main()
