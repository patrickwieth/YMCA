# Allied selection shape review

The findings below are the historical pre-adjustment review. The linked HTML has been refreshed
after the latest user-requested height changes and Airfield decoration-only enlargement; its
numbers can differ. Current settings are documented in `allied-building-art.md`.

This extends the earlier top-height check to the complete six-vertex selection polygon and
four ground corners, using resolved rules/sequences and the engine's flat-terrain projection.
No building rules, footprints, selection sizes or PNG files were changed during this review.

Open [the self-contained overlay report](allied-selection-shapes.html) to compare all 18 actors.
Green is the selection outline, yellow the ground diamond, pink the actor center. Source PNGs
are embedded unchanged at Scale 1; these are diagnostic overlays, not gameplay screenshots.

## Findings

- Large reactor (APWR): 13.4% of visible alpha is below the ground diamond.
- Ore Purifier (OREP): 15.0% below the ground diamond.
- Allied Airfield: 13.6% below the diamond; art bounding-box center is 19.5px left of the box center.
- Helipad: 7.4% below the ground diamond.
- Conyard: 3.7% above the sloping roof edges and 3.6% below the ground edges.
- Small reactor: 3.8% above sloping roof edges, despite matching the global top point.
- War Factory: 3.8% above sloping roof edges.
- Chronosphere: 1.6% outside overall, with only 2px horizontal overhang; no substantial width mismatch.
- Barracks, Prism Tower, Gap Generator, Weather Control, Tech Center, Radar and Service Depot
  each have at most 1% outside the polygon with the configured tolerance.

All artwork fits within the overall horizontal extent, except Chronosphere's 2px left overhang.
Some outlines are noticeably broader than the PNG: Conyard 392px vs 288px, Service Depot 384px
vs 250px, Gap Generator 256px vs 97px. **This does not prove the footprint is wrong:** slim towers
can intentionally occupy a larger site, and a silhouette cannot identify the correct ground depth.
Do not automatically shrink these approved gameplay footprints or selectors to alpha bounds.

Previously confirmed exceptions remain unchanged: Naval Yard has 16.7% above roof edges, Bunker
7.4%, Refinery 2.5%. Their existing confirmations should be reconsidered explicitly before edits.

## Method and limits

PNG pixels with alpha >=32 are compared with the full polygon, allowing two source pixels of
edge tolerance. Percentages are fractions of visible pixels, not lengths or suggested resize
amounts. Baked-in shadows, ramps, antennas and overhangs can legitimately extend outside a frame.
External shadow traits and bib overlays are not included. A lower-edge excursion can result from
depth, vertical registration, or artwork perspective; it is not automatically a width error.

The old top-only check could not detect sloping roof-edge excursions or a too-small/misaligned
ground diamond. This review adds those diagnostics without treating visual fit as permission to
change collision, passability, exits, or selector dimensions. Flat idle-frame calculations do not
replace live review on varied terrain and during animations.

## Reproduce

```
dotnet.exe engine/bin/OpenRA.Utility.dll mods/ca --export-allied-selection-geometry > geometry.json
python -B tools/audit_allied_selection_shapes.py geometry.json --html docs/modular/allied-selection-shapes.html
python -B -m unittest discover -s tools -p test_allied_selection_shapes.py
```

Redirect JSON as UTF-8 when using older Windows PowerShell. The report generator only reads the
PNG assets; its optional HTML output embeds their original bytes without resizing or recoloring.
