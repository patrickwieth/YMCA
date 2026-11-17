"""Original / previous study38 / material43 comparison at identical native density and pose."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,subprocess,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin shard launcher';P=ART/'pass-43';OUT=P/'review';OLD=ART/'pass-38/review'
subprocess.run([sys.executable,str(ROOT/'tools/test_scrin_shard_materials.py')],check=True)
source=Image.new('RGBA',(384,384));source.alpha_composite(Image.open(ART/'shard_launcher-332414650-0.png').convert('RGBA'),(64,96))
board=Image.new('RGBA',(1152,412),(77,80,78,255));d=ImageDraw.Draw(board)
for i,(label,image) in enumerate([('ORIGINAL',source),('PREVIOUS STUDY / MATERIAL38',Image.open(OLD/'reference.png')),('ORGANIC SURFACES43 / SAME GEOMETRY',Image.open(OUT/'reference.png'))]):
 board.alpha_composite(image,(384*i,28));d.text((i*384+8,8),label,fill='white')
board.save(OUT/'material-before-after-source.png');board.crop((0,112,1152,392)).resize((2304,560),Image.Resampling.NEAREST).save(OUT/'material-before-after-source-2x.png')
frames=[];oldbase=Image.open(OLD/'base.png');base=Image.open(OUT/'base.png')
for i in list(range(30,32))+list(range(30)):
 frame=Image.new('RGBA',(560,280),(77,80,78,255));d=ImageDraw.Draw(frame)
 for column,(directory,b,label) in enumerate([(OLD,oldbase,'PREVIOUS STUDY38'),(OUT,base,'MATERIAL43 / not installed')]):
  image=Image.alpha_composite(b,Image.open(directory/f'turret-{i:02d}.png'));frame.alpha_composite(image.resize((223,223),Image.Resampling.NEAREST),(29+280*column,28));d.text((8+280*column,8),label,fill='white')
 frames.append(frame.convert('RGB'))
frames[0].save(OUT/'material-rotation-native.gif',save_all=True,append_images=frames[1:],duration=130,loop=0)
(P/'material-validation.json').write_text(json.dumps({'pass':43,'geometry_reference':30,'geometry_identical':True,'raw_alpha_identical_all_frames':True,'raw_split_full_controls_equal':True,'crystal_cuts_exclude_gray_armor':True,'runtime30_unchanged':True,'test_command':'python tools/test_scrin_shard_materials.py','not_live_game_screenshots':True},indent=2)+'\n')
print(OUT/'material-before-after-source.png')
