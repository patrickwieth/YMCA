"""Review a fresh render of the modeled pedestal with the unchanged sprite turret.
Offline composite only; never writes runtime assets or rules.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2]
STUDY=ROOT/'art-source/soviet flak/pass-15';OUT=STUDY/'review'
previous=ROOT/'art-source/soviet flak/pass-14/review'
base=Image.open(OUT/'base.png').convert('RGBA')
bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA')
g=json.loads((STUDY/'geometry.json').read_text())
assert g['ground_diamond_max']<=64
with Image.open(STUDY/'base-raw.png') as a,Image.open(STUDY/'base-control-raw.png') as b:assert a.tobytes()==b.tobytes()
frames=[]
for i in range(32):
    turret=Image.open(OUT/f'turret-{i:03d}.png').convert('RGBA')
    with Image.open(previous/f'turret-{i:03d}.png') as old:assert old.tobytes()==turret.tobytes(),i
    frame=Image.new('RGBA',(192,192),(119,115,110,255));frame.alpha_composite(bib,(32,104));frame.alpha_composite(base);frame.alpha_composite(turret);frames.append(frame)
baseview=Image.new('RGBA',(192,192),(119,115,110,255));baseview.alpha_composite(bib,(32,104));baseview.alpha_composite(base)
board=Image.new('RGB',(192*5,216),'#77736e');draw=ImageDraw.Draw(board)
for slot,(label,im) in enumerate([('SOCKEL / 1x1',baseview)]+[(f'FLAK / {i*11.25:g} deg',frames[i]) for i in (0,8,16,24)]):
    board.paste(im,(192*slot,24));draw.text((192*slot+5,5),label,fill='white')
board.resize((1920,432),Image.Resampling.NEAREST).save(OUT/'matching-base-2x.png')
allviews=Image.new('RGB',(192*8,216*4),'#77736e');draw=ImageDraw.Draw(allviews)
for i,frame in enumerate(frames):
    x=i%8*192;y=i//8*216;allviews.paste(frame,(x,y+24));draw.text((x+5,y+5),f'{i*11.25:g} deg / 1x1',fill='white')
allviews.save(OUT/'matching-base-all32.png')
(OUT/'matching-base-check.json').write_text(json.dumps({'fresh_model_render':15,'geometry':'existing modeled pedestal, no new design','turret_pixels_identical_to_pass14':True,'fixed_control_identical':True,'ground_diamond_max':g['ground_diamond_max'],'ground_diamond_limit':64,'runtime_changed':False,'view':'offline composite, not game screenshot'},indent=2)+'\n')
print('MATCHING_BASE_REVIEW_READY;32 turret frames unchanged;1x1 fit;runtime untouched')
