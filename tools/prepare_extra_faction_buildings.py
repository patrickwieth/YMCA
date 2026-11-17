"""Add China (upper left) and Scrin (upper right) to the disposable building grid.
Never rewrites the authoritative source. No existing actor positions or global rules change.
"""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import re,sys
ROOT=Path(__file__).resolve().parents[1]
GROUPS={
 'China': ('chinatnk',(36,-12),[
  'chcommand','chpower','chpower2','chbarracks','chsupply',
  'chweap','chpropaganda','chinternetcenter','chairport','chsyrd',
  'chgatling','chbunker','chbunker2','chpropagandatower','chsilo',
  'chbunker.advinf','chbunker2.advinf','chpropagandatower.pdl']),
 'Scrin': ('reaper',(90,-66),[
  'sfac','reac','rea2','proc.scrin','port',
  'wsph','nerv','scrt','grav','sign',
  'srep','mani','rift','ptur','shar',
  'scol','swal'])}
# Not stand-alone player buildings: radar plug, AI duplicates, temporary cloud/column,
# docking helper, nonbuildable Nukular variants; disabled resource silos remain omitted.
def placements():
 for group,(faction,(x,y),names) in GROUPS.items():
  for i,name in enumerate(names):
   row,col=divmod(i,5)
   fac='chinaatom' if name=='chpower2' else 'chinainf' if name.endswith('.advinf') else faction
   yield f'Test{group}{i:02d}',name,(x+col*6+row*5,y-col*6+row*5),fac

def prepare(text):
 text=text.replace('\r\n','\n')
 # Idempotent, only replace our own generated actor IDs.
 text=re.sub(r'(?m)^\tTest(?:China|Scrin)\d+: [^\n]+\n(?:\t\t[^\n]*\n)*','',text)
 start=text.index('Actors:\n')+len('Actors:\n')
 actors=''.join(f'\t{actor}: {name}\n\t\tOwner: Multi0\n\t\tLocation: {x},{y}\n\t\tFaction: {faction}\n' for actor,name,(x,y),faction in placements())
 text=text[:start]+actors+text[start:]
 # Rubberduck terrain has no 'Wall' preview color. Keep the live wall, omit only
 # its generated minimap signature in this disposable map (no global trait edit).
 if '\tSWAL:\n' not in text:
  text=text.replace('Rules:\n','Rules:\n\tSWAL:\n\t\t-AppearsOnMapPreview:\n\t\tInteractable:\n',1)
 return text

if __name__=='__main__':
 path=Path(sys.argv[1]).resolve()
 if path==(ROOT/'mods/ca/maps/testmap1.oramap').resolve():raise ValueError('Source map is immutable')
 with ZipFile(path) as z:files={n:z.read(n) for n in z.namelist()}
 files['map.yaml']=prepare(files['map.yaml'].decode('utf-8-sig')).encode()
 with ZipFile(path,'w',ZIP_DEFLATED) as z:
  for n,data in files.items():z.writestr(n,data)
 print('Building test: China upper-left (18), Scrin upper-right (17); existing positions preserved.')
