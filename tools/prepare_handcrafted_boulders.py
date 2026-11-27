"""Extract intact, native-density boulders from the supplied Rubberduck atlas.
No source pixels or geometry are repainted; these replace inappropriate plateau
walls around small isolated legacy Rock components. Collision stays in map data.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'mods/ca/bits/terrain/rubberduck/rock_cliffs.png'
image=Image.open(source).convert('RGBA')
output=source.parent/'derived/handcrafted'
output.mkdir(parents=True,exist_ok=True)
records=[]
for index,slot in enumerate([16,17,20,21]):
    frame=[slot%12*128,slot//12*256,(slot%12+1)*128,(slot//12+1)*256]
    cut=image.crop(frame);bounds=cut.getchannel('A').getbbox();cut=cut.crop(bounds)
    filename='boulder-'+str(index)+'.png';cut.save(output/filename)
    records.append({'file':filename,'atlas_slot':slot,'frame':frame,'alpha_bounds':bounds,
                    'offset':[0,8-cut.height//2], 'sha256':hashlib.sha256((output/filename).read_bytes()).hexdigest()})
(output/'provenance.json').write_text(json.dumps({'source':'bits/terrain/rubberduck/rock_cliffs.png',
    'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'cuts':records},indent=2)+'\n')
print(output)
