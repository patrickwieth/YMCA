"""Exercise the public-server SSH guard without SSH, servers or deployment."""
from pathlib import Path
import subprocess
import unittest
import yaml

ROOT = Path(__file__).resolve().parents[1]


class PublicDeploymentGuardTests(unittest.TestCase):
    def test_both_entry_points_fail_closed(self):
        for filename, job in [('packaging.yml', 'deploy-servers'), ('Deploy.yml', 'deploy')]:
            doc = yaml.safe_load((ROOT / '.github/workflows' / filename).read_text())
            script = doc['jobs'][job]['steps'][-1]['with']['script']
            self.assertIn('sport = :1234 or sport = :1235', script)
            self.assertNotIn('1236', script)  # Cameo is outside this deployment.
            script = script.replace('./deploy-ymca-server.sh', 'echo DEPLOY_CALLED')
            for label, stub, allowed in [('empty', 'return 0', True),
                                         ('occupied', "echo 'ESTAB connected-client'", False),
                                         ('query-error', 'return 2', False)]:
                with self.subTest(workflow=filename, state=label):
                    result = subprocess.run(['bash', '-c', 'ss() { ' + stub + '; };\n' + script],
                                            text=True, capture_output=True)
                    self.assertEqual('DEPLOY_CALLED' in result.stdout, allowed)
                    self.assertEqual(result.returncode == 0, allowed)


if __name__ == '__main__':
    unittest.main()
