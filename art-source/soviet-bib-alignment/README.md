# Soviet construction-yard / power-plant bib alignment — installed locally

The user reported both buildings misaligned. They were pinned toward the rear-left
bib edges by a later override in `sequences/soviet-building-placement.yaml`, not the
older offsets visible in `soviet-building-art.yaml`.

Corrected runtime body offsets (Bottom, unchanged native Scale1):
- FACT.Soviet and FACT.Nukular.Soviet: -39,-54 -> **-1,-9** (right38/down45 pixels).
- POWR.Soviet: -29,-64 -> **0,-12** (right29/down52 pixels).
- Idle and damaged-idle agree. Bib size, image, registration and depth are unchanged.
- Visual selection follows the new pose; interaction footprint, collision, exits,
  production, powers, economy and all other gameplay are unchanged.

Placement centers the reviewed ground arrangement on the bib, not the roof/canvas.
The conyard's current supplied367x271 PNG has changed since the earlier calibration:
current SHA256 ec53df3ae940c0308249b16ba9a8e38a76af1fa434ebe64929540bc501da3c97,
alpha bounds (2,1,326,263). The former contact at (362,177) is transparent. New reviewed
contacts include crane feet, current right foundation and foreground equipment.
The original calibration/hash is archived, not silently treated as source corruption.
Power-plant182x209 art retains its original hash. No PNG was modified or rescaled.

Evidence:
- `before-after.png`, `before-after-selection.png`: native compositions from resolved
  engine registration, not live screenshots.
- `geometry-{before,after}.json`: actual engine body/bib/selection exports, including
  the Nukular variant.
- `*.rules-{before,after}.yaml`: resolved actor snapshots; normalized gameplay equal.
- `before-*.yaml/json`: prior calibration, placement and visual bounds.
- `protected-before.json`: source art, bibs, protected Nod conyard and authoritative
  map hashes; all retained.
- `engine-check.txt`: isolated `--check-soviet-bib-alignment` resolves actual body/bib
  sprites, anchors, scale and normal/damaged registrations.

Validation: isolated build and engine check pass; four targeted follow-up tests and
four existing resolved-rule tests pass. The broader old alignment suite is NOT green:
APWR, FIX and IRON PNG hashes differ from their previous calibration, FIX's old contact
is transparent and IRON has0.5234% outside its old visual envelope. These conditions
also apply to the archived pre-edit calibration; unrelated art/offsets were left alone.
No blanket hash refresh or unrelated realignment was performed.

Reproduce with `tools/test_soviet_bib_alignment_followup.py` and
`tools/review_soviet_bib_alignment_followup.py`. The generator now has an explicit
centered-ground policy only for these two buildings; other calibrations remain intact.
Live appearance still needs a restart/new match. No running game was stopped;
no commit, push, release or deployment was performed.
