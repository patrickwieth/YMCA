import re
import unittest
from unittest.mock import patch

from designer_english_labels import LABELS
from export_designer_catalog import data


class DesignerEnglishLabelsTests(unittest.TestCase):
    def test_all_runtime_component_labels_are_english(self):
        parts = data()['components']
        for id, label in LABELS.items():
            self.assertEqual(parts[id]['display_name'], label)
        for id, part in parts.items():
            label = part.get('display_name', '')
            self.assertTrue(label, id)
            self.assertIsNone(re.search(r'[äöüß]|Waffen|Panzer|Rumpf|Fahrwerk|Einbaugruppe|Spreng|Geschuetz|Schwere|Schweres|Leichte|Leichtes|Boden|Luft|Doppel|Schreit|Dieselmotor|Entladung|Mammut', label, re.I), (id, label))
        self.assertEqual(parts['tracks-standard']['display_name'], 'Heavy Tank Treads')
        self.assertEqual(parts['diesel']['display_name'], 'Standard Diesel Engine')
        self.assertEqual(parts['heavy']['display_name'], 'Heavy Armor')
        self.assertEqual(parts['designer-mammoth-hull']['display_name'], 'Mammoth - Bound Assembly')

    def test_localization_changes_only_display_names_not_ids_or_calibration(self):
        with patch('export_designer_catalog.apply_english_labels', lambda parts: None):
            before = data()
        after = data()
        self.assertEqual(set(before['components']), set(after['components']))
        for catalog in (before, after):
            for part in catalog['components'].values(): part.pop('display_name', None)
        self.assertEqual(before, after)


if __name__ == '__main__':
    unittest.main()
