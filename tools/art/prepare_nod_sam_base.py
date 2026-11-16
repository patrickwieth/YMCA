"""Isolate supplied Nod SAM foundation; preserve visible pixels, mark inferred roof.
No source overwrite, resizing, runtime binding or background scenery export.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/nod sam site';S=F/'sam_site-1179307279-0.png';O=F/'source-study';O.mkdir(exist_ok=True)
im=Image.open(S).convert('RGBA');mask=Image.new('L',im.size);d=ImageDraw.Draw(mask)
# Main attached structure, including its fixed roof radar and service cabinet.
outline=[(67,109),(86,98),(103,96),(143,89),(164,89),(168,95),(170,100),(181,103),(188,101),(189,105),(201,109),(203,120),(203,149),(204,158),(204,177),(198,182),(185,183),(187,188),(182,188),(176,183),(164,182),(154,187),(139,189),(138,192),(130,192),(129,190),(115,189),(104,184),(95,183),(91,181),(76,177),(74,169),(65,164),(64,149),(61,150),(62,137),(65,129)]
d.polygon(outline,fill=255)
d.polygon([(166,103),(171,97),(171,92),(167,85),(167,73),(171,66),(176,62),(182,62),(188,66),(194,71),(195,88),(192,93),(187,96),(187,102),(193,105),(194,116),(187,121),(174,119),(166,114)],fill=255)
# Old rotating mechanism above the stationary bearing. Geometry hidden by it is
# inferred below, not presented as extracted original pixels.
occlusion=[(60,0),(160,0),(160,67),(137,74),(143,95),(147,106),(149,114),(142,122),(127,126),(105,127),(89,121),(84,112),(84,98),(60,103)]
hidden=Image.new('L',im.size);ImageDraw.Draw(hidden).polygon(occlusion,fill=255)
keep=np.array(mask)>0;keep&=np.array(hidden)==0
visible=np.array(im);visible[~keep]=0;visible=Image.fromarray(visible)
base=Image.new('RGBA',im.size);b=ImageDraw.Draw(base)
# Small rear roof patch under the old launcher; original exposed roof overlays it.
b.polygon([(67,109),(91,97),(143,89),(164,90),(180,106),(151,126),(96,133),(68,121)],fill=(70,68,65))
b.line([(69,110),(92,100),(144,92),(162,92)],fill=(104,47,47),width=2)
# Inferred bearing top; no newly invented facade or replacement of visible walls.
b.ellipse((86,99,154,131),fill=(25,24,24));b.ellipse((88,100,152,129),fill=(73,72,68));b.ellipse((93,103,148,125),fill=(43,41,40))
b.ellipse((96,105,145,124),outline=(95,91,84),width=1)
base.alpha_composite(visible)
# All known preserved pixels must be byte identical.
a=np.array(base);orig=np.array(im);assert np.array_equal(a[keep],orig[keep])
base.save(O/'base.png');visible.save(O/'preserved-visible.png');Image.fromarray((keep*255).astype('uint8')).save(O/'preserved-mask.png')
inferred=(np.array(base)[:,:,3]>0)&~keep;Image.fromarray((inferred*255).astype('uint8')).save(O/'inferred-mask.png')
board=Image.new('RGBA',(768,280),(119,115,110,255));draw=ImageDraw.Draw(board)
for i,(p,label) in enumerate(((im,'SUPPLIED SOURCE'),(base,'FIXED BASE / native pixels'),(visible,'ORIGINAL VISIBLE PIXELS ONLY'))):board.alpha_composite(p,(i*256,24));draw.text((i*256+5,5),label,fill='white')
board.resize((1536,560),Image.Resampling.NEAREST).save(O/'extraction-2x.png')
(O/'extraction.json').write_text(json.dumps({'source_sha256':hashlib.sha256(S.read_bytes()).hexdigest(),'source_size':[256,256],'pivot_pixel':[119,115],'ground_pixel':[133,181],'visible_pixels_preserved':int(keep.sum()),'inferred_pixels':int(inferred.sum()),'fixed_radar_retained':True,'scenery_excluded':True,'runtime_installed':False},indent=2)+'\n')
print('NOD_SAM_BASE_EXTRACTED',keep.sum(),inferred.sum())
