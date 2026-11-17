"""Compare resolved selection tops with source PNG alpha tops on flat terrain.

Export first with: OpenRA.Utility ... --export-allied-selection-geometry > geometry.json
Then: python tools/audit_allied_selection_heights.py geometry.json [--check]
Requires Pillow. Reads images only. This is a top-height diagnostic, not a full visual proof.
"""
import argparse
import json
import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def measure(entry):
    if float(entry["Scale"]) != 1:
        raise ValueError(f"{entry['Actor']}: diagnostic expects Scale 1")
    with Image.open(ROOT / "mods/ca" / entry["File"]) as image:
        box = image.convert("RGBA").getchannel("A").getbbox()
        if box is None:
            raise ValueError(f"{entry['Actor']}: empty image")
        top = box[1] - image.height / 2
        if entry["Anchor"] == "Bottom":
            top += image.width / 4 - image.height / 2
        elif entry["Anchor"] != "Center":
            raise ValueError(f"{entry['Actor']}: unsupported anchor {entry['Anchor']}")
    top += float(entry["Offset"].split(",")[1])
    shortage = entry["SelectionTop"] - top
    increase = math.ceil(shortage) if shortage > 1 else 0
    return top, increase


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("geometry", type=Path)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    entries = json.loads(args.geometry.read_text(encoding="utf-8-sig"))
    print("| Actor | Art top | Box top | Height | Suggested height | Status |")
    print("| --- | ---: | ---: | ---: | ---: | --- |")
    failed = False
    for entry in entries:
        top, increase = measure(entry)
        preserve = entry["PreserveConfirmedBox"]
        status = "confirmed box: review only" if preserve else "too low" if increase else "covers top"
        failed |= increase > 0 and not preserve
        print(f"| {entry['Actor']} | {top:g} | {entry['SelectionTop']} | {entry['Height']} | "
              f"{entry['Height'] + increase} | {status} |")
    return 1 if args.check and failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
