"""Review full Allied selection polygons against supplied PNGs; never edits rules/art.
Input: --export-allied-selection-geometry JSON. Optional self-contained HTML overlay report.
Pixel coverage includes baked-in shadows/overhangs: findings are review hints, not auto-fixes.
"""
import argparse
import base64
import html
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]


def measure(entry):
    if float(entry["Scale"]) != 1 or entry["Anchor"] not in ("Center", "Bottom"):
        raise ValueError("Only Scale 1 Center/Bottom PNG registrations are supported")
    path = ROOT / "mods/ca" / entry["File"]
    with Image.open(path) as image:
        width, height = image.size
        alpha = image.convert("RGBA").getchannel("A").point(lambda a: 255 if a >= 32 else 0)
    ox, oy = map(float, entry["Offset"].split(",")[:2])
    x = -width / 2 + ox
    y = -height / 2 + oy
    if entry["Anchor"] == "Bottom":
        y += width / 4 - height / 2
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"Empty PNG: {path}")
    polygon = entry["SelectionPolygon"]
    mask = Image.new("L", (width, height))
    ImageDraw.Draw(mask).polygon([(px - x, py - y) for px, py in polygon], fill=255)
    # Two source pixels of tolerance for rounding and edge antialiasing.
    mask = mask.filter(ImageFilter.MaxFilter(5))
    outside_mask = ImageChops.subtract(alpha, mask)
    outside = outside_mask.histogram()[255]
    total = alpha.histogram()[255]
    # Split excursions across the roof edges from those below the ground diamond.
    # This distinguishes a low roof from a base-position/depth mismatch.
    upper, upper_left, lower_left, lower, lower_right, upper_right = polygon
    def outside_in(vertices):
        region = Image.new("L", (width, height))
        ImageDraw.Draw(region).polygon([(px - x, py - y) for px, py in vertices], fill=255)
        return 100 * ImageChops.multiply(outside_mask, region).histogram()[255] / total
    roof_outside = outside_in([upper_left, upper, upper_right,
                               (upper_right[0], y - 1000), (upper_left[0], y - 1000)])
    base_outside = outside_in([lower_left, lower, lower_right,
                               (lower_right[0], y + height + 1000), (lower_left[0], y + height + 1000)])
    left, right = min(p[0] for p in polygon), max(p[0] for p in polygon)
    art_left, art_right = bounds[0] + x, bounds[2] + x
    return dict(x=x, y=y, width=width, height=height, path=path,
                art_width=bounds[2] - bounds[0], box_width=right - left,
                left_over=max(0, left - art_left), right_over=max(0, art_right - right),
                center_delta=(art_left + art_right - left - right) / 2,
                outside=100 * outside / total, roof_outside=roof_outside, base_outside=base_outside)


def preview(entry, result):
    points = entry["SelectionPolygon"] + entry["GroundCorners"]
    x, y, w, h = (result[k] for k in ("x", "y", "width", "height"))
    left = min(x, *(p[0] for p in points)) - 12
    top = min(y, *(p[1] for p in points)) - 12
    right = max(x + w, *(p[0] for p in points)) + 12
    bottom = max(y + h, *(p[1] for p in points)) + 12
    source = base64.b64encode(result["path"].read_bytes()).decode("ascii")
    coords = lambda vertices: " ".join(f"{px},{py}" for px, py in vertices)
    return (f'<svg width="{right-left}" height="{bottom-top}" viewBox="{left} {top} {right-left} {bottom-top}">'
            f'<image x="{x}" y="{y}" width="{w}" height="{h}" href="data:image/png;base64,{source}"/>'
            f'<polygon points="{coords(entry["GroundCorners"])}" fill="none" stroke="#ffcc44" stroke-dasharray="4 3"/>'
            f'<polygon points="{coords(entry["SelectionPolygon"])}" fill="none" stroke="#55ff88"/>'
            '<path d="M -4,0 H 4 M 0,-4 V 4" stroke="#ff7799"/></svg>')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("geometry", type=Path)
    parser.add_argument("--html", type=Path)
    args = parser.parse_args()
    entries = json.loads(args.geometry.read_text(encoding="utf-8-sig"))
    cards = []
    print("| Actor | Body cells | Box / art width | Left / right overhang | Center delta X | Alpha outside polygon (roof / base) |")
    print("| --- | --- | ---: | ---: | ---: | ---: |")
    for entry in entries:
        result = measure(entry)
        actor = html.escape(entry["Actor"])
        body = "x".join(map(str, entry["BodyCells"]))
        summary = (f'{result["box_width"]:g}/{result["art_width"]:g}px; '
                   f'left/right overhang {result["left_over"]:g}/{result["right_over"]:g}px; '
                   f'{result["outside"]:.1f}% alpha outside '
                   f'(roof {result["roof_outside"]:.1f}%, base {result["base_outside"]:.1f}%)')
        print(f'| {actor} | {body} | {result["box_width"]:g} / {result["art_width"]:g} | '
              f'{result["left_over"]:g} / {result["right_over"]:g} | '
              f'{result["center_delta"]:g} | {result["outside"]:.1f}% '
              f'({result["roof_outside"]:.1f} / {result["base_outside"]:.1f}) |')
        if args.html:
            note = " — previously confirmed box" if entry["PreserveConfirmedBox"] else ""
            footprint = entry.get("DecorationFootprint", [0, 0])
            decoration = ""
            if footprint != [0, 0]:
                decoration = f'; decoration {footprint[0]}x{footprint[1]} + depth {entry["DecorationFootprintExtraDepth"]} world units'
            cards.append(f'<section><h2>{actor} (body {body}{decoration}){note}</h2><p>{summary}</p>{preview(entry, result)}</section>')
    if args.html:
        args.html.write_text('<!doctype html><meta charset="utf-8"><title>Allied selection shape review</title>'
                            '<style>body{background:#20242b;color:#eee;font:14px sans-serif}main{display:flex;flex-wrap:wrap;gap:16px}'
                            'section{background:#303640;padding:12px}h2{font-size:16px}svg{background:#3c434c}</style>'
                            '<h1>Allied selection shape review — flat terrain, idle PNG, Scale 1</h1>'
                            '<p>Green: selection outline. Yellow: ground diamond. Pink: actor center. Original PNGs are embedded unchanged.</p>'
                            '<p>Alpha outside includes shadows and overhangs. Width/depth cannot be inferred reliably from a silhouette. '
                            'These are diagnostic overlays, not screenshots or automatic resize instructions.</p><main>'
                            + "".join(cards) + '</main>', encoding="utf-8")


if __name__ == "__main__":
    main()
