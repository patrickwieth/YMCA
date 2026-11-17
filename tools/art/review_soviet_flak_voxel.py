"""Contact sheet/GIF of the exported-cell inspection, explicitly not live engine output."""
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];p=ROOT/'art-source/soviet flak/voxel-01';frames=[]
for frame in (0,1,4,8,10):
 im=Image.new('RGB',(768,808),'#77736e');d=ImageDraw.Draw(im)
 for j,facing in enumerate((0,8,16,24)):
  v=Image.open(p/f'voxel-{facing:02d}-{frame:02d}.png').resize((384,384),Image.Resampling.NEAREST);x=j%2*384;y=j//2*404
  im.paste(v,(x,y+20),v);d.text((x+5,y+3),f'Exported voxel cells / {facing*11.25:g} deg / {frame}',fill='white')
 frames.append(im)
frames[0].save(p/'voxel-four-views.png');frames[0].save(p/'voxel-recoil.gif',save_all=True,append_images=frames[1:],duration=[240,120,160,80,240],loop=0)
