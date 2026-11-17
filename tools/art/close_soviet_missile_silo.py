"""Close the native silo halves at rest; preserve originals and translate pixels only."""
from pathlib import Path
import hashlib,json,shutil
from PIL import Image
R=Path(__file__).resolve().parents[2]
F=R/'art-source/soviet-missile-silo/close-pass-01'
F.mkdir(parents=True,exist_ok=True)
source=F/'original';source.mkdir(exist_ok=True)
runtime=R/'mods/ca/bits/soviet/buildings'
frames=[];records=[]
for i in range(7):
 name=f'missile silo {i}.png';saved=source/name
 if not saved.exists():shutil.copy2(runtime/name,saved)
 im=Image.open(saved).convert('RGBA');assert im.size==(440,280)
 # Foreground right housing correctly occludes the hidden inner rear face.
 shift=24-4*i
 out=Image.new('RGBA',im.size)
 out.alpha_composite(im.crop((0,0,225,280)),(shift,0))
 out.alpha_composite(im.crop((225,0,440,280)),(225-shift,0))
 out.save(F/name);frames.append(out)
 records.append({'frame':i,'left_shift':[shift,0],'right_shift':[-shift,0],'source_sha256':hashlib.sha256(saved.read_bytes()).hexdigest()})
review=Image.new('RGBA',(880,280),(44,48,44,255));review.alpha_composite(Image.open(source/'missile silo 0.png').convert('RGBA'));review.alpha_composite(frames[0],(440,0));review.convert('RGB').save(F/'closed-before-after.png')
canvas=[]
for im in frames+frames[-2::-1]:
 bg=Image.new('RGBA',im.size,(44,48,44,255));bg.alpha_composite(im);canvas.append(bg.convert('RGB'))
canvas[0].save(F/'open-close.gif',save_all=True,append_images=canvas[1:],duration=[900]+[100]*11+[900],loop=0)
(F/'translations.json').write_text(json.dumps(records,indent=2)+'\n')
print(F)
