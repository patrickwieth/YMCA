"""Complete the clipped Chemical Tower with integer source cuts only; preserve original."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
R=Path(__file__).resolve().parents[2];F=R/'art-source/soviet chem tower';src=F/'chemtower.png';im=Image.open(src).convert('RGBA');assert im.size==(128,192)
O=F/'pass-03';O.mkdir(exist_ok=True);out=Image.new('RGBA',(128,208));records=[]
# The reference was also cut horizontally at y48. Complete only the missing cap area.
cap=Image.new('RGBA',out.size)
cap.paste(im.crop((56,49,75,53)),(56,44))
cap_polygon=[(56,48),(56,47),(58,45),(62,44),(67,44),(71,45),(74,48)]
capmask=Image.new('1',out.size);ImageDraw.Draw(capmask).polygon(cap_polygon,fill=1)
out.paste(cap,(0,0),capmask);records.append({'source_rect':[56,49,75,53],'destination':[56,44],'clip_polygon':cap_polygon})
for x,poly in [(38,[(40,44),(44,44),(46,46),(46,48),(38,48),(38,46)]),(82,[(84,44),(87,44),(89,46),(89,48),(82,48),(82,46)])]:
 patch=Image.new('RGBA',out.size);patch.paste(im.crop((x,49,x+8,54)),(x,44));mask=Image.new('1',out.size);ImageDraw.Draw(mask).polygon(poly,fill=1);out.paste(patch,(0,0),mask);records.append({'source_rect':[x,49,x+8,54],'destination':[x,44],'clip_polygon':poly})
out.alpha_composite(im,(0,0))
# Continue the existing lower wall/door/pier pixels. Only the previously transparent rows are filled.
wall=im.crop((16,164,119,176));out.paste(wall,(16,176));records.append({'source_rect':[16,164,119,176],'destination':[16,176]})
# Native circumferential ledge becomes the bottom footing; preserve its unscaled curvature.
poly=[(23,140),(44,146),(65,149),(82,146),(106,139),(106,142),(83,149),(65,152),(43,149),(23,143)]
mask=Image.new('1',im.size);ImageDraw.Draw(mask).polygon(poly,fill=1);ledge=im.copy();ledge.putalpha(Image.composite(im.getchannel('A'),Image.new('L',im.size),mask));out.alpha_composite(ledge,(0,40));records.append({'source_polygon':poly,'translation':[0,40]})
# Cap the original side piers using existing complete curved shoulder/foot pixels.
# No mirrored/scaled samples, invented colors or drawn seam smoothing.
out.save(O/'chemical-tower.png');before=Image.new('RGBA',(128,208),(40,44,40));before.alpha_composite(im);after=Image.new('RGBA',(128,208),(40,44,40));after.alpha_composite(out);review=Image.new('RGBA',(256,208),(40,44,40));review.alpha_composite(before);review.alpha_composite(after,(128,0));review.resize((768,624),Image.Resampling.NEAREST).save(O/'before-after-3x.png')
(O/'cuts.json').write_text(json.dumps({'source':str(src.relative_to(R)),'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'cuts':records,'inferred':'missing dome cap/rear stud tops above y48; lower wall continuation and foundation rim below y175','pixel_policy':'integer copies with binary masks; no recoloring/resampling/painting','ground_center':[64,186],'runtime_scale':1,'anchor':'Bottom','offset':[0,-10]},indent=2)+'\n')
print(O)
