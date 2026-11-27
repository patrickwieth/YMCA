"""Bounded alpha-only removal of enclosed radar mast background; no recoloring."""
from pathlib import Path
from PIL import Image,ImageDraw
import hashlib,json
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'art-source/allied-radar-helipad'
SOURCE=ART/'supplied-radar dome.png'
# Interior faces only. Outer black contour, central posts, antennas, dishes,
# cables, dome and machinery are outside these polygons.
FACES=[[(35,62),(38,64),(38,146),(29,156),(30,140)],
       [(43,64),(47,67),(51,149),(44,156)],
       [(104,55),(105,55),(105,67),(103,69)],
       [(109,55),(111,56),(111,66),(109,66)],
       [(163,60),(164,60),(164,71),(162,71)],
       [(167,60),(169,61),(169,73),(167,71)],
       [(129,36),(130,36),(130,66),(129,66)],
       [(133,35),(134,35),(134,65),(133,65)]]
def main():
    im=Image.open(SOURCE).convert('RGBA');before=im.copy()
    region=Image.new('L',im.size);draw=ImageDraw.Draw(region)
    for face in FACES:draw.polygon(face,fill=255)
    mask=Image.new('L',im.size);removed=[]
    for y in range(im.height):
        for x in range(im.width):
            # This exact repeated fill is the enclosed void, not a blanket black key.
            if region.getpixel((x,y)) and im.getpixel((x,y))==(0,1,7,255):
                im.putpixel((x,y),(0,1,7,0));mask.putpixel((x,y),255);removed.append([x,y])
    im.save(ROOT/'mods/ca/bits/allies/buildings/radar dome.png');mask.save(ART/'mast-void-mask.png')
    (ART/'mast-cutout.json').write_text(json.dumps({'sourceSHA256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'removedPixels':removed,'faces':FACES},indent=2)+'\n')
    sheet=Image.new('RGB',(1024,512),'#c5a679')
    for i,source in enumerate([before,im]):
        bg=Image.new('RGBA',source.size,'#c5a679');bg.alpha_composite(source)
        sheet.paste(bg.resize((512,512),Image.Resampling.NEAREST),(i*512,0))
    sheet.save(ART/'mast-cutout-review.png')
    print('Opened',len(removed),'bounded mast-void pixels; RGB and all other alpha unchanged.')
if __name__=='__main__':main()
