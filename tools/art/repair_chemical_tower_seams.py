"""Repair the duplicated lower facade; preserve supplied source and all old passes."""
from pathlib import Path
from PIL import Image, ImageDraw
import json
R = Path(__file__).resolve().parents[2]
F = R / 'art-source/soviet chem tower'
O = F / 'pass-04'
O.mkdir(exist_ok=True)
src = Image.open(F / 'chemtower.png').convert('RGBA')
out = Image.open(F / 'pass-03/chemical-tower.png').convert('RGBA')
# Discard the repeated twelve-row facade (including duplicate vents and severed piers).
ImageDraw.Draw(out).rectangle((0,176,127,207), fill=(0,0,0,0))
# Continue each architectural panel separately with its own native plain-wall strip.
# No second copy of the vents, doorway or hazard markings.
cuts = []
def cut(rect, dest):
    out.alpha_composite(src.crop(rect), dest)
    cuts.append({'source': list(rect), 'destination': list(dest)})
# Plain panel donors retain the native light/dark split. Close the lower wall
# behind the curved footing; never repeat a recognizable vent or control panel.
wallmask=Image.new('L',out.size)
ImageDraw.Draw(wallmask).polygon([(18,175),(116,175),(115,183),(106,181),(83,188),(65,191),(43,188),(23,182),(18,184)],fill=255)
wall=Image.new('RGBA',out.size)
for left,right,donor in [(18,28,(23,162,26,166)),(28,40,(33,163,36,167)),
                        (40,49,(43,153,46,157)),(49,59,(52,152,55,156)),
                        (59,72,(74,153,77,157)),(72,85,(74,153,77,157)),
                        (85,96,(88,151,91,155)),(96,107,(100,158,103,162)),
                        (107,117,(110,161,113,165))]:
    # Use a native median-tone texel, not repeated high-contrast donor marks.
    pixels=[p for p in src.crop(donor).getdata() if p[3]]
    pixels.sort(key=lambda p:sum(p[:3]))
    tone=pixels[len(pixels)//2]
    ImageDraw.Draw(wall).rectangle((left,176,right-1,191),fill=tone)
    # A narrow structural seam uses the original panel edge, not a foreign detail.
    edge=src.getpixel((left,175))
    if edge[3]:ImageDraw.Draw(wall).line((left,176,left,188),fill=edge)
    cuts.append({'source':list(donor),'destination_panel':[left,176,right,192]})
wall.putalpha(Image.composite(wall.getchannel('A'),Image.new('L',out.size),wallmask))
out.alpha_composite(wall)
# Reuse the coherent curved ledge as one bounded part, not a rectangular overlay.
poly=[(23,140),(44,146),(65,149),(82,146),(106,139),(106,142),(83,149),(65,152),(43,149),(23,143)]
mask=Image.new('L',src.size);ImageDraw.Draw(mask).polygon(poly,fill=255)
ledge=src.copy();ledge.putalpha(Image.composite(src.getchannel('A'),Image.new('L',src.size),mask))
out.alpha_composite(ledge,(0,27))
# Finish at the nearest coherent curved footing instead of inventing a tall,
# blank skirt. Clip the formerly chopped pier ends along the same ground plane.
keep=Image.new('L',out.size)
ImageDraw.Draw(keep).polygon([(0,0),(127,0),(127,174),(116,177),(107,172),(83,176),(65,179),(43,176),(23,171),(18,177),(0,174)],fill=255)
out.putalpha(Image.composite(out.getchannel('A'),Image.new('L',out.size),keep))
clean=Image.new('RGBA',out.size)
clean.alpha_composite(out)
out=clean
out.save(O/'chemical-tower.png')
out.resize((512,832),Image.Resampling.NEAREST).save(O/'review-4x.png')
(O/'cuts.json').write_text(json.dumps({'source':'chemtower.png','cuts':cuts,'ledge_translation':[0,27],'inferred':'Lower wall closure and footing; supplied image is clipped.'},indent=2)+'\n')
