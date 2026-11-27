"""Isolated eight-facing, air/ground Carryall rotor review. Never install as a playable map."""
import sys, re, zipfile, struct
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
from convert_handcrafted_layouts import miniyaml
src, dst = map(Path, sys.argv[1:3])
def cell(u,v): return u + (v+1)//2, v//2-u  # native rectangular-isometric CPos
with zipfile.ZipFile(src) as z:
 text=z.read('map.yaml').decode().replace('\r\n','\n')
 # Keep player spawn markers, but remove unrelated resource and scenery actors.
 actors=''
 for i,a in enumerate(miniyaml(text).child('Actors').children):
  if a.value == 'mpspawn':
   x,y=cell(10 if not actors else 65,10)
   actors+=f'\tspawn{i}: mpspawn\n\t\tOwner: Neutral\n\t\tLocation: {x},{y}\n'
 for row in range(4):
  for col in range(4):
   x,y=cell(34+4*col,34+5*row)
   actors+=f'\trotor{row}{col}: ocar.review'+('ground' if row>=2 else 'air')+f'\n\t\tOwner: Neutral\n\t\tLocation: {x},{y}\n\t\tFacing: {((row%2)*4+col)*128}\n'
 text=re.sub(r'^Actors:\n.*?(?=^Rules:)', 'Actors:\n'+actors, text, flags=re.M|re.S)
 cx,cy=cell(41,41)
 rules=f'''Rules:
	ocar.reviewair:
		Inherits: OCAR
		Aircraft:
			CruiseAltitude: 1c0
			IdleTurnSpeed: 0
			Repulsable: false
	ocar.reviewground:
		Inherits: ocar.reviewair
		Aircraft:
			CruiseAltitude: 0c0
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
 text=text.replace('Rules:\n',rules)
 with zipfile.ZipFile(dst,'w',zipfile.ZIP_DEFLATED) as out:
  for n in z.namelist():
   data=z.read(n)
   if n=='map.yaml':data=text.encode()
   if n=='map.bin':
    data=bytearray(data); _,w,h,_,_,offset=struct.unpack_from('<BHHIII',data)
    data[offset:offset+2*w*h]=bytes(2*w*h)
   out.writestr(n,data)
