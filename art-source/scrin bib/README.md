# Scrin bibs — native cut & paste

Requested 1x1, 2x2 and 3x3 cut/paste derivatives of the supplied
`ground_plating-1534399694-0.png`. Original retained byte-for-byte.

| PNG | Isometric footprint (128x64 cells) | PNG dimensions |
| --- | --- | --- |
| `bib1x1.png` | 128x64 | 128x90 |
| `bib2x2.png` | 256x128 | 256x154 |
| `bib3x3.png` | 384x192 | 384x218 |

The extra image height retains the source's raised front lip and edge details;
these are not stretched flat diamonds. Every visible pixel is copied at native
size from the supplied art. No resizing, color changes, painted fill or blending.

The supplied image clips the side corners of the larger plating arrangement.
Those corners are completed from translated interior, rear-edge and front-lip
cuts. Smaller variants remove central diagonal strips while retaining the outer
rim and corner details. Detached cut fragments were removed (three pixels in2x2).

`bib-contact-sheet.png` shows all three at native resolution;
`bib-contact-sheet-2x.png` is a nearest-neighbor inspection enlargement only.
`source-3x.png` is a review enlargement, not an export.

Rebuild with `python tools/art/cut_scrin_bibs.py` from Game. Tests:
`python tools/test_scrin_bib_cuts.py`. The manifest records dimensions and candidate
placement origins; the24px foundation height is inferred from the front lip.
The early Bottom0,-1 candidate referred to the lower lip. Installed sequences instead
use Bottom0,23: the TOP plating surface is registered to the existing building contact
plane. Actual engine sprite loading/offsets are checked; live review remains pending.

## Seam repair follow-up

The user liked the overall bibs but identified hard joins across unrelated parts.
`studies/before-seam-repair/` preserves that version. The new joins choose binary
source-piece boundaries within overlapping native cuts, favoring matching dark
plate joints rather than slicing through bright details. A complete bounded source
plate, including its rim and attached feeler, bridges the remaining left tube/plate
junction in2x2 and3x3. This is cut/paste, not blurred seam concealment: every visible
RGBA pixel still comes directly from the original. All three alpha silhouettes,
canvas sizes and origins are unchanged. Native/enlarged comparisons were inspected.

`seam-before-after.png` shows the previous and repaired versions. The user subsequently
requested installation: all three were promoted unchanged to
`mods/ca/bits/scrin/buildings/bib{1,2,3}x{1,2,3}.png` (matching sizes), with decorative
bindings in `rules/scrin/bibs.yaml` and `sequences/scrin-bibs.yaml`.
See `integration/README.md` and `integration/placement-review.png`. Tools additionally require SciPy for the binary
seam calculation (`tools/art/quilt_scrin_bib_seams.py`).

Initial intermediate cuts remain under `studies/initial-cuts/`; the first preparation
was source-only. The subsequent authorized installation changes decorative runtime
art/bindings only. No gameplay, commits, pushes or releases were changed.
