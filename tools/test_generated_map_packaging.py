"""Regression for M0020: version stamping must not hide generated maps."""
import pathlib
import shutil
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class GeneratedMapPackagingTests(unittest.TestCase):
    def test_version_stamp_preserves_generated_folder(self):
        with tempfile.TemporaryDirectory(prefix="ymca-map-packaging-") as folder:
            work = pathlib.Path(folder)
            script = work / "engine/packaging/functions.sh"
            script.parent.mkdir(parents=True)
            script.write_bytes((ROOT / "engine/packaging/functions.sh").read_bytes())
            patch = ROOT / "tools/patches/stable-generated-map-folder.patch"
            applied = subprocess.run(["git", "apply", "--reverse", "--check", "--ignore-space-change", str(patch)], cwd=work, capture_output=True)
            if applied.returncode:
                subprocess.run(["git", "apply", "--ignore-space-change", str(patch)], cwd=work, check=True)
            manifest = work / "mod.yaml"
            manifest.write_bytes(b"Metadata:\n\tVersion: dev\nMapFolders:\n\tca|maps: System\n\t~^SupportDir|maps/ca/modular: User\n\t~^SupportDir|maps/ca/generated: User\n")
            shell = shutil.which("sh")
            self.assertIsNotNone(shell)
            for version in ("v0.97.3", "v0.97.4"):
                subprocess.run([shell, "-c", '. "$1"; set_mod_version "$2" "$3"', "test", script.as_posix(), version, manifest.as_posix()], check=True)
                text = manifest.read_text()
                self.assertIn("Version: " + version, text)
                self.assertEqual(text.count("maps/ca/" + version + ": User"), 1)
                self.assertEqual(text.count("maps/ca/generated: User"), 1)
                self.assertIn("ca|maps: System", text)


if __name__ == "__main__":
    unittest.main()
