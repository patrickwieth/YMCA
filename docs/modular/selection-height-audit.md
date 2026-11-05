# Allied selection-height audit

Historical initial measurements, before the later user-requested percentage increases. Current
heights and the Airfield decoration-only footprint are listed in `allied-building-art.md`.

Measured from resolved rules/sequences and current supplied PNG alpha bounds. Coordinates are
source pixels relative to the actor center on flat isometric terrain (negative Y is upward).
This compares the TOP only, not full polygon containment, gameplay footprints or aesthetics.

## Corrected decoration heights

| Actor | Before | After | Visible PNG top | New selection top |
| --- | ---: | ---: | ---: | ---: |
| Prism Tower | 143 | 155 | -187 | -187 |
| Small fusion power | 42 | 46 | -118 | -118 |
| Barracks | 48 | 64 | -135.75 | -136 |
| Gap Generator | 122 | 146 | -210 | -210 |
| Chronosphere (now 2x2) | 64 | 84 | -148 | -148 |
| Weather Control | 75 | 81 | -145 | -145 |
| Tech Center | 78 | 86 | -150 | -150 |
| Radar Station | 93 | 103 | -167 | -167 |
| Airfield | 30 | 66 | -148 | -148 |

Ground corners/offsets remain fixed: only DecorationHeight changes. The Chronosphere's separate
requested 3x2 -> 2x2 body change is handled by the common driveable-bib template, not this audit.
Its 3x3 site keeps five passable cells. No PNG was modified or rescaled.

## Explicitly preserved exceptions

| Actor | PNG top | Box top | Suggested height if later approved | Why not changed |
| --- | ---: | ---: | ---: | --- |
| Bunker | -120 | -108 | 88 (currently 76) | User confirmed the box |
| Naval Yard | -180 | -128 | 82 (currently 30) | User confirmed the box |
| Refinery | -156 | -140 | 60 (currently 44) | User requested fixed top / trimmed lower edge |

These remain visible in the report as review-only exceptions. The diagnostic does not silently
change them. Conyard, War Factory, large reactor, Purifier, Helipad and Service Depot already
have box tops at or above their PNG tops; their heights were not reduced.

## Reproduce

From Game with the engine/runtime environment configured:

```
dotnet.exe engine/bin/OpenRA.Utility.dll mods/ca --export-allied-selection-geometry > geometry.json
python -B tools/audit_allied_selection_heights.py geometry.json --check
```

Use UTF-8 output when redirecting under older Windows PowerShell. The export resolves the full
sequence manifest, including the final placement layer. It mirrors the engine's integer/rounded
selection projection. PNG processing supports Scale 1 and Center/Bottom anchoring; it does not
run the renderer or account for terrain slopes, extra overlays, shadows or transient legacy SHP
animations. Re-run after replacing PNGs or changing offsets. Live visual confirmation remains
necessary; do not treat successful numeric checks as a screenshot review.
