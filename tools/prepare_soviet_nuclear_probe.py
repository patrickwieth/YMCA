"""Isolated healthy/damaged Soviet nuclear plant art and foundation review."""
import sys,re,zipfile,struct
from pathlib import Path
from convert_handcrafted_layouts import miniyaml
src,dst=map(Path,sys.argv[1:3])
selected='--selected' in sys.argv
if dst.exists():raise SystemExit('Use a fresh output package.')
def cell(u,v):return u+(v+1)//2,v//2-u
with zipfile.ZipFile(src) as z:
 text=z.read('map.yaml').decode().replace('\r\n','\n');data=miniyaml(text);actors='Actors:\n';i=0
 for a in data.child('Actors').children:
  if a.value=='mpspawn':
   x,y=cell(10+50*i,10);actors+=f'\tspawn{i}: mpspawn\n\t\tOwner: Neutral\n\t\tLocation: {x},{y}\n';i+=1
 for i,u in enumerate([35] if selected else [35,43]):
  x,y=cell(u,42);actors+=f'\tplant{i}: apwr.soviet\n\t\tOwner: Multi0\n\t\tLocation: {x},{y}\n\t\tHealth: {100 if i==0 else 40}\n'
  actors+=f'\tscale{i}: engineer\n\t\tOwner: Multi0\n\t\tLocation: {x+5},{y+3}\n'
 text=re.sub(r'^Actors:\n.*?(?=^Rules:)',actors,text,flags=re.M|re.S)
 cx,cy=cell(40,42)
 rules=f'''Rules:
	^Autobattler:
		Autobattler:
			RequiresCondition: false
		ArmyFollower:
			RequiresCondition: false
	World:
		TerrainCalibrationView:
			Center: {cx},{cy}
			ZoomScale: 0.65
			MinimumZoomScale: 0.04
			ScreenshotTicks: 100
	Player:
		Shroud:
			FogCheckboxEnabled: false
			FogCheckboxLocked: true
			ExploredMapCheckboxEnabled: true
			ExploredMapCheckboxLocked: true
'''
 if selected:
  result=dst.with_suffix('.txt').resolve().as_posix()
  rules=rules.replace('\tWorld:\n',f'\tWorld:\n\t\tBuildingPlacementReviewProbe:\n\t\t\tActorType: apwr.soviet\n\t\t\tResultPath: {result}\n')
 text=text.replace('Rules:\n',rules)
 with zipfile.ZipFile(dst,'w',zipfile.ZIP_DEFLATED)as out:
  for n in z.namelist():
   b=z.read(n)
   if n=='map.yaml':b=text.encode()
   if n=='map.bin':
    b=bytearray(b);_,w,h,_,_,offset=struct.unpack_from('<BHHIII',b);b[offset:offset+2*w*h]=bytes(2*w*h)
   out.writestr(n,b)
