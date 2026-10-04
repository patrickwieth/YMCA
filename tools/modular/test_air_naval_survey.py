import unittest
from collections import Counter
from survey_air_naval import load, declarations, classify, features, report, REPORT
from survey_loadouts import source_hash, selected, pack, unpack
from list_missing_vehicles import native_actors


class AirNavalSurveyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = load(); cls.rows = {r['actor']: r for r in cls.data['actors']}

    def test_complete_declaration_inventory_and_source_fingerprint(self):
        self.assertEqual(set(self.rows), {r['actor'] for r in declarations()})
        self.assertEqual(len(self.rows), 160)
        self.assertEqual(self.data['source_sha256'], source_hash())
        self.assertFalse(set(a.lower() for a in self.rows) & native_actors(), 'Research must not silently enable air/naval designs.')
        for r in self.rows.values(): self.assertEqual(r['category'], classify(r))

    def test_inherited_buildable_is_not_misclassified_as_nonproduction(self):
        for actor in ('B2B', 'YF23.Bomber'):
            row = self.rows[actor]
            self.assertFalse(row['local_buildable'])
            self.assertEqual(row['category'], 'production-candidate')
        self.assertEqual(Counter(r['category'] for r in self.rows.values())['production-candidate'], 114)

    def test_aircraft_speed_comes_from_aircraft_trait_not_a_missing_mobile(self):
        for row in self.rows.values():
            if row['movement_kind'] != 'aircraft': continue
            aircraft = next(t for t in selected(row, 'Aircraft') if t['type'] == 'AircraftInfo')
            self.assertEqual(row['speed'], int(aircraft['fields']['Speed']))
            self.assertIn('CruiseAltitude', aircraft['fields'])
        self.assertEqual(self.rows['CARR']['movement_kind'], 'mobile')
        self.assertEqual(self.rows['CARR']['speed'], 25)

    def test_manual_and_automatic_carryall_and_transport_are_distinct(self):
        self.assertEqual(self.rows['OCAR']['category'], 'disabled-vehicle')
        self.assertIn('Carryall', features(self.rows['OCAR']))
        self.assertIn('AutoCarryall', features(self.rows['OCAR.Eagle']))
        self.assertTrue(selected(self.rows['LST'], 'Cargo'))
        self.assertTrue(selected(self.rows['TRAN'], 'Cargo'))
        self.assertTrue(selected(self.rows['Eurofighter'], 'Rearmable'))

    def test_carriers_preserve_spawned_actor_and_rearming_references(self):
        for actor in ('CARR', 'PAC', 'Orca.Warship', 'Kirov.Carrier'):
            carrier = selected(self.rows[actor], 'CarrierMaster')[0]
            self.assertTrue(carrier['fields']['Actors'])
            self.assertIn('RearmTicks', carrier['fields'])
            self.assertIn('SpawnIsMissile', carrier['fields'])
        self.assertEqual(sum(r['category'] == 'carrier/drone-slave' for r in self.rows.values()), 6)

    def test_upgrades_missiles_and_wrecks_are_not_counted_as_production_aircraft(self):
        self.assertEqual(self.rows['helix.armorupgrade']['category'], 'non-mobile/review')
        self.assertEqual(self.rows['ICBM']['category'], 'spawned-missile')
        self.assertEqual(self.rows['C17']['category'], 'non-production/support/review')
        counts = Counter(r['category'] for r in self.rows.values())
        self.assertEqual(counts['wreck'], 3)
        self.assertEqual(counts['spawned-missile'], 4)

    def test_deduplicated_snapshot_and_report_remain_reproducible(self):
        self.assertEqual(unpack(pack(self.data))['actors'], self.data['actors'])
        self.assertEqual(REPORT.read_text(encoding='utf-8'), report(self.data))
        for row in self.rows.values():
            for trait in row['traits']:
                self.assertNotIn('OpenRA.Support.BooleanExpression', trait['fields'].values())


if __name__ == '__main__':
    unittest.main()
