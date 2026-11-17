"""Offline placement review of direct original pedestal + installed rendered turret.
Nearest-pixel approximation of runtime Scale0.4; not an engine screenshot.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/soviet flak/integration-sprite14';OUT.mkdir(exist_ok=True)
base=Image.open(ROOT/'mods/ca/bits/soviet/buildings/flak base.png').convert('RGBA').resize((64,36),Image.Resampling.NEAREST)
bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings/bib1x1.png').convert('RGBA')
board=Image.new('RGB',(192*8,216*4),'#77736e');draw=ImageDraw.Draw(board)
for i in range(32):
    frame=Image.new('RGBA',(192,192),(119,115,110,255));frame.alpha_composite(bib,(32,104));frame.alpha_composite(base,(64,118))
    frame.alpha_composite(Image.open(ROOT/f'art-source/soviet flak/pass-14/review/turret-{i:03d}.png'),(0,5))
    x=i%8*192;y=i//8*216;board.paste(frame,(x,y+24));draw.text((x+4,y+4),f'{i*11.25:g} deg / original base',fill='white')
    if i==0:frame.resize((576,576),Image.Resampling.NEAREST).save(OUT/'original-base-placement.png')
board.save(OUT/'original-base-all32.png')
(OUT/'original-base-placement.json').write_text(json.dumps({'source':'bits/soviet/buildings/flak base.png','source_size':[161,90],'base_scale':.4,'base_offset':[0,-1],'ground_center_source':[80.5,46],'bib':'sovietbib1x1','turret_offset':[0,-29],'turret_attachment':[0,0,131],'muzzle_relative':[1337,0,1603],'preview':'nearest-pixel offline approximation; no live rendering claim'},indent=2)+'\n')
