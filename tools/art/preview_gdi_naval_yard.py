"""Native naval-yard composition with resolved selection; not a live water test."""
from pathlib import Path
import json,sys
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from calibrate_gdi_building_alignment import ROOT,report
entries=json.loads((ROOT/'docs/modular/gdi-building-alignment-geometry.json').read_text())
out=ROOT/'docs/modular/gdi-naval-yard-preview.png'
report(entries,out,actors=('syrd.gdi',))
with Image.open(out) as sheet:card=sheet.crop((0,0,576,sheet.height))
card.save(out)
print(out)
