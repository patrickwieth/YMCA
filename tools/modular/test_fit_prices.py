import json
from pathlib import Path
import unittest

from fit_prices import family_targets, fit_at, scan


class PriceFitTests(unittest.TestCase):
    def setUp(self):
        catalog = json.loads(Path(__file__).with_name('catalog.json').read_text())
        self.base, self.targets = family_targets(catalog)

    def test_synthetic_consistent_prices_recover_shared_parameters(self):
        targets = [(str(i), price) for i, price in enumerate(
            (950, 1600, 950, 950, 1600, 950, 460, 920, 400))]
        result = fit_at(950, targets, 300, 0.8)
        for key, expected in [('upgrade', 300), ('pdl', 950), ('reflector', 300), ('rmse', 0)]:
            self.assertAlmostEqual(result[key], expected, places=5)

    def test_reference_prices_and_no_silent_exact_fit(self):
        self.assertEqual(self.base, 950)
        self.assertEqual([p for _, p in self.targets], [950, 1600, 950, 950, 1600, 950, 600, 1000, 600])
        result = scan(self.base, self.targets, 300, steps=100)
        self.assertGreater(result['rmse'], 1)
        self.assertEqual(len(result['residuals']), 9)
        self.assertTrue(all(result[key] >= 0 for key in ('upgrade', 'pdl', 'reflector')))
        for predicted, (_, target), residual in zip(result['predictions'], self.targets, result['residuals']):
            self.assertAlmostEqual(predicted - target, residual)

    def test_finer_scan_cannot_worsen_fit(self):
        coarse = scan(self.base, self.targets, 600, steps=10)
        fine = scan(self.base, self.targets, 600, steps=100)
        self.assertLessEqual(fine['rmse'], coarse['rmse'] + 1e-8)

    def test_bad_parameters(self):
        for cp, factor in [(-1, 0.8), (300, 0), (300, 1.1), (float('inf'), 0.5)]:
            with self.assertRaises(ValueError):
                fit_at(self.base, self.targets, cp, factor)


if __name__ == '__main__':
    unittest.main()
