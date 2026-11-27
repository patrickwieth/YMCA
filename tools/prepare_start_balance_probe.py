"""Create a review-only native economy probe from a freshly exported procedural map.
Usage: python tools/prepare_start_balance_probe.py INPUT.oramap OUTPUT.oramap
Never install the output in a normal map folder.
"""
import re
import sys
import zipfile
from pathlib import Path
from convert_handcrafted_layouts import miniyaml

if __name__ == '__main__':
    source, destination = map(Path, sys.argv[1:])
    if destination.exists(): raise SystemExit('Refusing to overwrite an existing package.')
    with zipfile.ZipFile(source) as archive:
        text = archive.read('map.yaml').decode().replace('\r\n', '\n')
        data = miniyaml(text)
        if data.child('Rules').children: raise SystemExit('Expected an export without map-local rule overrides.')
        starts = [tuple(map(int, a.child('Location').value.split(','))) for a in data.child('Actors').children if a.value == 'mpspawn']
        if not starts: raise SystemExit('No starting positions.')
        cx = sum(p[0] for p in starts) // len(starts); cy = sum(p[1] for p in starts) // len(starts)
        bounds = list(map(int, data.child('Bounds').value.split(',')))
        zoom = min(0.16, 950 / (bounds[2] * 128), 650 / (bounds[3] * 64))
        text = re.sub(r'^(Title: .+)$', r'\1 [REVIEW ONLY]', text, flags=re.M)
        rules = f'''Rules:
	^Autobattler:
		Autobattler:
			RequiresCondition: false
		ArmyFollower:
			RequiresCondition: false
	World:
		HandcraftedRuntimeProbe:
			ExpectedOilGroundAccess: true
			CheckHarvestRoutes: true
		TerrainCalibrationView:
			Center: {cx},{cy}
			ZoomScale: {zoom:.5f}
			MinimumZoomScale: 0.04
			ScreenshotTicks: 60
	Player:
		Shroud:
			FogCheckboxEnabled: false
			FogCheckboxLocked: true
			ExploredMapCheckboxEnabled: true
			ExploredMapCheckboxLocked: true
'''
        if text.count('Rules:\n') != 1: raise SystemExit('Unexpected Rules declaration.')
        text = text.replace('Rules:\n', rules)
        with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as output:
            for name in archive.namelist(): output.writestr(name, text.encode() if name == 'map.yaml' else archive.read(name))
