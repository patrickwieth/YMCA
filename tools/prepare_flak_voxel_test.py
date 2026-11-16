"""Disposable sprite flak test; legacy script/package name, immutable testmap1."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import hashlib
from prepare_air_defense_targets import prepare
ROOT=Path(__file__).resolve().parents[1];source=ROOT/'mods/ca/maps/testmap1.oramap';dest=ROOT/'mods/ca/maps/soviet-flak-voxel-test.oramap';before=hashlib.sha256(source.read_bytes()).hexdigest()
with ZipFile(source) as z:files={name:z.read(name) for name in z.namelist()}
text=files['map.yaml'].decode('utf-8-sig');assert ': sam\n' in text
text=text.replace('Title: Building Grid Test (All Factions)','Title: Flak Sprite Test (local art)')
text=text.replace(': sam\n',': flak.soviet.sprite\n').replace(': hpad\n',': hpad.soviet\n')
# Only the Nod display slot changes; the separate GDI factory remains WEAP.TD.
assert '\tActor10: weap.td\n\t\tOwner: Multi0\n\t\tLocation: 78,6' in text
text=text.replace('\tActor10: weap.td\n','\tActor10: weap.nod\n',1)
# Ground-fire variant ONLY in this disposable test map, for Ctrl+click firing tests.
text=text.replace('Rules:\n','Rules:\n\tFLAK.SOVIET.SPRITE:\n\t\tArmament:\n\t\t\tWeapon: SOVIET-CLUSTER-AA.Test\n',1)
files['map.yaml']=prepare(text).encode('utf-8')
with ZipFile(dest,'w',ZIP_DEFLATED) as z:
 for name,data in files.items():z.writestr(name,data)
assert hashlib.sha256(source.read_bytes()).hexdigest()==before
print('FLAK_TEST_MAP_READY',dest,'original preserved',before)
