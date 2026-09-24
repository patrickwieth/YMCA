import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


@unittest.skipUnless(importlib.util.find_spec('openpyxl'), 'Optional Excel dependency not installed')
class ExcelExportTests(unittest.TestCase):
    def test_roundtrip_formulas_and_overwrite_protection(self):
        from openpyxl import load_workbook
        from export_excel import build
        catalog = json.loads(Path(__file__).with_name('catalog.json').read_text())
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'test.xlsx'
            build(catalog, path)
            book = load_workbook(path)
            self.assertEqual(len(book.sheetnames), 7)
            self.assertEqual(book['Bausteine'].max_row, len(catalog['components']) + 1)
            self.assertEqual(book['Entwuerfe'].max_row, len(catalog['designs']) + 1)
            self.assertEqual(book['Entwuerfe']['Q2'].value, '=N2*Parameter!$B$2')
            self.assertEqual(book['AltNeu']['E2'].value, '=Entwuerfe!R2')
            self.assertEqual(book['Parameter']['B2'].value, 300)
            self.assertTrue(book.calculation.fullCalcOnLoad)
            for row in book['Entwuerfe'].iter_rows(min_row=2):
                for cell in row[12:25]:
                    self.assertEqual(cell.data_type, 'f')
            self.assertEqual(book['Preisfit'].max_row, 10)
            original = path.read_bytes()
            with self.assertRaises(FileExistsError):
                build(catalog, path)
            self.assertEqual(path.read_bytes(), original)


if __name__ == '__main__':
    unittest.main()
