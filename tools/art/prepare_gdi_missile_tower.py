"""Native source cuts only. Originals stay untouched; no runtime files are written."""
from pathlib import Path
from PIL import Image,ImageDraw
import hashlib,json
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/gdi missile tower';SRC=next(F.glob('*.png'));OUT=F/'source-cuts';OUT.mkdir(exist_ok=True)
im=Image.open(SRC).convert('RGBA');background=im.getpixel((0,0))[:3]
# Remove only near-white EXTERIOR background with a binary flood fill; retain original RGB.
from collections import deque
bg=set();queue=deque([(x,y) for x in range(im.width) for y in (0,im.height-1)]+[(x,y) for y in range(im.height) for x in (0,im.width-1)])
while queue:
 x,y=queue.popleft()
 if (x,y) in bg or not (0<=x<im.width and 0<=y<im.height) or min(im.getpixel((x,y))[:3])<245:continue
 bg.add((x,y));queue.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
for x,y in bg:
 r,g,b,_=im.getpixel((x,y));im.putpixel((x,y),(r,g,b,0))
im.save(OUT/'source-transparent.png')
PATCHES={'gold':(76,23,80,33),'goldlight':(61,104,69,108),'goldshade':(74,73,78,79),'steel':(86,41,90,44),'darksteel':(37,10,41,13),'black':(63,25,65,27),'steelbright':(61,0,65,2),'bearing-floor':(52,38,56,41),'vent':(34,68,42,77),'cyan':(62,64,67,67),'rim':(39,49,47,52),'goldedge':(39,59,47,64)}
records=[];sheet=Image.new('RGBA',(540,480),(44,47,44,255));d=ImageDraw.Draw(sheet)
for i,(name,rect) in enumerate(PATCHES.items()):
 cut=im.crop(rect);assert cut.getextrema()[3]==(255,255),name
 cut.save(OUT/(name+'.png'));records.append({'name':name,'source_rect':rect,'size':cut.size,'pixel_operation':'integer rectangular copy; unmodified RGBA'})
 x=(i%3)*180;y=(i//3)*120;sheet.alpha_composite(cut.resize((cut.width*6,cut.height*6),Image.Resampling.NEAREST),(x+8,y+28));d.text((x+8,y+7),name+' '+str(rect),fill='white')
sheet.save(OUT/'patch-review-6x.png')
# Keep the supplied stationary tower; remove rotating machinery above the bearing.
# The occluded bearing face is inferred ONLY by pasting a native dark-metal source cut.
base=im.copy();mask=Image.new('1',im.size);md=ImageDraw.Draw(mask)
md.polygon([(0,0),(127,0),(127,38),(102,38),(94,39),(90,43),(80,47),(67,49),(50,45),(38,36),(23,36),(0,36)],fill=1)
base.paste((0,0,0,0),(0,0),mask)
ringmask=Image.new('1',im.size);ImageDraw.Draw(ringmask).ellipse((34,36,96,53),fill=1)
patch=im.crop(PATCHES['bearing-floor']);fill=Image.new('RGBA',im.size)
for y in range(36,54,patch.height):
 for x in range(34,97,patch.width):fill.paste(patch,(x,y))
# Replacement confined to the hidden top face; all visible architecture below remains native.
from PIL import ImageChops
ringmask=ImageChops.logical_and(ringmask,mask)
base.paste(fill,(0,0),ringmask)
# User request: remove the foreground/right turbine clutter by a binary native-pixel cut.
# No invented tower pixels or painted ground beneath the occluding accessories.
clutter_polygon=[(105,89),(127,89),(127,127),(73,127),(73,119),(78,114),(85,111),(94,107),(99,100)]
clutter=Image.new('1',im.size);ImageDraw.Draw(clutter).polygon(clutter_polygon,fill=1)
base.paste((0,0,0,0),(0,0),clutter)
# Follow-up: reconstruct the concealed ground and side wall using whole source pixels.
repair=Image.new('RGBA',im.size);ground_rect=(0,112,18,128)
for y in (96,112):
 for x in (72,90,108,126):repair.paste(im.crop(ground_rect),(x,y))
# Uppermost removed sliver uses the same paving strip, clipped to the repair region.
for x in (90,108,126):repair.paste(im.crop(ground_rect),(x,80))
wall_polygon=[(73,119),(78,114),(85,111),(94,107),(99,100),(105,89),(109,101),(109,108),(76,127),(73,127)]
wallmask=Image.new('1',im.size);ImageDraw.Draw(wallmask).polygon(wall_polygon,fill=1)
wall=Image.new('RGBA',im.size)
wall_cuts=[((73,91,83,109),(73,111)),((82,79,97,103),(82,99)),((97,77,105,101),(97,93)),((99,82,103,98),(105,96))]
for rect,dst in wall_cuts:wall.paste(im.crop(rect),dst)
# Never replace an available paving pixel with an empty part of a wall cut.
opaque=wall.getchannel('A').point(lambda v:255 if v==255 else 0).convert('1')
repair.paste(wall,(0,0),ImageChops.logical_and(wallmask,opaque))
base.paste(repair,(0,0),clutter);base.save(OUT/'base.png')
records.append({'name':'removed-right-turbines','pixel_operation':'native cut-and-paste ground and concealed wall reconstruction; no painting/resizing','source_polygon':clutter_polygon,'ground_rect':ground_rect,'wall_polygon':wall_polygon,'wall_cuts':[{'source_rect':r,'destination':d} for r,d in wall_cuts]})
records.append({'name':'base','pixel_operation':'binary machinery mask; native copied bearing-floor patch only on inferred occluded bearing face; visible rim retained','bearing_inferred_ellipse':[34,36,96,53]})
(OUT/'manifest.json').write_text(json.dumps({'source':str(SRC.relative_to(ROOT)),'source_sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'background_rgb':background,'cuts':records,'policy':'No resizing/recoloring/painting in texture assets. Enlarged contact sheet is review only.'},indent=2)+'\n')
print(OUT)
