"""Low grass-to-water rock banks from bounded native cliff-face cuts.
RGB is sampled only from the original rock faces, at approximately native pixel
spacing. Only alpha is feathered; no roof/cap, cave, noise or foreign image details.
"""
from pathlib import Path
import hashlib,json,math
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]

def clamp(x): return max(0.,min(1.,x))
def smooth(x): x=clamp(x);return x*x*(3-2*x)
def main():
    source=ROOT/'mods/ca/bits/terrain/rubberduck/rock_cliffs.png'
    atlas=Image.open(source).convert('RGBA')
    output=source.parent/'derived/handcrafted/banks';output.mkdir(parents=True,exist_ok=True)
    # Front faces have opaque rock surfaces rather than the raised rear roof lip.
    faces=[atlas.crop((slot*128,0,(slot+1)*128,256)) for slot in [5,6,7]]
    for mask in range(1,16):
        for variant in range(3):
            image=Image.new('RGBA',(192,128))
            for y in range(128):
                for x in range(192):
                    px=x+.5-96;py=y+.5-64
                    u=px/128+py/64+.5;v=py/64-px/128+.5
                    options=[(u-1,v),(v-1,u),(-u,v),(-v,u)]
                    best=(0,0,0,0)
                    for direction,(depth,tangent) in enumerate(options):
                        if not mask&(1<<direction) or not -.35<depth<.18 or not -.12<tangent<1.12:continue
                        alpha=smooth((depth+.35)/.12)*smooth((.18-depth)/.10)
                        alpha*=smooth((tangent+.12)/.12)*smooth((1.12-tangent)/.12)
                        sx=max(16,min(95,round(24+tangent*64)))
                        sy=max(75,min(160,round(96-sx/2+24+(depth+.35)*64)))
                        r,g,b,a=faces[(variant+direction)%3].getpixel((sx,sy))
                        sample=(r,g,b,round(a*alpha))
                        if sample[3]>best[3]:best=sample
                    image.putpixel((x,y),best)
            image.save(output/f'bank-{mask}-{variant}.png')
    (output/'provenance.json').write_text(json.dumps({'source':str(source.relative_to(ROOT/'mods/ca')),'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'slots':[5,6,7],'source_bounds_in_slot':[16,75,96,161],'size':[192,128],'description':'Bounded source RGB samples, native-scale tangent/depth mapping, alpha-only bank feather. No raised roof.'},indent=2)+'\n')

if __name__=='__main__':main()
