"""Reference-palette derivatives only: originals, references and geometry unchanged."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
from collections import Counter
import colorsys,hashlib,json,numpy as np
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'mods/ca/bits/gdi/buildings';OUT=ART/'color-matched';OUT.mkdir(exist_ok=True);REPORT=ROOT/'art-source/gdi-color-match';REPORT.mkdir(exist_ok=True)
REFERENCES=('weapons factory.png','communication center.png')
def eligible(c):
 h,s,v=colorsys.rgb_to_hsv(*(x/255 for x in c));return c[0]>=75 and c[0]>c[1]>c[2] and c[0]-c[2]>=25 and .065<=h<=.18 and s>=.25
def lum(c):return sum(a*b for a,b in zip(c,(.2126,.7152,.0722)))
palette=set();reference_info={}
for name in REFERENCES:
 p=ART/name
 with Image.open(p) as im:counts=Counter(c[:3] for c in im.convert('RGBA').getdata() if c[3]==255)
 # Exclude pale warm stone/metal from the reference ramp: use actual gold paint.
 colors=[c for c,n in counts.items() if n>=24 and eligible(c) and colorsys.rgb_to_hsv(*(v/255 for v in c))[1]>=.4];palette.update(colors);reference_info[name]={'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'colors':colors}
palette=sorted(palette,key=lambda c:(lum(c),c));entries=[];bindings={};previews=[]
files=[p for p in sorted(ART.glob('*.png')) if p.name not in REFERENCES and not p.name.startswith('bib')]
files += [ART/'experimental/heavy-blaster-base.png',ART/'experimental/heavy-blaster-turret32.png']
for p in files:
 with Image.open(p) as im:a=np.array(im.convert('RGBA'));text=im.text.copy()
 b=a.copy();colors=np.unique(a[:,:,:3].reshape(-1,3),axis=0);mapping={tuple(map(int,c)):min(palette,key=lambda x:(abs(lum(x)-lum(c)),x)) for c in colors if eligible(c)}
 # Preserve the power plant's orange reactor glass/emission, not painted armor.
 protected=Image.new('L',(a.shape[1],a.shape[0]))
 if p.name=='power plant.png':ImageDraw.Draw(protected).polygon([(98,119),(138,114),(141,170),(127,183),(100,172)],fill=255)
 protect=np.array(protected)>0
 for old,new in mapping.items():b[np.all(a[:,:,:3]==old,axis=2)&(a[:,:,3]>0)&~protect,:3]=new
 changed=np.any(a!=b,axis=2);assert np.array_equal(a[:,:,3],b[:,:,3]);assert np.array_equal(a[protect],b[protect])
 metadata=PngInfo()
 for k,v in text.items():metadata.add_text(k,v)
 target=OUT/p.name;Image.fromarray(b).save(target,pnginfo=metadata);Image.fromarray((changed*255).astype(np.uint8)).save(REPORT/(p.stem+'-mask.png'));protected.save(REPORT/(p.stem+'-protected.png'))
 oldpath='bits/gdi/buildings/'+p.relative_to(ART).as_posix();newpath='bits/gdi/buildings/color-matched/'+p.name;bindings[oldpath]=newpath
 entries.append({'source':oldpath,'output':newpath,'source_sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'size':[a.shape[1],a.shape[0]],'metadata':text,'changed_pixels':int(changed.sum()),'mapping':[{'from':list(k),'to':list(v)} for k,v in mapping.items()]})
 if p.name=='heavy-blaster-base.png':continue
 if p.name=='heavy-blaster-turret32.png':
  before=Image.open(ART/'experimental/heavy-blaster-base.png').convert('RGBA');after=Image.open(OUT/'heavy-blaster-base.png').convert('RGBA');x,y=4*160,160
  # Native turret frame12; six-right/four-down centered attachment.
  pair=[]
  for base,data in ((before,a),(after,b)):
   v=Image.new('RGBA',(192,192));v.alpha_composite(base,(16,16));v.alpha_composite(Image.fromarray(data).crop((x,y,x+160,y+160)),(22,20));pair.append(v)
 else:pair=[Image.fromarray(a),Image.fromarray(b)]
 previews.append((p.stem,pair))
# Add only Filename overrides. All offsets, facings, scales and effects stay inherited.
lines=['# Generated color-only overrides; see art-source/gdi-color-match/README.md.']
nodes={};actor=state=None
for line in (ROOT/'mods/ca/sequences/gdi-building-art.yaml').read_text().splitlines():
 if line and not line.startswith(('\t','#')):actor=line.split(':')[0]
 elif line.startswith('\t') and not line.startswith('\t\t'):state=line.strip().split(':')[0]
 elif line.startswith('\t\tFilename: '):
  old=line.split(': ',1)[1]
  if old in bindings:nodes.setdefault(actor,{})[state]=bindings[old]
for actor,states in nodes.items():
 lines+=['',actor+':']
 for state,path in states.items():lines += ['\t'+state+':','\t\tFilename: '+path]
(ROOT/'mods/ca/sequences/gdi-building-colors.yaml').write_text('\n'.join(lines)+'\n')
# Paired native-resolution thumbnails (large buildings downsampled for review only).
w=960;cellw=480;cellh=300;canvas=Image.new('RGB',(w,cellh*((len(previews)+1)//2)),'#77736e');d=ImageDraw.Draw(canvas)
for i,(name,pair) in enumerate(previews):
 x=(i%2)*cellw;y=(i//2)*cellh;d.text((x+6,y+5),name+' / before | matched',fill='white')
 for j,im in enumerate(pair):
  im=im.copy();im.thumbnail((230,270),Image.Resampling.NEAREST);canvas.paste(im,(x+j*240+(240-im.width)//2,y+25+(270-im.height)//2),im)
canvas.save(REPORT/'before-after.png')
context_items=[(name+' / REFERENCE',Image.open(ART/name).convert('RGBA')) for name in REFERENCES]+[(name,pair[1]) for name,pair in previews]
context=Image.new('RGB',(1200,320*((len(context_items)+3)//4)),'#77736e');draw=ImageDraw.Draw(context)
for i,(name,im) in enumerate(context_items):
 x=(i%4)*300;y=(i//4)*320;im=im.copy();im.thumbnail((286,285),Image.Resampling.NEAREST);context.paste(im,(x+(300-im.width)//2,y+30+(285-im.height)//2),im);draw.text((x+4,y+5),name,fill='white')
context.save(REPORT/'matched-with-references.png')
(REPORT/'binding.json').write_text(json.dumps({'references':reference_info,'target_palette':palette,'method':'source palette luminance matching on yellow/gold only; no global brightness normalization','entries':entries,'sequence_images':nodes,'originals_unchanged':True},indent=2)+'\n');print('GDI_COLOR_DERIVATIVES_READY',len(entries),'files;',len(palette),'reference colors')
