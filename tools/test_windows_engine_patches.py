"""Exercise the real Windows patch preparation against isolated engine-source fixtures."""
import hashlib
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
PATCHES = [ROOT / line for line in (ROOT / 'tools/engine-patches.txt').read_text().splitlines() if line]

class EnginePatchPathTests(unittest.TestCase):
    def test_registered_paths_match_windows_strip_depth(self):
        for patch in PATCHES:
            headers = [line for line in patch.read_text().splitlines() if line.startswith(('--- ', '+++ '))]
            self.assertTrue(headers, patch.name)
            for header in headers:
                path = header[4:].split('\t', 1)[0]
                prefix = 'a/engine/' if header.startswith('--- ') else 'b/engine/'
                self.assertTrue(path == '/dev/null' or path.startswith(prefix), f'{patch.name}: {header}')

@unittest.skipUnless(shutil.which('powershell.exe') and shutil.which('git'), 'Windows PowerShell and Git required')
class WindowsEnginePatchTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='YMCA engine patches ')
        self.parent = Path(self.temp.name)
        self.engine = self.parent / 'engine source'
        paths = {'OpenRA.Game/OpenRA.Game.csproj'}
        for patch in PATCHES:
            paths.update(line[len('+++ b/engine/'):] for line in patch.read_text().splitlines() if line.startswith('+++ b/engine/'))
        for rel in paths:
            source = ROOT / 'engine' / rel
            if source.exists():
                dest = self.engine / rel
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, dest)
        # Normalize the installed engine copy back to its pre-patch state.
        for patch in reversed(PATCHES):
            if self.git('--reverse', '--check', patch=patch).returncode == 0:
                self.assertEqual(self.git('--reverse', patch=patch).returncode, 0)

    def tearDown(self):
        self.temp.cleanup()

    def git(self, *args, patch):
        return subprocess.run(['git', 'apply', '--no-index', '--ignore-space-change', '-p2', '--directory=engine source', *args, str(patch)], cwd=self.parent, capture_output=True, text=True)

    def prepare(self):
        return subprocess.run(['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(ROOT / 'tools/prepare-engine-patches.ps1'), '-EngineDirectory', str(self.engine)], cwd=self.parent, capture_output=True, text=True)

    def snapshot(self):
        return {str(p.relative_to(self.engine)): hashlib.sha256(p.read_bytes()).hexdigest() for p in self.engine.rglob('*') if p.is_file()}

    def test_fresh_engine_and_repeat_in_path_with_spaces(self):
        result = self.prepare()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        for patch in PATCHES:
            self.assertEqual(self.git('--reverse', '--check', patch=patch).returncode, 0, patch.name)
        before = self.snapshot()
        self.assertEqual(self.prepare().returncode, 0)
        self.assertEqual(self.snapshot(), before)
        self.assertIn('IReplayMapProvider', (self.engine / 'OpenRA.Game/FileFormats/ReplayMapSupport.cs').read_text())

    def test_conflicting_local_source_is_not_overwritten(self):
        self.assertEqual(self.prepare().returncode, 0)
        file = self.engine / 'OpenRA.Game/FileFormats/ReplayMapSupport.cs'
        file.write_text('LOCAL CONFLICT - MUST NOT BE OVERWRITTEN\n')
        before = self.snapshot()
        result = self.prepare()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.snapshot(), before)

if __name__ == '__main__':
    unittest.main()
