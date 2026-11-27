"""Direct-PNG building cameos. Does not write any building artwork or Allied icon."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib
from prepare_allied_power_icons import make_icon
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'art-source/faction-building-icons'
OUT=ROOT/'mods/ca/bits/faction-icons'
COMPOSITES={'atwr','chgatling','cram','flak-soviet-sprite','ftur','ion_defense','ltur','nsam','ptur','sam','shar','stwr'}
def parse(text):
    result={};current=None
    for line in text.splitlines():
        if not line.startswith('\t'):
            current=line.split(':',1)[0];result[current]={}
        elif not line.startswith('\t\t') and ':' in line:
            key,value=line.strip().split(':',1);result[current][key]=value.strip()
    return result

def crop_box(im,key):
    x0,y0,x1,y1=im.getchannel('A').getbbox();w=x1-x0;h=y1-y0
    # Source-space close-up, retaining the recognizable roof/head and upper body.
    width=w*.86
    if h>w*1.15:width=max(width,h*.75)
    width=max(width,32);height=width*.75
    cx=(x0+x1)/2;cy=y0+min(h*.46,height*.51)
    if key in ('gtwr','hand','obli','tsla','chpropagandatower'):cy=y0+height*.46
    return tuple(round(v) for v in (cx-width/2,cy-height/2,cx+width/2,cy+height/2))

def sequence(file,label):
    return (f'\ticon:\n\t\tFilename: {file}\n\t\tStart: 0\n\t\tLength: 1\n\t\tFacings: 1\n'
            '\t\tOffset: 0,0\n\t\tScale: 1\n\t\tButtonize: true\n\t\tButtonStyle: ra1\n'
            f'\t\tButtonLabel: {label}\n\t\tButtonLabelFont: OsShp\n\t\tButtonLabelOffset: 0,-2\n')

def main():
    OUT.mkdir(exist_ok=True);(ART/'masters').mkdir(exist_ok=True)
    catalog=json.loads((ART/'catalog.json').read_text())
    bindings=json.loads((ART/'source-bindings.json').read_text())
    yaml='# Icon-only overrides; building art and gameplay remain unchanged.\n';records=[]
    for c in catalog:
        key=c['key'];binding=bindings[key];path=ROOT/binding['source']
        im=Image.open(path).convert('RGBA')
        if 'frame' in binding:im=im.crop(binding['frame'])
        if key=='chcommand-radar':
            # The plug's placement preview uses the same two supplied dishes,
            # without a cameo background or lettering. Original PNG stays intact.
            preview=ROOT/'mods/ca/bits/misc/overlays/china-radar-preview.png'
            preview.parent.mkdir(parents=True,exist_ok=True);im.save(preview)
        box=c.get('directCrop',crop_box(im,key))
        if binding['method'] != 'direct PNG' and 'directCrop' not in c:
            x0,y0,x1,y1=im.getchannel('A').getbbox()
            w=max((x1-x0)*1.06,(y1-y0)*4/3*1.06);h=w*.75
            cx,cy=(x0+x1)/2,(y0+y1)/2
            box=tuple(round(v) for v in (cx-w/2,cy-h/2,cx+w/2,cy+h/2))
        master=make_icon(im,box);master.save(ART/'masters'/f'{key}.png')
        icon=master.resize((192,144),Image.Resampling.LANCZOS);icon.save(OUT/f'{key}.png')
        entry=sequence(f'bits/faction-icons/{key}.png',c['label'])
        if key=='chpower2':entry=entry.replace('\ticon:\n','\ticonadv:\n',1)
        yaml+=f"\n{c['image']}:\n"+entry
        records.append(dict(id=key,image=c['image'],actors=c['actors'],family=c['family'],crop=box,**binding,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    # Reuse approved income portraits for other factions without touching the originals.
    for image,file,label in [('oilp','oil-pump','Oil Pump'),('oil_platform','oil-platform','Oil Platform'),('swiss_bank','swiss-bank','Swiss Bank')]:
        yaml+=f'\n{image}:\n'+sequence(f'bits/allies/icons/{file}.png',label)
    (ROOT/'mods/ca/sequences/faction-building-icons.yaml').write_text(yaml)
    (ART/'provenance.json').write_text(json.dumps(records,indent=2)+'\n')
    for family in sorted({c['family'] for c in catalog}):
        rows=[c for c in catalog if c['family']==family];sheet=Image.new('RGB',(1000,((len(rows)+4)//5)*175),(22,29,36));draw=ImageDraw.Draw(sheet)
        for i,c in enumerate(rows):
            x=i%5*200;y=i//5*175;sheet.paste(Image.open(OUT/f"{c['key']}.png"),(x+4,y));draw.text((x+4,y+148),c['label'],fill='white')
        sheet.save(ART/f'{family}-direct-review.png')
    print(f'{len(records)} portraits: '+str(sum(r['method']=='direct PNG' for r in records))+' direct PNG sources; remaining multipart/legacy.')
if __name__=='__main__':main()
