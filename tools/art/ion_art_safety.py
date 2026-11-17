"""Read-only guards shared by historical preview builders and art tests."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
NAMES=('heavy-blaster-base.png','heavy-blaster-turret32.png')

def active_assets():
    return {name:(ROOT/'mods/ca/bits/gdi/buildings/experimental'/name).read_bytes() for name in NAMES}

def assert_preserved_ion_art(test):
    base=ROOT/'art-source/gdi ion cannon'
    historical=json.loads((base/'attempt-05/integration/binding.json').read_text())['assets']
    for name in NAMES:
        test.assertEqual(hashlib.sha256((base/'attempt-05/export'/name).read_bytes()).hexdigest(),historical[name])
        test.assertEqual(active_assets()[name],(base/'attempt-11-centering/export'/name).read_bytes())
