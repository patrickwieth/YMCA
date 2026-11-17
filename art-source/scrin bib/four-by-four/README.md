# Additional native4x4 Scrin bib

Output: ../bib4x4.png (512x282 RGBA). Original source and existing1x1/2x2/3x3 files
are untouched. No runtime assignment has been made for this new size.

Four integer-translated pieces of the reviewed3x3 extend the slab by one grid cell
in both isometric axes. Binary minimum-cost seams choose whole existing pixels along
matching darker joints; no blur, resizing, interpolation, recoloring or painted filler.
All visible RGBA pixels are present in the supplied ground_plating PNG. Repeated
native panels are intentional cut/paste construction, not foreign imagery.

Top surface center:256,131. Inferred lower ground center:256,155. Future runtime
registration at Scale1 uses Bottom0,23, matching the installed smaller Scrin bibs.
Do not use the lower front lip as the top grid plane.

Reproduce: python tools/art/cut_scrin_bib4x4.py
Validation: python tools/test_native_cut_followups.py
Source hashes and method: ../bib4x4-cut-paste.json
Review: before-after-native.png / before-after-2x.png (native composed, not live).
