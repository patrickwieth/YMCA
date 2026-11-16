# Further China feedback — installed locally

Requested moves are relative to the previously installed art, leaving the bibs fixed:

| Actor | New Bottom offset | Screen-space change |
| --- | --- | --- |
| Airfield | 8,28 / Scale1.5 | right12, down12 |
| Barracks | 0,20 / Scale1 | down6 |
| Supply Center | 0,19 / Scale1.25 | down5 |
| Internet Center | 0,20 / Scale1.25 | down10; retains4x4 bib |
| Speaker Tower / PDL | 0,-13 / Scale1 | another4 up; total12 up |
| Weapons Factory | 28,2 / Scale1 | another12 right; total28 right |
| Power Plant | 0,16 / Scale1 | net down6; raised4 from the previous20;3x3 bib |
| Advanced Power Plant | 0,28 / Scale1 | net down14; raised4 from the previous32;3x3 bib |

Both reactors keep their2x2 occupied cells (the existing2x3 Building dimensions include
a passable legacy bib row). Neither footprint nor placement/exit logic was rewritten.
The newly replaced reactor art was recalibrated, including smoke mouths (120,31),
(55,75), (117,42); neutral2x steam remains. Airfield source art had also changed during
the session: its requested offset is anchored to the installed20 ->28, not a fresh
auto-calibration that would have accidentally moved it to41.

Visual bounds follow the requested down/right shifts, and the higher speaker/PDL
selection/effect origins follow their art. The bunker and fortified bunker extra
Soviet bib overlays are removed, including AI/advanced-infantry variants; the PNGs'
own foundations remain. NKOREA_BUNKER inherits chbunker, so its now-invalid redundant
CHINABIB removal was deleted; its own Soviet bib remains.

Actual engine sprite loading/offsets and affected resolved variants pass the isolated
new-faction-building checker. Requested placement/selection regressions and full
normalized gameplay-isolation checks pass. PNG source updates are recorded explicitly;
prior hashes remain archived. Historical generated-runtime hashes (including the old
Shard/base and replaced Gatling base) were refreshed only against their authorized
installation manifests; the previous fixture is archived, not silently discarded.

Subsequent global bib/grid registration and the supplied Gatling platform replacement
are now applied. See `art-source/bib-grid-alignment/README.md` and
`art-source/chinese gatling/platform-cut/README.md`. Bunker origins were also corrected
from0,-724 to0,0,0 so their2x2 bodies/selection sit with the unchanged mine cells.
No process termination, commit, push or release. Live appearance still requires review.
