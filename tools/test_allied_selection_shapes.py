"""Synthetic in-memory geometry tests; never writes or changes supplied artwork."""
import unittest
from unittest.mock import patch

from PIL import Image

from audit_allied_selection_shapes import measure


class SelectionShapeTests(unittest.TestCase):
    def measure_offset(self, offset):
        entry = {
            "Scale": "1", "Anchor": "Center", "File": "unused.png", "Offset": offset,
            "SelectionPolygon": [[0, -32], [-32, -16], [-32, 16], [0, 32], [32, 16], [32, -16]],
        }
        with patch("audit_allied_selection_shapes.Image.open", return_value=Image.new("RGBA", (32, 32), "white")):
            return measure(entry)

    def test_contained_silhouette(self):
        result = self.measure_offset("0,0")
        self.assertEqual(result["outside"], 0)
        self.assertEqual(result["center_delta"], 0)
        self.assertEqual(result["box_width"], 64)

    def test_roof_and_base_mismatch_are_distinct(self):
        above = self.measure_offset("0,-60")
        below = self.measure_offset("0,60")
        self.assertEqual((above["outside"], above["roof_outside"], above["base_outside"]), (100, 100, 0))
        self.assertEqual((below["outside"], below["roof_outside"], below["base_outside"]), (100, 0, 100))

    def test_horizontal_mismatch(self):
        result = self.measure_offset("100,0")
        self.assertEqual(result["outside"], 100)
        self.assertEqual(result["center_delta"], 100)
        self.assertGreater(result["right_over"], 0)
        self.assertEqual(result["left_over"], 0)


if __name__ == "__main__":
    unittest.main()
