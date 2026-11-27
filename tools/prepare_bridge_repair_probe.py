"""Isolated engineer repair + replacement oil-art native review."""
import sys,re,zipfile,struct
from pathlib import Path
from convert_handcrafted_layouts import miniyaml
source,destination,result=map(Path,sys.argv[1:4])
if destination.exists():raise SystemExit('Use a fresh output package.')
with zipfile.ZipFile(source) as archive:
 text=archive.read('map.yaml').decode().replace('\r\n','\n');data=miniyaml(text)
 bridge=next(a for a in data.child('Actors').children if a.value.startswith('terrain.rubberduck.bridge.') and '.hut.' not in a.value)
 x,y=map(int,bridge.child('Location').value.split(',')); length=int(bridge.value.split('.')[-1]); along=bridge.value.split('.')[-2]=='y'
 cx=x+(0 if along else length//2);cy=y+(length//2 if along else 0)
 binary=archive.read('map.bin');_,w,h,tiles,_,_=struct.unpack_from('<BHHIII',binary)
 def water(x,y):
  v=x+y;u=(x-y)//2
  return 0<=u<w and 0<=v<h and struct.unpack_from('<H',binary,tiles+3*(u*h+v))[0]==1050
 blocked=set()
 for a in data.child('Actors').children:
  if a.value.startswith('terrain.rubberduck.bridge.') and '.hut.' not in a.value:
   bx,by=map(int,a.child('Location').value.split(','));ln=int(a.value.split('.')[-1]);ay=a.value.split('.')[-2]=='y'
   for i in range(ln):
    for dx in range(-2,3):
     for dy in range(-2,3):blocked.add((bx+(0 if ay else i)+dx,by+(i if ay else 0)+dy))
 candidates=[(cx+dx,cy+dy) for dx in range(-16,17) for dy in range(-16,17) if abs(dx)+abs(dy)>6 and all(water(cx+dx+i,cy+dy+j) and (cx+dx+i,cy+dy+j) not in blocked for i in range(4) for j in range(4))]
 if not candidates:raise SystemExit('No suitable nearby water footprint for oil art review.')
 ox,oy=min(candidates,key=lambda p:(p[0]-cx)**2+(p[1]-cy)**2)
 text=text.replace('Rules:\n',f'\toilArtReview: oil_platform\n\t\tOwner: Neutral\n\t\tLocation: {ox},{oy}\nRules:\n')
 rules=f'''Rules:
	^Autobattler:
		Autobattler:
			RequiresCondition: false
		ArmyFollower:
			RequiresCondition: false
	World:
		RubberduckBridgeRepairProbe:
			ResultPath: {result.as_posix()}
		TerrainCalibrationView:
			Center: {cx},{cy}
			ZoomScale: 0.5
			MinimumZoomScale: 0.04
			ScreenshotTicks: 100
	Player:
		Shroud:
			FogCheckboxEnabled: false
			FogCheckboxLocked: true
			ExploredMapCheckboxEnabled: true
			ExploredMapCheckboxLocked: true
'''
 text=text.replace('Rules:\n',rules)
 with zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED) as out:
  for name in archive.namelist():out.writestr(name,text.encode() if name=='map.yaml' else archive.read(name))
 print('bridge',x,y,'length',length,'oil',ox,oy)
