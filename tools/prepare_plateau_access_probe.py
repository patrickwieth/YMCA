"""Review-only Carryall/MCV/roof movement fixture; never install in normal map folders."""
import re
import sys
import zipfile
from pathlib import Path

source, destination, result = map(Path, sys.argv[1:4])
center = sys.argv[4] if len(sys.argv) > 4 else '80,0'
if destination.exists():
    raise SystemExit('Use a fresh destination.')
with zipfile.ZipFile(source) as archive:
    text = archive.read('map.yaml').decode().replace('\r\n', '\n')
    if text.count('Rules:\n') != 1:
        raise SystemExit('Unexpected Rules declaration.')
    text = re.sub(r'^(Title: .+)$', r'\1 [PLATEAU REVIEW ONLY]', text, flags=re.M)
    rules = f'''Rules:
	^Autobattler:
		Autobattler:
			RequiresCondition: false
		ArmyFollower:
			RequiresCondition: false
	World:
		TerrainCalibrationView:
			Center: {center}
			ZoomScale: 0.35
			MinimumZoomScale: 0.04
			ScreenshotTicks: 400
		PlateauAccessProbe:
			ResultPath: {result.as_posix()}
			CheckSsm: {'true' if 'BattleNexus' in text else 'false'}
	Player:
		Shroud:
			FogCheckboxEnabled: false
			FogCheckboxLocked: true
			ExploredMapCheckboxEnabled: true
			ExploredMapCheckboxLocked: true
'''
    text = text.replace('Rules:\n', rules)
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as output:
        for name in archive.namelist():
            output.writestr(name, text.encode() if name == 'map.yaml' else archive.read(name))
