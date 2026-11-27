"""Source-derived dirt/sand roofs and ramps; preserve original alpha, registration and rock pixels."""
from pathlib import Path
import json,hashlib
from PIL import Image,PngImagePlugin
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'mods/ca/bits/terrain/rubberduck';OUT=BASE/'derived/biomes'
def save(im,source,path):
    source.load();meta=PngImagePlugin.PngInfo()
    for k,v in source.info.items():
        if isinstance(v,str):meta.add_text(k,v)
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path,pnginfo=meta)
records=[]
for biome,material in [('barren','dirt_a_base'),('desert','sand_base')]:
    tex=Image.open(BASE/(material+'.png')).convert('RGBA').crop((0,0,128,64))
    def sample(x,y):
        p=tex.getpixel((x%128,y%64));return p if p[3]==255 else tex.getpixel((64,32))
    for shaded in [False,True]:
        suffix='-shaded' if shaded else ''
        texture=Image.open(BASE/(material+('_shaded' if shaded else '')+'.png')).convert('RGBA').crop((0,0,128,64))
        for ramp in range(5):
            name='plateau-roof'+suffix+'.png' if ramp==0 else f'plateau-ramp{ramp}{suffix}.png'
            src=Image.open(BASE/'derived'/name);src.load();old=src.convert('RGBA');im=old.copy()
            for y in range(im.height):
                for x in range(im.width):
                    a=old.getpixel((x,y))[3]
                    if not a:continue
                    if ramp:
                        mask={1:6,2:12,3:9,4:3}[ramp];n=64-32*(mask&1);ay=96-32*((mask>>1)&1)-n;by=96-32*((mask>>3)&1)-n
                        dx=x+.5-64;dy=y+.5-n;u=(dx*by+64*dy)/(64*(ay+by));v=(64*dy-dx*ay)/(64*(ay+by))
                        sx=max(0,min(127,int(64+64*(u-v))));sy=max(0,min(63,int(32*(u+v))))
                        if texture.getpixel((sx,sy))[3]!=255:
                            u=max(.06,min(.94,u));v=max(.06,min(.94,v));sx=int(64+64*(u-v));sy=int(32*(u+v))
                    else:sx=x;sy=y
                    im.putpixel((x,y),texture.getpixel((sx,sy))[:3]+(a,))
            save(im,src,OUT/biome/name)
    for path in (BASE/'derived/closed-cliffs').glob('closed-cliff-*.png'):
        src=Image.open(path);src.load();im=src.convert('RGBA');changed=0
        for y in range(im.height):
            for x in range(im.width):
                r,g,b,a=im.getpixel((x,y))
                if a and g>r+8 and g>b+8:
                    # Only the green cap/fringe, not the stone faces or their alpha.
                    rgb=sample(x,y)[:3];shade=max(.55,min(1.35,(r+g+b)/210))
                    im.putpixel((x,y),tuple(min(255,round(c*shade)) for c in rgb)+(a,));changed+=1
        save(im,src,OUT/biome/path.name);records.append({'biome':biome,'source':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'greenPixels':changed})
art=ROOT/'art-source/map-biome-followup';art.mkdir(exist_ok=True);(art/'biome-art.json').write_text(json.dumps(records,indent=2))
print('Generated source-derived biome roofs, ramps and green-fringe replacements; originals untouched.')
