"""Bind the user's edited runtime ring without rewriting a single supplied PNG byte."""
from pathlib import Path
from PIL import Image
import json,hashlib,shutil,subprocess,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/chinese gatling';OUT=ART/'ring-alignment';OUT.mkdir(exist_ok=True);p=ROOT/'mods/ca/bits/china/buildings/gatling-base.png';binding=ART/'integration/binding.json'
for f in [p,binding,ROOT/'mods/ca/rules/china/gatling-art.yaml',ROOT/'mods/ca/sequences/chinese-gatling.yaml']:
 q=OUT/('supplied-ring.png' if f==p else 'before-'+f.name)
 if not q.exists():shutil.copy2(f,q)
sha=hashlib.sha256(p.read_bytes()).hexdigest();b=json.loads(binding.read_text());im=Image.open(p);assert im.size==(86,54)
# Supplied cut matches the old bearing translated by(-21,-17). Original mount64,39
# is now43,22. Lower ring ellipse center43,32 sits at the unchanged bib origin.
b.update(body_frame_size=[86,54],body_offset=[0,-5],body_scale=1,mount=[0,0,226],user_supplied_ring=True,platform_review='ring-alignment',live_review=False)
b['assets']['gatling-base.png']=sha;binding.write_text(json.dumps(b,indent=2)+'\n')
subprocess.run([sys.executable,str(ROOT/'tools/art/bind_chinese_gatling.py')],check=True)
assert hashlib.sha256(p.read_bytes()).hexdigest()==sha
(OUT/'registration.json').write_text(json.dumps({'source_sha256':sha,'source_size':[86,54],'mount_pixel':[43,22],'ground_center':[43,32],'body_offset':[0,-5],'turret_mount':[0,0,226],'mount_screen_error':abs(10-226*64/1448),'turret_atlas_unchanged':b['assets']['gatling-turret32.png'],'local_muzzles_unchanged':b['local_offsets']},indent=2)+'\n')
# Actual native-scale composition for all facings; no source/turret texture edits.
atlas=Image.open(ROOT/'mods/ca/bits/china/buildings/gatling-turret32.png').convert('RGBA');bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA');board=Image.new('RGBA',(1792,896),(65,70,63,255));frames=[]
for i in range(32):
 t=Image.new('RGBA',(224,224),(65,70,63,255));t.alpha_composite(bib,(48,143));t.alpha_composite(im.convert('RGBA'),(69,143));gun=atlas.crop((i%8*320,i//8*320,i%8*320+320,i//8*320+320)).resize((208,208),Image.Resampling.NEAREST);t.alpha_composite(gun,(8,round(175-104-34*.65-226*64/1448)));board.alpha_composite(t,(i%8*224,i//8*224));frames.append(t.convert('RGB'))
board.save(OUT/'all32-native.png');frames[0].save(OUT/'rotation-native.gif',save_all=True,append_images=frames[1:],duration=120,loop=0)
