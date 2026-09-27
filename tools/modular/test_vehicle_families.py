import copy
import json
from pathlib import Path
import unittest

from survey_vehicles import classify, family_summary, report, survey

HERE = Path(__file__).resolve().parent


class VehicleFamilyTests(unittest.TestCase):
    def setUp(self):
        self.catalog = json.loads((HERE / 'catalog.json').read_text())
        self.rows = survey(HERE.parents[1], self.catalog)
        self.actors = {(r['faction'], r['actor']): r for r in self.rows}
        self.families = {(f['faction'], f['family']): f for f in family_summary(self.rows)}

    def test_named_variants_not_counted_as_new_families(self):
        for actor in ('Challenger_Tank', 'Leclerc_Tank', 'Leopard_Tank'):
            self.assertEqual(self.actors['allies', actor]['family'], '2TNK')
            self.assertNotIn(('allies', actor), self.families)
        jeep = self.families['allies', 'JEEP']
        for actor in ('Heavy_Jeep', 'Tow_Jeep', 'AA_Jeep'):
            self.assertIn(actor, jeep['variants'])
        self.assertIn('PCAN', self.families['allies', 'Prismtank']['variants'])

    def test_distinct_hulls_and_roles_are_not_merged_by_inheritance(self):
        for actor in ('TTNK', 'TTNK.RA2', 'BTR', 'NonaSVK'):
            self.assertIn(('soviet', actor), self.families)
        for actor in ('ARTY.nod', 'HOWI', 'SPEC'):
            self.assertIn(('nod', actor), self.families)
        self.assertEqual(self.actors['scrin', 'HARV.Scrin']['family'], 'HARV.Scrin')

    def test_projectiles_wrecks_and_attachments_are_excluded(self):
        for faction, actor, category in (
            ('china', 'chnukecannon.shell.range', 'projectile'),
            ('china', 'Bixi.Missile', 'projectile'),
            ('soviet', 'V3B', 'projectile'),
            ('soviet', 'Rice_PassengerShell', 'projectile'),
            ('china', 'choverlord.Husk', 'wreck'),
            ('gdi', 'MDRN.Attached', 'attachment'),
            ('china', 'CHTRUK', 'review'),
        ):
            row = self.actors[faction, actor]
            self.assertEqual(row['category'], category)
            self.assertFalse(row['family'])
            self.assertNotIn((faction, actor), self.families)

    def test_disabled_parent_does_not_hide_active_family(self):
        for actor in ('BATF', '2TNK'):
            self.assertEqual(self.families['allies', actor]['status'], 'production candidate')
        self.assertEqual(self.actors['allies', 'BATF']['category'], 'prototype')
        self.assertEqual(self.actors['allies', '2TNK']['category'], 'non-production form')
        self.assertEqual(self.families['soviet', 'SFTNK']['status'], 'prototype / availability review')
        self.assertEqual(self.actors['soviet', 'MCV.Nukular.Soviet']['family'], 'MCV.Soviet')
        self.assertEqual(self.actors['soviet', 'MCV.Nukular.Soviet']['category'], 'non-production form')

    def test_every_raw_actor_is_accounted_for_once(self):
        self.assertEqual(len(self.actors), len(self.rows))
        assigned = [(f['faction'], a) for f in self.families.values() for a in f['members']]
        self.assertEqual(len(assigned), len(set(assigned)))
        excluded = {(r['faction'], r['actor']) for r in self.rows if not r['family']}
        self.assertFalse(set(assigned) & excluded)
        self.assertEqual(set(assigned) | excluded, set(self.actors))
        for f in self.families.values():
            self.assertEqual(set(f['covered']) | set(f['pending']), set(f['members']))
            self.assertFalse(set(f['covered']) & set(f['pending']))
        self.assertIn('NOT full family coverage', report(self.rows))
        self.assertIn('## Raw audit appendix', report(self.rows))

    def test_stale_policy_and_unknown_dotted_actor_fail_safely(self):
        with self.assertRaisesRegex(ValueError, 'Stale family policy'):
            classify(copy.deepcopy(self.rows), {'anchors': {'nod': ['does-not-exist']}})
        with self.assertRaisesRegex(ValueError, 'alias target'):
            classify(copy.deepcopy(self.rows), {'aliases': {'nod': {'LTNK': 'missing'}}})
        row = copy.deepcopy(self.actors['gdi', 'HMMV'])
        row.update(actor='unknown.special', inherits=[])
        classify([row], {})
        self.assertEqual(row['category'], 'review')
        self.assertFalse(row['family'])


if __name__ == '__main__':
    unittest.main()
