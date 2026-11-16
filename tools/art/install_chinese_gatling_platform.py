"""Install the approved supplied-platform cut; never repack/rescale the gun atlas."""
from pathlib import Path
from PIL import Image,PngImagePlugin
import hashlib,json,shutil,subprocess,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';P=ART/'platform-cut/pass-04';OUT=ART/'platform-cut/integration-pass04';OUT.mkdir(parents=True,exist_ok=True);old_binding=ART/'integration/binding.json';runtime=ROOT/'mods/ca/bits/china/buildings'
if json.loads(old_binding.read_text()).get('user_supplied_ring'):
 raise SystemExit('The user-supplied ring is authoritative; do not overwrite it with an older platform cut.')
for path in [old_binding,runtime/'gatling-base.png',runtime/'gatling-turret32.png',ROOT/'mods/ca/sequences/chinese-gatling.yaml',ROOT/'mods/ca/rules/china/gatling-art.yaml']:
 target=OUT/('before-'+path.name)
 if not target.exists():shutil.copy2(path,target)
meta=json.loads((P/'binding.json').read_text());b=json.loads(old_binding.read_text());turret_hash=hashlib.sha256((runtime/'gatling-turret32.png').read_bytes()).hexdigest()
with Image.open(P/'platform.png') as im:
 info=PngImagePlugin.PngInfo();info.add_text('FrameSize','128,112');info.add_text('FrameAmount','1');im.save(runtime/'gatling-base.png',pnginfo=info)
b.update(body_scale=1,body_frame_size=[128,112],body_offset=meta['body_offset'],decoration_bounds=1504,platform_source=meta['source'],platform_source_sha256=meta['source_sha256'],platform_review='platform-cut/pass-04',live_review=False)
b['assets']['gatling-base.png']=hashlib.sha256((runtime/'gatling-base.png').read_bytes()).hexdigest();old_binding.write_text(json.dumps(b,indent=2)+'\n')
subprocess.run([sys.executable,str(ROOT/'tools/art/bind_chinese_gatling.py')],check=True)
assert hashlib.sha256((runtime/'gatling-turret32.png').read_bytes()).hexdigest()==turret_hash
(OUT/'binding.json').write_text(json.dumps(dict(meta,runtime_installed=True,body_asset_sha256=b['assets']['gatling-base.png'],turret_unchanged_sha256=turret_hash),indent=2)+'\n')
print('Native120px trimmed platform installed on128px canvas; gun atlas, turret/muzzle registration and weapon settings retained.')
