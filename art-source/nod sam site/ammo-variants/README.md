# Nod SAM — 0, 1, 2, 3 and 4 missiles

The user approved pass12 and requested five ammo variants. These are now exported
with32 directions each. These exports are now installed on NSAM; see
`../integration/README.md` for runtime ammo/rendering and validation details.

## Files

- `variants-native.png` / `variants-2x.png`: all five states, reference and opposite.
- `all-five-rotation.gif`: all states rotating together for comparison.
- `base.png`: one unchanged fixed base.
- `turret-0.png` through `turret-4.png`: native256×256/32-facing PNG sheets.
- Each numbered folder contains its blend, geometry/visibility record, separate
  frames, full rotation contact sheet and GIF. States0–3 include fresh raw/ID renders.

## Frozen geometry

The renderer opens the approved `pass-12/nod-sam.blend`; it does NOT regenerate or
redesign the model. Only complete missile-object visibility changes. Empty launch
rails, saddles, trunnions, cables and the entire support remain. Hidden surfaces
are actually rendered in3D, not patched by erasing pixels from a loaded image.

Firing order is slot2,3,0,1 (front pair before rear pair):

| Remaining | Visible slots |
| --- | --- |
|4|0,1,2,3|
|3|0,1,3|
|2|0,1|
|1|1|
|0|none|

No surviving missile changes position. All states share pivot119,115, native scale1
and facing start4. The full four-missile sheet and frames are copied byte-for-byte
from the approved export. An independent loaded-blend control render also matches
its original raw pixels exactly. Geometry/UV/local-transform hashes remain equal
for every object in every state. `manifest.json` records the frozen blend hash,
slot memberships and per-state visibility.

Blender discards unused diagnostic surface-ID materials on save/load; the export
script recreates their exact original color encoding without changing the actual
materials or approved scene. This export process changes no supplied originals.
Subsequent runtime bindings are documented separately. No commit/push/release.

## Repeat

```sh
blender --background --python tools/art/render_nod_sam_ammo.py
# For n = 0,1,2,3:
python -B tools/art/review_nod_sam.py --pass 12 --study-dir "art-source/nod sam site/ammo-variants/0"
python -B tools/art/review_nod_sam_ammo.py
python -B -m unittest discover -s tools -p test_nod_sam_ammo.py
```

Native/side/rear/end-on views reviewed; tests check frozen output, unchanged
geometry, fixed slots/base, no leftover missile parts in the empty state,32-frame
ordering and no clipping. New variants are not claimed to have been tested in-game.
