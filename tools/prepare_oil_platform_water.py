"""Reproduce the retired source-foam study for archival review only (not runtime)."""
from pathlib import Path
from PIL import Image
import math,json
root=Path(__file__).resolve().parents[1]
archive=root/'art-source/oil-platform'
source=Image.open(archive/'original.png').convert('RGBA')
body=Image.open(root/'mods/ca/bits/terrain/environment/oil-platform.png').convert('RGBA')
water=Image.new('RGBA',body.size)
# Source foam was lower than the repaired ends: translate only these small cuts.
piers=[(54,76,190,4,18,6,2),(114,141,217,4,21,6,4),(179,201,190,4,18,6,1)]
for x0,x1,edge,depth,rx,ry,source_dy in piers:
 cx=(x0+x1)/2;radius=(x1-x0)/2
 for y in range(edge-ry,edge+ry+1):
  for x in range(math.floor(cx-rx),math.ceil(cx+rx)+1):
   distance=((x-cx)/rx)**2+((y-edge)/ry)**2
   if distance>=1:continue
   r,g,b,a=source.getpixel((x,y+source_dy))
   if not(b>r and g>r and b>=150):continue
   if body.getpixel((x,y))[3]:
    bottom=edge+round(depth*math.sqrt(max(0,1-((x-cx)/radius)**2)))
    if not x0<=x<=x1 or y<bottom:continue
   # Bright foam stays crisp near the pier; low-contrast ripples and patch edges
   # blend into the actual map water, rather than importing a sea-colored tile.
   strength=205 if b>=225 else 160 if b>=190 else 105
   alpha=round(strength*min(1,(1-distance)/.25))
   water.putpixel((x,y),(r,g,b,alpha))
water.save(archive/'water-contact-retired.png')
review=Image.new('RGBA',body.size,(55,94,118,255));review.alpha_composite(body);review.alpha_composite(water)
review.resize((768,768),Image.Resampling.NEAREST).save(archive/'water-contact-review.png')
(archive/'water-contact-provenance.json').write_text(json.dumps({'source':'original.png','operation':'Three bounded original foam/ripple cuts, translated upward to meet the repaired pier contours. Soft alpha at outer edges; no sea background, structural changes or unrelated sprite reuse.','piers':piers},indent=2)+'\n')
