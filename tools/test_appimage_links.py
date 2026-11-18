"""User-local registration tests; never touches real desktop associations."""
import os
import pathlib
import shutil
import subprocess
import tempfile
import time
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / 'packaging/linux/register-links.sh'


class AppImageLinksTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='ymca-links-test-')
        self.root = pathlib.Path(self.temp.name)
        self.bin = self.root / 'bin'
        self.bin.mkdir()
        self.env = dict(os.environ, TEST_ROOT=str(self.root), DISPLAY=':99',
                        XDG_DATA_HOME=str(self.root / 'data'), XDG_CONFIG_HOME=str(self.root / 'config'))
        self.env['PATH'] = str(self.bin) + os.pathsep + os.environ['PATH']
        self.command('xdg-mime', '''case "$1" in
query) if [ -f "$TEST_ROOT/current" ]; then head -n 1 "$TEST_ROOT/current"; fi ;;
default) printf '%s\\n' "$2" > "$TEST_ROOT/current"; printf 'set\\n' >> "$TEST_ROOT/sets" ;;
esac
''')
        self.command('update-desktop-database', ':\n')
        self.command('zenity', 'printf "asked\\n" >> "$TEST_ROOT/prompts"\nexit "${ANSWER:-0}"\n')
        self.image = self.app('YMCA with spaces %.AppImage')

    def command(self, name, text):
        p = self.bin / name
        p.write_bytes(('#!/bin/sh\n' + text).encode())
        p.chmod(0o755)

    def app(self, name):
        p = self.root / name
        p.write_bytes(b'#!/bin/sh\nprintf "%s\\n" "$@" > "$TEST_ROOT/received"\n')
        p.chmod(0o755)
        return p

    def run_registration(self, mode='--prompt', image=None):
        return subprocess.run([shutil.which('sh'), str(SCRIPT), str(image or self.image), mode],
                              env=self.env, capture_output=True, text=True, check=True)

    @property
    def desktop(self):
        return self.root / 'data/applications/ymca-appimage.desktop'

    def tearDown(self):
        self.temp.cleanup()

    def test_decline_is_remembered_without_registering(self):
        self.env['ANSWER'] = '1'
        self.run_registration(); self.run_registration()
        self.assertEqual((self.root / 'prompts').read_text().splitlines(), ['asked'])
        self.assertFalse(self.desktop.exists())
        self.assertFalse((self.root / 'sets').exists())

    def test_accept_and_refresh_after_move(self):
        self.run_registration()
        self.assertIn('MimeType=x-scheme-handler/ymca;', self.desktop.read_text())
        self.assertIn('spaces %%.AppImage" %u', self.desktop.read_text())
        moved = self.app('YMCA moved.AppImage')
        self.run_registration(image=moved)
        self.assertIn('YMCA moved.AppImage', self.desktop.read_text())
        self.assertEqual((self.root / 'prompts').read_text().splitlines(), ['asked'])

    def test_existing_handler_is_not_stolen(self):
        (self.root / 'current').write_text('other.desktop\n')
        self.run_registration()
        self.assertFalse(self.desktop.exists())
        self.assertFalse((self.root / 'prompts').exists())
        self.run_registration('--register')
        self.assertTrue(self.desktop.exists())
        self.assertEqual((self.root / 'current').read_text().strip(), 'ymca-appimage.desktop')

    def test_later_default_change_is_respected(self):
        self.run_registration('--register')
        old = self.desktop.read_text()
        (self.root / 'current').write_text('other.desktop\n')
        self.run_registration(image=self.app('Another.AppImage'))
        self.assertEqual(self.desktop.read_text(), old)
        self.assertEqual((self.root / 'current').read_text().strip(), 'other.desktop')

    def test_headless_start_does_not_prompt_or_register(self):
        self.env.pop('DISPLAY', None); self.env.pop('WAYLAND_DISPLAY', None)
        self.run_registration()
        self.assertFalse(self.desktop.exists())
        self.assertFalse((self.root / 'prompts').exists())

    @unittest.skipUnless(os.name != 'nt' and shutil.which('gio'), 'Native Linux GIO required')
    def test_real_desktop_launch_preserves_uri_and_special_path_characters(self):
        image = self.app('YMCA "quoted" $HOME `touch owned` % back\\slash.AppImage')
        self.run_registration('--register', image)
        uri = 'ymca://127.0.0.1:1234?name=Alice%20Bob&password=abc%25'
        subprocess.run(['gio', 'launch', str(self.desktop), uri], env=self.env, check=True, capture_output=True)
        received = self.root / 'received'
        for _ in range(50):
            if received.exists(): break
            time.sleep(.1)
        self.assertEqual(received.read_text().strip(), uri)
        self.assertFalse((self.root / 'owned').exists())


if __name__ == '__main__':
    unittest.main()
