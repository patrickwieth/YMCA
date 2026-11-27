"""Extend the approved Allied cameo treatment without rewriting the two power icons."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, json
from prepare_allied_power_icons import make_icon

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT/'art-source/allied-building-icons'
OUT = ROOT/'mods/ca/bits/allies/icons'
BUILDINGS = ROOT/'mods/ca/bits/allies/buildings'
# id, resolved sequence image, label, original PNG, deliberate source-space close-up
ITEMS = [
 ('conyard','fact.allies.icons','Con Yard','conyard.png',(12,18,292,228)),
 ('barracks','tent.allies','Barracks','barracks.png',(0,5,192,149)),
 ('refinery','proc.allies','Ore Refinery','refinery.png',(70,10,310,190)),
 ('war-factory','weap.allies','War Factory','war factory2.png',(22,25,294,229)),
 ('radar','dome.allies','Radar Station','radar dome.png',(14,33,254,213)),
 ('service-depot','fix.allies','Service Depot','service depot.png',(8,40,256,226)),
 ('airfield','afld.allies.custom','Airfield','airfield.png',(64,0,252,141)),
 ('helipad','hpad.allies','Helipad','helipad.png',(30,15,294,213)),
 ('naval-yard','syrd.allies','Naval Yard','shipyard.png',(26,0,258,174)),
 ('tech-center','atek.allies','Tech Center','tech center.png',(16,35,248,209)),
 ('ore-purifier','orep.allies','Ore Purifier','purifier.png',(32,14,224,158)),
 ('chronosphere','pdox.allies','Chronosphere','chronosphere.png',(22,25,254,199)),
 ('weather-control','weat.allies','Weather Ctrl','weather control.png',(30,25,246,187)),
 ('prism-tower','pris.allies','Prism Tower','prism tower.png',(12,13,116,91)),
 ('gap-generator','gap.allies','Gap Generator','euro_forces_gap_gene...-1070714859-0.png',(45,0,213,126)),
 ('pillbox','pbox.allies','Pillbox','bunker.png',(-25,21,155,156)),
]
FAKES = {'fact.allies.icons':'Fake Con Yard','weap.allies':'Fake Factory','dome.allies':'Fake Radar','syrd.allies':'Fake Shipyard'}
PARENTS = {'fact.allies.icons':'fact','gun.allies.icons':'gun','silo.allies.icons':'silo',
           'swiss_bank.allies.icons':'swiss_bank','oilp.allies.icons':'oilp','oil_platform.allies.icons':'oil_platform'}

def allied_palette(source, td=False):
    # Same overlay blend as the engine's OverlayPlayerColorPalette, restricted
    # to its player-color indices. Fixed Allied blue is appropriate for a cameo.
    image=Image.open(source)
    palette=image.getpalette()
    for i in range(176,192) if td else range(80,96):
        rgb=palette[3*i:3*i+3]
        bw=max(0,(sum(rgb)//3)/255-(.14 if td else .13))
        palette[3*i:3*i+3]=[int(255*(1-(1-2*(bw-.5))*(1-c/255) if bw>.5 else 2*bw*c/255)) for c in (56,111,173)]
    image.putpalette(palette)
    return image.convert('RGBA')

def fit_box(im):
    x0,y0,x1,y1 = im.getbbox()
    w,h = x1-x0+8,y1-y0+8
    w=max(w,h*4/3);h=w*3/4
    cx,cy=(x0+x1)/2,(y0+y1)/2
    return tuple(round(v) for v in (cx-w/2,cy-h/2,cx+w/2,cy+h/2))

def sequence(name,filename,label):
    return (f'\t{name}:\n\t\tFilename: bits/allies/icons/{filename}.png\n'
            '\t\tStart: 0\n\t\tLength: 1\n\t\tFacings: 1\n\t\tOffset: 0,0\n\t\tScale: 1\n'
            f'\t\tButtonize: true\n\t\tButtonStyle: ra1\n\t\tButtonLabel: {label}\n'
            '\t\tButtonLabelFont: OsShp\n\t\tButtonLabelOffset: 0,-2\n')

def main():
    ART.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
    (ART/'masters').mkdir(exist_ok=True)
    jobs=[]
    for key,image,label,filename,box in ITEMS:
        source=BUILDINGS/filename
        jobs.append((key,image,label,Image.open(source).convert('RGBA'),box,[source]))
    # Native flak sheets use 256x256 frames, eight across. Facing 24 exposes all four barrels.
    base=BUILDINGS/'flak-base.png';turret=BUILDINGS/'flak-turret32.png'
    flak=Image.open(base).convert('RGBA');sheet=Image.open(turret).convert('RGBA')
    flak.alpha_composite(sheet.crop((0,768,256,1024)))
    # Native turret Z offset 656 projects to 29 px; -5 turret vs -34 base sequence
    # offsets cancel that projection, so both source canvases register at the same origin.
    flak.save(ART/'sources/aa-gun-composite.png')
    jobs.append(('aa-gun','agun.native','AA Gun',flak,(55,40,231,172),[base,turret]))
    for key,image,label in [('grand-cannon','grand_cannon','Grand Cannon'),('swiss-bank','swiss_bank.allies.icons','Swiss Bank'),('turret','gun.allies.icons','Turret'),('silo','silo.allies.icons','Silo')]:
        source=ART/'sources'/f'{key}.png';im=allied_palette(source,td=key=='swiss-bank')
        box=(47,17,183,119) if key=='swiss-bank' else fit_box(im)
        jobs.append((key,image,label,im,box,[source]))
    source=ROOT/'mods/ca/bits/misc/oil-0001.png'
    pump=Image.open(source).convert('RGBA').crop((0,0,63,88))
    jobs.append(('oil-pump','oilp.allies.icons','Oil Pump',pump,(-15,9,89,87),[source]))
    source=ROOT/'mods/ca/bits/terrain/environment/oil-platform.png'
    jobs.append(('oil-platform','oil_platform.allies.icons','Oil Platform',Image.open(source).convert('RGBA'),(42,53,242,203),[source]))
    preview=Image.new('RGB',(1000,((len(jobs)+4)//5)*175),(27,33,42));draw=ImageDraw.Draw(preview)
    yaml='# Icon-only overrides. Source building sequences and gameplay are preserved.\n'
    records=[]
    for i,(key,image,label,im,box,sources) in enumerate(jobs):
        master=make_icon(im,box);master.save(ART/'masters'/f'{key}.png')
        icon=master.resize((192,144),Image.Resampling.LANCZOS);icon.save(OUT/f'{key}.png')
        x,y=i%5*200,i//5*175;preview.paste(icon,(x+4,y));draw.text((x+5,y+148),label,fill='white')
        yaml+=f'\n{image}:\n'
        if image in PARENTS:yaml+=f'\tInherits: {PARENTS[image]}\n'
        yaml+=sequence('icon',key,label)
        if image in FAKES:yaml+=sequence('fake-icon',key,FAKES[image])
        if image=='pbox.allies':yaml+=sequence('camo-icon',key,'Camo Pillbox')
        records.append({'id':key,'image':image,'label':label,'crop':box,'sources':[
            {'path':s.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(s.read_bytes()).hexdigest()} for s in sources]})
    (ROOT/'mods/ca/sequences/allied-building-icons.yaml').write_text(yaml)
    preview.save(ART/'comparison.png')
    (ART/'provenance.json').write_text(json.dumps(records,indent=2)+'\n')
    print(f'Prepared {len(jobs)} additional building cameos; approved power icons untouched.')

if __name__=='__main__':main()
