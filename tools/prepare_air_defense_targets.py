"""Shared disposable-map cleanup and stationary air targets; never edit the source map."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import re,sys
ROOT=Path(__file__).resolve().parents[1]
ACTORS=re.compile(r'(?m)^\t([^\t\n:]+): ([^\n]+)\n((?:\t\t[^\n]*\n)*)')
def prepare(text):
 text=text.replace('\r\n','\n')
 start=text.index('Actors:\n')+len('Actors:\n')
 end=re.search(r'(?m)^[^\t\n][^\n]*:',text[start:])
 stop=start+end.start() if end else len(text)
 body=text[start:stop]
 body=ACTORS.sub(lambda m:'' if m[2].strip() in ('airs','sentry.soviet.voxel','aa.practice') else m[0],body)
 # Dedicated legacy generator also gets the sprite Sentry in the Soviet display slot.
 body=re.sub(r'(?m)^(\tActor27: )silo$',r'\1sentry.soviet',body)
 found={m[1]:m for m in ACTORS.finditer(body)}
 # Keep the NK flak slot and add the Russia/Iraq SAM in a free nearby slot.
 if 'SovietSamDisplay' not in found:
  x,y=map(int,re.search(r'Location: (-?\d+),(-?\d+)',found['Actor37'][3]).groups())
  body+=f'\tSovietSamDisplay: sam\n\t\tOwner: Multi0\n\t\tLocation: {x+6},{y-6}\n\t\tFacing: 384\n'
 found={m[1]:m for m in ACTORS.finditer(body)}
 for actor,kind in [('Actor14','nsam'),('Actor37','flak.soviet.sprite'),('SovietSamDisplay','sam'),('Actor59','agun'),('Actor80','cram')]:
  m=found[actor];assert m[2].strip()==kind,(actor,m[2])
  x,y=map(int,re.search(r'Location: (-?\d+),(-?\d+)',m[3]).groups())
  body+=f'\tAirTarget{actor}: aa.practice\n\t\tOwner: Multi0\n\t\tLocation: {x+3},{y+1}\n\t\tFacing: 256\n'
 text=text[:start]+body+text[stop:]
 rule='\tAA.PRACTICE:\n\t\tInherits: TRAN\n\t\t-Buildable:\n\t\tTooltip:\n\t\t\tName: Unarmed AA practice target\n\t\tHealth:\n\t\t\tHP: 10000000\n\t\tAircraft:\n\t\t\tCruiseAltitude: 2c512\n\t\t\tTakeOffOnCreation: true\n\t\t\tIdleBehavior: None\n\t\t\tRepulsable: false\n'
 if '\tAA.PRACTICE:\n' not in text:text=text.replace('Rules:\n','Rules:\n'+rule,1)
 return text
if __name__=='__main__':
 path=Path(sys.argv[1]).resolve()
 if path==(ROOT/'mods/ca/maps/testmap1.oramap').resolve():raise ValueError('Refusing to modify authoritative source map')
 with ZipFile(path) as z:files={n:z.read(n) for n in z.namelist()}
 files['map.yaml']=prepare(files['map.yaml'].decode('utf-8-sig')).encode('utf-8')
 with ZipFile(path,'w',ZIP_DEFLATED) as z:
  for n,data in files.items():z.writestr(n,data)
 print('Test map: airstrip/voxel Sentry removed; SAM plus flak displayed; five unarmed airborne practice targets added.')
