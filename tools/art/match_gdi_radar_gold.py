"""Match only the radar's authored gold ramp to the supplied GDI building palette.

The original is archived once. Generate a review first; --install explicitly
promotes the result to the existing authoritative runtime radar.png binding.
"""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT/'mods/ca/bits/gdi/buildings/radar.png'
OUT = ROOT/'art-source/gdi-radar-gold'
# Source radar tones -> authored Conyard / Barracks tones (not global grading).
RAMP = {
    (252,222,86): (241,209,95),
    (248,205,40): (236,198,69),
    (235,169,16): (221,172,57),
    (207,151,44): (206,152,55),
    (198,133,36): (196,139,50),
    (172,108,42): (172,117,52),
    (140,86,44): (147,103,63),
}


def recolor(original):
    result=original.copy()
    changed=0
    for y in range(original.height):
        for x in range(original.width):
            p=original.getpixel((x,y))
            if not p[3]: continue
            anchor=min(RAMP, key=lambda c: sum((a-b)**2 for a,b in zip(c,p[:3])))
            if max(abs(a-b) for a,b in zip(anchor,p[:3]))>2: continue
            rgb=tuple(max(0,min(255,RAMP[anchor][i]+p[i]-anchor[i])) for i in range(3))
            result.putpixel((x,y),rgb+(p[3],))
            changed+=1
    return result,changed


def generate(install=False):
    OUT.mkdir(parents=True,exist_ok=True)
    original_path=OUT/'radar-original.png'
    if not original_path.exists(): shutil.copy2(RUNTIME,original_path)
    original=Image.open(original_path).convert('RGBA')
    result,count=recolor(original)
    review=OUT/'radar-matched.png'
    result.save(review)
    (OUT/'manifest.json').write_text(json.dumps(dict(
        source_sha256=hashlib.sha256(original_path.read_bytes()).hexdigest(),
        result_sha256=hashlib.sha256(review.read_bytes()).hexdigest(),
        changed_pixels=count, size=original.size,
        ramp=[dict(source=a,target=b) for a,b in RAMP.items()]),indent=2)+'\n')
    if install:
        # Do not accidentally overwrite a newer supplied replacement on a rerun.
        current=hashlib.sha256(RUNTIME.read_bytes()).hexdigest()
        assert current in {hashlib.sha256(p.read_bytes()).hexdigest() for p in [original_path,review]}, 'Runtime radar changed since review.'
        shutil.copy2(review,RUNTIME)
    print(f'{count} gold pixels adjusted; alpha, metal, flags and outlines preserved.')


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--install',action='store_true')
    generate(parser.parse_args().install)
