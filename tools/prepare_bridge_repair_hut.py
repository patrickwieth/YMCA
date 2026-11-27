"""Non-destructive, bounded cutout of the supplied left-hand bridge repair hut."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops
import hashlib, json, shutil
root=Path(__file__).resolve().parents[1]
source=Path('E:/SharedShitzens/src/openRA/retrodiffusion/misc/bridge_repair_hut-447710944-0.png')
archive=root/'art-source/bridge-repair-hut';archive.mkdir(parents=True,exist_ok=True)
original=archive/'original.png'
if original.exists() and original.read_bytes()!=source.read_bytes():raise SystemExit('Archived original differs; refusing to overwrite.')
shutil.copy2(source,original)
im=Image.open(source).convert('RGBA');mask=Image.new('L',im.size);d=ImageDraw.Draw(mask)
d.polygon([(6,47),(43,27),(48,27),(58,44),(57,49),(56,52),(56,74),(47,77),(47,83),(28,89),(27,91),(24,91),(24,88),(17,91),(13,94),(4,96),(2,94),(2,89),(1,85),(1,79),(3,77),(3,72),(5,70)],fill=255)
d.polygon([(24,34),(24,23),(25,23),(25,20),(26,20),(26,23),(28,23),(28,25),(27,25),(27,32),(28,34),(31,34),(31,36),(28,36),(26,35)],fill=255)
im.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
box=im.getbbox();im=im.crop(box)
out=root/'mods/ca/bits/terrain/environment/bridge-repair-hut.png';im.save(out)
(archive/'provenance.json').write_text(json.dumps({'source':source.name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'crop':box,'operation':'Polygon cutout only; original RGB retained, no resampling or reconstruction.'},indent=2)+'\n')
review=Image.new('RGBA',im.size,(100,150,100,255));review.alpha_composite(im)
review.resize((im.width*6,im.height*6),Image.Resampling.NEAREST).save(archive/'cutout-review.png')
