"""Close the unsupported faces of native side corners without altering originals.
The donor face is translated at native pixel density, not stretched/projected.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'mods/ca/bits/terrain/rubberduck/derived'

def main():
    output=BASE/'handcrafted/closed-corners';output.mkdir(parents=True,exist_ok=True)
    records=[]
    for slot,direction,dx in [(26,1,-64),(30,0,64)]:
        corner=BASE/f'coasts/coast-piece-{slot}.png'
        face=BASE/f'coasts/coast-native-{direction}-0-0.png'
        image=Image.open(corner).convert('RGBA');donor=Image.open(face).convert('RGBA')
        for y in range(256):
            for x in range(128):
                top=96+x/2 if slot==26 else 160-x/2
                if (x<64 if slot==26 else x>=64) and top<=y<top+64:
                    pixel=donor.getpixel((x-dx,y-32))
                    if pixel[3]:image.putpixel((x,y),pixel)
        name=f'corner-{slot}.png';image.save(output/name)
        records.append({'file':name,'corner':str(corner.relative_to(ROOT/'mods/ca')),'face':str(face.relative_to(ROOT/'mods/ca')),
            'corner_sha256':hashlib.sha256(corner.read_bytes()).hexdigest(),'face_sha256':hashlib.sha256(face.read_bytes()).hexdigest(),
            'translation':[dx,32],'size':[128,256]})
    (output/'provenance.json').write_text(json.dumps(records,indent=2)+'\n')

if __name__=='__main__':main()
